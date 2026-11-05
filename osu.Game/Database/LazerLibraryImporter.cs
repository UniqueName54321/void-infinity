// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Game.Beatmaps;
using osu.Game.Skinning;
using Newtonsoft.Json;
using Realms;
using osu.Game.Overlays.Notifications;

namespace osu.Game.Database
{
    /// <summary>Copies beatmap sets from a snapshot of another lazer library. Never opens the source Realm for writing.</summary>
    public class LazerLibraryImporter
    {
        private readonly IModelImporter<BeatmapSetInfo> beatmaps;

        private readonly IModelImporter<SkinInfo>? skins;
        private readonly Func<string[], Task<int>>? applyOptions;
        private readonly string settingsFilename;

        public LazerLibraryImporter(IModelImporter<BeatmapSetInfo> beatmaps, IModelImporter<SkinInfo>? skins = null,
                                    Func<string[], Task<int>>? applyOptions = null, string settingsFilename = "game.ini")
        {
            this.beatmaps = beatmaps;
            this.skins = skins;
            this.applyOptions = applyOptions;
            this.settingsFilename = settingsFilename;
        }

        public static bool IsLibrary(string directory) => File.Exists(Path.Combine(directory, OsuGameBase.CLIENT_DATABASE_FILENAME))
                                                         && Directory.Exists(Path.Combine(directory, "files"));

        public async Task<LazerLibraryImportResult> ImportAsync(string sourceDirectory, string destinationDirectory,
                                                               IProgress<string>? progress = null, CancellationToken cancellationToken = default, LazerLibraryImportSelection? selection = null)
        {
            selection ??= new LazerLibraryImportSelection();
            if (!selection.Beatmaps && !selection.Skins && !selection.Options)
                throw new InvalidOperationException("Select at least one kind of content to import.");
            if (selection.Skins && skins == null) throw new InvalidOperationException("Skin import is unavailable.");
            if (selection.Options && applyOptions == null) throw new InvalidOperationException("Options import is unavailable.");
            sourceDirectory = Path.GetFullPath(sourceDirectory);
            destinationDirectory = Path.GetFullPath(destinationDirectory);
            if (string.Equals(Path.TrimEndingDirectorySeparator(sourceDirectory), Path.TrimEndingDirectorySeparator(destinationDirectory),
                              OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new InvalidOperationException("Choose a different library from void!infinity's current data directory.");
            if (!IsLibrary(sourceDirectory))
                throw new InvalidDataException("Select the lazer data folder containing client.realm and files.");

            string temporaryDirectory = Path.Combine(Path.GetTempPath(), "void-lazer-import-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
            try
            {
                var result = new LazerLibraryImportResult();
                if (selection.Beatmaps || selection.Skins)
                {
                    var source = new FileInfo(Path.Combine(sourceDirectory, OsuGameBase.CLIENT_DATABASE_FILENAME));
                    long sourceLength = source.Length;
                    DateTime sourceModified = source.LastWriteTimeUtc;
                    string snapshot = Path.Combine(temporaryDirectory, "snapshot.realm");
                    progress?.Report("Reading library snapshot…");
                    using (var input = new FileStream(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var output = new FileStream(snapshot, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
                    source.Refresh();
                    if (source.Length != sourceLength || source.LastWriteTimeUtc != sourceModified)
                        throw new IOException("The source library changed during copying. Close osu!lazer and try again.");

                    // Read dynamically so newer library schemas do not trigger a migration using this client's models.
                    var sets = selection.Beatmaps ? ReadSets(snapshot) : new List<Dictionary<string, string>>();
                    for (int index = 0; index < sets.Count; index++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        progress?.Report($"Importing beatmap set {index + 1} of {sets.Count}…");
                        string archivePath = Path.Combine(temporaryDirectory, "beatmap.vibz");
                        try
                        {
                            WriteArchive(sourceDirectory, sets[index], archivePath, cancellationToken);
                            // The normal importer handles duplicate sets and content-addressed asset storage.
                            // A private temporary archive is passed in, never a path inside the source library.
                            var imported = await beatmaps.Import(new ProgressNotification(), new[] { new ImportTask(archivePath) },
                                new ImportParameters { SkipOnlineMetadataLookup = true }).ConfigureAwait(false);
                            if (!imported.Any()) result.Failed++;
                            else result.Imported++;
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException)
                        {
                            result.Failed++;
                        }
                        finally
                        {
                            if (File.Exists(archivePath)) File.Delete(archivePath);
                        }
                    }
                    if (selection.Skins)
                    {
                        var skinSets = ReadSkins(snapshot);
                        for (int index = 0; index < skinSets.Count; index++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            progress?.Report($"Importing skin {index + 1} of {skinSets.Count}…");
                            string archivePath = Path.Combine(temporaryDirectory, "skin.osk");
                            var skin = skinSets[index];
                            try
                            {
                                WriteArchive(sourceDirectory, skin.Files, archivePath, cancellationToken);
                                using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
                                {
                                    foreach (var entry in archive.Entries.Where(e => e.FullName.Equals("skininfo.json", StringComparison.OrdinalIgnoreCase)).ToArray())
                                        entry.Delete();
                                    using var writer = new StreamWriter(archive.CreateEntry("skininfo.json").Open());
                                    writer.Write(JsonConvert.SerializeObject(new { skin.Name, skin.Creator, InstantiationInfo = SafeSkinType(skin.InstantiationInfo).AssemblyQualifiedName }));
                                }
                                var imported = await skins!.Import(new ProgressNotification(), new[] { new ImportTask(archivePath) }).ConfigureAwait(false);
                                var importedSkin = imported.FirstOrDefault();
                                if (importedSkin == null) result.Failed++;
                                else
                                {
                                    importedSkin.PerformWrite(s => { s.Name = skin.Name; s.Creator = skin.Creator; });
                                    result.SkinsImported++;
                                }
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException)
                            { result.Failed++; }
                            finally { if (File.Exists(archivePath)) File.Delete(archivePath); }
                        }
                    }
                }
                if (selection.Options)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report("Importing local options…");
                    string path = Path.Combine(sourceDirectory, settingsFilename);
                    if (File.Exists(path))
                    {
                        var lines = await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false);
                        result.OptionsImported = await applyOptions!(lines).WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else result.Failed++;
                }
                return result;
            }
            finally
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        internal static List<Dictionary<string, string>> ReadSets(string snapshotPath)
        {
            using var realm = Realm.GetInstance(new RealmConfiguration(snapshotPath) { IsReadOnly = true, IsDynamic = true });
            if (!realm.Schema.Any(s => s.Name == "BeatmapSet"))
                throw new InvalidDataException("This database does not contain a supported lazer beatmap library.");
            var result = new List<Dictionary<string, string>>();
            foreach (var set in realm.DynamicApi.All("BeatmapSet"))
            {
                if (set.DynamicApi.Get<bool>("DeletePending")) continue;
                var files = ReadFiles(set);
                if (files.Keys.Any(f => f.EndsWith(".osu", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".vib", StringComparison.OrdinalIgnoreCase)))
                    result.Add(files);
            }
            return result;
        }

        private static Dictionary<string, string> ReadFiles(IRealmObjectBase model)
        {
            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var usage in model.DynamicApi.GetList<IRealmObjectBase>("Files"))
            {
                string filename = usage.DynamicApi.Get<string>("Filename");
                var file = usage.DynamicApi.Get<IRealmObjectBase>("File");
                string hash = file.DynamicApi.Get<string>("Hash");
                if (!IsSafeFilename(filename) || hash.Length != 64 || !hash.All(Uri.IsHexDigit))
                    throw new InvalidDataException("The library contains an invalid beatmap filename or asset hash.");
                files.Add(filename.Replace('\\', '/'), hash);
            }
            return files;
        }

        internal static List<LazerSkinImport> ReadSkins(string snapshotPath)
        {
            using var realm = Realm.GetInstance(new RealmConfiguration(snapshotPath) { IsReadOnly = true, IsDynamic = true });
            if (!realm.Schema.Any(s => s.Name == "Skin")) return new List<LazerSkinImport>();
            var result = new List<LazerSkinImport>();
            foreach (var skin in realm.DynamicApi.All("Skin"))
            {
                if (skin.DynamicApi.Get<bool>("DeletePending") || skin.DynamicApi.Get<bool>("Protected")) continue;
                var files = ReadFiles(skin);
                if (files.Count == 0) continue;
                result.Add(new LazerSkinImport
                {
                    Name = skin.DynamicApi.Get<string>("Name"), Creator = skin.DynamicApi.Get<string>("Creator"),
                    InstantiationInfo = skin.DynamicApi.Get<string>("InstantiationInfo"), Files = files,
                });
            }
            return result;
        }

        private static Type SafeSkinType(string instantiationInfo)
        {
            string name = instantiationInfo.Split(',')[0].Trim();
            Type[] supported = { typeof(LegacySkin), typeof(TrianglesSkin), typeof(ArgonSkin), typeof(RetroSkin) };
            return supported.FirstOrDefault(t => t.FullName == name) ?? typeof(LegacySkin);
        }

        internal static bool IsSafeFilename(string name) => !string.IsNullOrWhiteSpace(name) && !Path.IsPathRooted(name)
            && !name.Contains(':') && !name.Contains('\0') && !name.Replace('\\', '/').Split('/').Any(p => p is ".." or "." or "");

        internal static void WriteArchive(string sourceDirectory, Dictionary<string, string> files, string archivePath, CancellationToken cancellationToken)
        {
            using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
            foreach (var (name, hash) in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string assetPath = Path.Combine(sourceDirectory, "files", hash[..1], hash[..2], hash);
                if (!File.Exists(assetPath))
                    throw new FileNotFoundException("A beatmap asset is missing from the source library.", assetPath);
                archive.CreateEntryFromFile(assetPath, name, CompressionLevel.Fastest);
            }
        }
    }

    public class LazerLibraryImportSelection
    {
        public bool Beatmaps { get; set; } = true;
        public bool Skins { get; set; }
        public bool Options { get; set; }
    }

    internal class LazerSkinImport
    {
        public string Name { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public string InstantiationInfo { get; set; } = string.Empty;
        public Dictionary<string, string> Files { get; set; } = new Dictionary<string, string>();
    }

    public class LazerLibraryImportResult
    {
        public int Imported { get; internal set; }
        public int SkinsImported { get; internal set; }
        public int OptionsImported { get; internal set; }
        public int Failed { get; internal set; }
    }
}
