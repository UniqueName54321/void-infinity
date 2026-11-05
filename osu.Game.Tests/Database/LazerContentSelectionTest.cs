// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using Moq;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Models;
using osu.Game.Skinning;
using Realms;

namespace osu.Game.Tests.Database
{
    public class LazerContentSelectionTest : RealmTest
    {
        [Test]
        public async Task TestOptionsOnlyNeverOpensDatabaseOrImportsAssets()
        {
            using var source = new TemporaryNativeStorage("void-options-source");
            using var destination = new TemporaryNativeStorage("void-options-destination");
            using var config = new OsuConfigManager(destination);
            config.SetValue(OsuSetting.Token, "fixture-local-token");
            string root = source.GetFullPath(string.Empty);
            Directory.CreateDirectory(Path.Combine(root, "files"));
            File.WriteAllText(Path.Combine(root, OsuGameBase.CLIENT_DATABASE_FILENAME), "not a realm; options-only must not read it");
            string options = "MenuTips = False\nDimLevel = 0.25\nToken = fixture-foreign-token\nEnableExperimentalFeatures = True\nExperimentalTest = True\nUserOnlineStatus = Online\nDimLevel = NaN\n";
            string optionsPath = Path.Combine(root, config.SettingsFilename);
            File.WriteAllText(optionsPath, options);
            var beatmaps = new Mock<IModelImporter<BeatmapSetInfo>>(MockBehavior.Strict);
            var skins = new Mock<IModelImporter<SkinInfo>>(MockBehavior.Strict);
            var importer = new LazerLibraryImporter(beatmaps.Object, skins.Object, lines => Task.FromResult(config.ImportLocalPreferences(lines)), config.SettingsFilename);
            var result = await importer.ImportAsync(root, destination.GetFullPath(string.Empty), selection: new LazerLibraryImportSelection { Beatmaps = false, Options = true });
            Assert.That(result.OptionsImported, Is.EqualTo(2));
            Assert.That(result.Imported, Is.Zero);
            Assert.That(result.SkinsImported, Is.Zero);
            Assert.That(config.Get<bool>(OsuSetting.MenuTips), Is.False);
            Assert.That(config.Get<double>(OsuSetting.DimLevel), Is.EqualTo(0.25));
            Assert.That(config.Get<string>(OsuSetting.Token), Is.EqualTo("fixture-local-token"));
            Assert.That(config.Get<bool>(OsuSetting.EnableExperimentalFeatures), Is.False);
            Assert.That(config.Get<bool>(OsuSetting.ExperimentalTest), Is.False);
            Assert.That(File.ReadAllText(optionsPath), Is.EqualTo(options));
            beatmaps.VerifyNoOtherCalls();
            skins.VerifyNoOtherCalls();
        }

        [Test]
        public async Task TestCancellationStopsWaitingForOptionsApplication()
        {
            using var source = new TemporaryNativeStorage("void-cancel-source");
            using var destination = new TemporaryNativeStorage("void-cancel-destination");
            string root = source.GetFullPath(string.Empty);
            Directory.CreateDirectory(Path.Combine(root, "files"));
            File.WriteAllText(Path.Combine(root, OsuGameBase.CLIENT_DATABASE_FILENAME), "unused");
            File.WriteAllText(Path.Combine(root, "game.ini"), "MenuTips = False");
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var beatmaps = new Mock<IModelImporter<BeatmapSetInfo>>(MockBehavior.Strict);
            var importer = new LazerLibraryImporter(beatmaps.Object, applyOptions: _ => { entered.SetResult(); return pending.Task; });
            using var cancellation = new CancellationTokenSource();
            var operation = importer.ImportAsync(root, destination.GetFullPath(string.Empty), cancellationToken: cancellation.Token,
                selection: new LazerLibraryImportSelection { Beatmaps = false, Options = true });
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();
            Assert.CatchAsync<OperationCanceledException>(async () => await operation.WaitAsync(TimeSpan.FromSeconds(5)));
            beatmaps.VerifyNoOtherCalls();
        }

        [Test]
        public void TestSkinsOnlyDoesNotImportBeatmapsAndPreservesMetadata()
        {
            RunTestWithRealmAsync(async (realmAccess, storage) =>
            {
                using var source = new TemporaryNativeStorage("void-skin-source");
                string root = source.GetFullPath(string.Empty);
                byte[] data = System.Text.Encoding.UTF8.GetBytes("[General]\nName: Fixture skin\nAuthor: Fixture\n");
                string hash = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
                string asset = Path.Combine(root, "files", hash[..1], hash[..2], hash);
                Directory.CreateDirectory(Path.GetDirectoryName(asset)!);
                File.WriteAllBytes(asset, data);
                string database = Path.Combine(root, OsuGameBase.CLIENT_DATABASE_FILENAME);
                using (var realm = Realm.GetInstance(new RealmConfiguration(database) { SchemaVersion = 999 }))
                {
                    realm.Write(() =>
                    {
                        var file = realm.Add(new RealmFile { Hash = hash });
                        var skin = new SkinInfo("Fixture skin", "Fixture");
                        skin.Files.Add(new RealmNamedFileUsage(file, "skin.ini"));
                        realm.Add(skin);
                        var deleted = new SkinInfo("Deleted") { DeletePending = true };
                        deleted.Files.Add(new RealmNamedFileUsage(file, "skin.ini"));
                        realm.Add(deleted);
                        var builtIn = new SkinInfo("Built-in") { Protected = true };
                        builtIn.Files.Add(new RealmNamedFileUsage(file, "skin.ini"));
                        realm.Add(builtIn);
                    });
                }
                byte[] before = File.ReadAllBytes(database);
                var beatmaps = new Mock<IModelImporter<BeatmapSetInfo>>(MockBehavior.Strict);
                var skins = new SkinImporter(storage, realmAccess, null!);
                var importer = new LazerLibraryImporter(beatmaps.Object, skins);
                var result = await importer.ImportAsync(root, storage.GetFullPath(string.Empty), selection: new LazerLibraryImportSelection { Beatmaps = false, Skins = true });
                Assert.That(result.SkinsImported, Is.EqualTo(1));
                Assert.That(result.Failed, Is.Zero);
                realmAccess.Run(realm =>
                {
                    var skin = realm.All<SkinInfo>().Single();
                    Assert.That(skin.Name, Is.EqualTo("Fixture skin"));
                    Assert.That(skin.Creator, Is.EqualTo("Fixture"));
                    Assert.That(skin.Files.Any(f => f.Filename == "skin.ini"), Is.True);
                    Assert.That(realm.All<BeatmapSetInfo>().Count(), Is.Zero);
                });
                beatmaps.VerifyNoOtherCalls();
                Assert.That(File.ReadAllBytes(database), Is.EqualTo(before));
                Assert.That(File.ReadAllBytes(asset), Is.EqualTo(data));
            });
        }
    }
}
