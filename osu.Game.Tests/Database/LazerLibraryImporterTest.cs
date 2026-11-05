// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Models;
using Realms;

namespace osu.Game.Tests.Database
{
    [TestFixture]
    public class LazerLibraryImporterTest : RealmTest
    {
        [Test]
        public void TestReadsDifferentSchemaWithoutChangingDatabaseAndRestoresAssetNames()
        {
            string root = Path.Combine(Path.GetTempPath(), "void-library-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                byte[] content = System.Text.Encoding.UTF8.GetBytes("osu file format v14\n[Metadata]\nTitle:Fixture\n");
                string hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
                string asset = Path.Combine(root, "files", hash[..1], hash[..2], hash);
                Directory.CreateDirectory(Path.GetDirectoryName(asset)!);
                File.WriteAllBytes(asset, content);
                string database = Path.Combine(root, "client.realm");
                using (var realm = Realm.GetInstance(new RealmConfiguration(database) { SchemaVersion = 999 }))
                {
                    realm.Write(() =>
                    {
                        var file = realm.Add(new RealmFile { Hash = hash });
                        var set = new BeatmapSetInfo();
                        set.Files.Add(new RealmNamedFileUsage(file, "difficulty.osu"));
                        realm.Add(set);
                        var deleted = new BeatmapSetInfo { DeletePending = true };
                        deleted.Files.Add(new RealmNamedFileUsage(file, "deleted.osu"));
                        realm.Add(deleted);
                    });
                }
                byte[] before = SHA256.HashData(File.ReadAllBytes(database));
                var sets = LazerLibraryImporter.ReadSets(database);
                Assert.That(sets.Count, Is.EqualTo(1));
                Assert.That(sets[0].Keys, Is.EqualTo(new[] { "difficulty.osu" }));
                string archive = Path.Combine(root, "export.vibz");
                LazerLibraryImporter.WriteArchive(root, sets.Single(), archive, CancellationToken.None);
                using (var zip = ZipFile.OpenRead(archive))
                {
                    using var entry = zip.GetEntry("difficulty.osu")!.Open();
                    using var restored = new MemoryStream();
                    entry.CopyTo(restored);
                    Assert.That(restored.ToArray(), Is.EqualTo(content));
                }
                Assert.That(SHA256.HashData(File.ReadAllBytes(database)), Is.EqualTo(before));
                Assert.That(File.ReadAllBytes(asset), Is.EqualTo(content));
            }
            finally { Directory.Delete(root, true); }
        }

        [Test]
        public void TestImportsSnapshotIntoAnotherLibraryAndDeduplicates()
        {
            RunTestWithRealmAsync(async (destinationRealm, storage) =>
            {
                using var rulesets = new osu.Game.Rulesets.RealmRulesetStore(destinationRealm, storage);
                var importer = new BeatmapImporter(storage, destinationRealm);
                bool processedLocally = false;
                importer.ProcessBeatmap = (_, scope) =>
                {
                    Assert.That(scope, Is.EqualTo(MetadataLookupScope.None));
                    processedLocally = true;
                };
                string sourceRoot = storage.GetFullPath("source-library");
                Directory.CreateDirectory(sourceRoot);
                string sourcePath = Path.Combine(sourceRoot, OsuGameBase.CLIENT_DATABASE_FILENAME);
                byte[] map = System.Text.Encoding.UTF8.GetBytes("osu file format v14\n[General]\nMode:0\n[Metadata]\nTitle:Import fixture\nArtist:Fixture\nCreator:Fixture\nVersion:Test\n[Difficulty]\nCircleSize:5\nOverallDifficulty:5\nSliderMultiplier:1.4\nSliderTickRate:1\n[TimingPoints]\n0,500,4,1,0,100,1,0\n[HitObjects]\n256,192,1000,1,0,0:0:0:0:\n");
                string hash = Convert.ToHexString(SHA256.HashData(map)).ToLowerInvariant();
                string asset = Path.Combine(sourceRoot, "files", hash[..1], hash[..2], hash);
                Directory.CreateDirectory(Path.GetDirectoryName(asset)!);
                File.WriteAllBytes(asset, map);
                using (var source = Realm.GetInstance(new RealmConfiguration(sourcePath) { SchemaVersion = 999 }))
                {
                    source.Write(() =>
                    {
                        var set = new BeatmapSetInfo();
                        set.Files.Add(new RealmNamedFileUsage(new RealmFile { Hash = hash }, "fixture.osu"));
                        source.Add(set);
                    });
                }
                byte[] original = File.ReadAllBytes(sourcePath);
                var libraryImporter = new LazerLibraryImporter(importer);
                var result = await libraryImporter.ImportAsync(sourceRoot, storage.GetFullPath(string.Empty));
                Assert.That(result.Imported, Is.EqualTo(1));
                Assert.That(result.Failed, Is.Zero);
                Assert.That(processedLocally, Is.True);
                await libraryImporter.ImportAsync(sourceRoot, storage.GetFullPath(string.Empty));
                destinationRealm.Run(realm =>
                {
                    var set = realm.All<BeatmapSetInfo>().Single();
                    Assert.That(set.Beatmaps.Count, Is.EqualTo(1));
                    Assert.That(set.Files.Single().Filename, Is.EqualTo("fixture.osu"));
                });
                Assert.That(File.ReadAllBytes(sourcePath), Is.EqualTo(original));
                Assert.That(File.ReadAllBytes(asset), Is.EqualTo(map));
                Assert.ThrowsAsync<InvalidOperationException>(() => libraryImporter.ImportAsync(sourceRoot, sourceRoot));
            });
        }

        [TestCase("../outside.osu")]
        [TestCase("/absolute.osu")]
        [TestCase("folder/../../outside.osu")]
        [TestCase("C:\\outside.osu")]
        [TestCase("folder\\..\\outside.osu")]
        public void TestRejectsUnsafePaths(string path) => Assert.That(LazerLibraryImporter.IsSafeFilename(path), Is.False);

        [TestCase("difficulty.osu")]
        [TestCase("背景/image.jpg")]
        [TestCase("folder\\image.jpg")]
        public void TestAcceptsRelativePaths(string path) => Assert.That(LazerLibraryImporter.IsSafeFilename(path), Is.True);

        [Test]
        public void TestMissingAssetFailsInsteadOfProducingPartialSet()
        {
            string root = Path.Combine(Path.GetTempPath(), "void-missing-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                Assert.Throws<FileNotFoundException>(() => LazerLibraryImporter.WriteArchive(root,
                    new Dictionary<string, string> { ["map.osu"] = new string('a', 64) }, Path.Combine(root, "map.vibz"), CancellationToken.None));
            }
            finally { Directory.Delete(root, true); }
        }
    }
}
