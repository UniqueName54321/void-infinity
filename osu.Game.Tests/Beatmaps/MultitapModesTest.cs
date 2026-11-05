// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Objects;
using osuTK;
using Decoder = osu.Game.Beatmaps.Formats.Decoder;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class MultitapModesTest
    {
        public static Ruleset RulesetFor(int mode) => mode switch
        {
            0 => new OsuRuleset(), 1 => new TaikoRuleset(), 2 => new CatchRuleset(), _ => new ManiaRuleset(),
        };

        public static Beatmap CreateMap(int mode, int count = 12)
        {
            HitObject group = mode switch
            {
                0 => new MultitapCircle { Position = new Vector2(256, 192) },
                1 => new MultitapHit { Type = HitType.Rim, IsStrong = true },
                2 => new MultitapFruit { X = 256 },
                _ => new MultitapNote { Column = 1 },
            };
            group.StartTime = 1000;
            group.Multitap = new MultitapSettings { Count = count, Subdivision = 4 };
            var map = new Beatmap { HitObjects = { group } };
            map.BeatmapInfo.Ruleset = RulesetFor(mode).RulesetInfo;
            map.Difficulty.CircleSize = mode == 3 ? 4 : 5;
            return map;
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void TestNativeRoundTripAndNoMultis(int mode)
        {
            var source = CreateMap(mode);
            var ruleset = RulesetFor(mode);
            using var writer = new StringWriter();
            new VibBeatmapEncoder(source).Encode(writer);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(writer.ToString()));
            using var reader = new LineBufferedReader(stream);
            var decoded = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
            var working = new TestWorkingBeatmap(decoded);
            var grouped = working.GetPlayableBeatmap(ruleset.RulesetInfo);
            Assert.That(grouped.HitObjects.Single(), Is.InstanceOf<IHasMultitap>());
            Assert.That(grouped.HitObjects.Single().NestedHitObjects.Count, Is.EqualTo(12));
            var expectedTimes = source.HitObjects.Single().Multitap.GetTapTimes(1000, source.ControlPointInfo);
            Assert.That(grouped.HitObjects.Single().NestedHitObjects.Select(h => h.StartTime), Is.EqualTo(expectedTimes));
            var plain = working.GetPlayableBeatmap(ruleset.RulesetInfo, new Mod[] { new ModNoMultis() });
            Assert.That(plain.HitObjects.Count, Is.EqualTo(12));
            Assert.That(plain.HitObjects.All(h => h.Multitap == null && h is not IHasMultitap), Is.True);
            Assert.That(plain.HitObjects.Select(h => h.StartTime), Is.EqualTo(expectedTimes));
            if (mode == 1) Assert.That(plain.HitObjects.Cast<Hit>().All(h => h.Type == HitType.Rim && h.IsStrong), Is.True);
            if (mode == 3) Assert.That(plain.HitObjects.Cast<Note>().All(h => h.Column == 1), Is.True);
            Assert.That(decoded.HitObjects.Count, Is.EqualTo(1));
            Assert.That(decoded.HitObjects.Single().Multitap.Count, Is.EqualTo(12));
            Assert.That(ruleset.GetModsFor(ModType.Conversion).Any(m => m is ModNoMultis), Is.True);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void TestRatingUsesEveryTap(int mode)
        {
            var ruleset = RulesetFor(mode);
            var working = new TestWorkingBeatmap(CreateMap(mode, 64));
            var calculator = ruleset.CreateDifficultyCalculator(working);
            var grouped = calculator.Calculate();
            var plain = calculator.Calculate(new Mod[] { new ModNoMultis() });
            Assert.That(double.IsFinite(grouped.StarRating), Is.True);
            Assert.That(grouped.MaxCombo, Is.GreaterThanOrEqualTo(64));
            Assert.That(grouped.MaxCombo, Is.EqualTo(plain.MaxCombo));
            Assert.That(grouped.StarRating, Is.EqualTo(plain.StarRating).Within(0.05));
            if (mode != 2)
            {
                var easy = new TestWorkingBeatmap(CreateMap(mode, 2));
                var easyRating = ruleset.CreateDifficultyCalculator(easy).Calculate();
                Assert.That(grouped.StarRating, Is.GreaterThan(easyRating.StarRating));
            }
        }

        [Test]
        public void TestMirrorHardRockAndTaikoColourModsReachChildNotes()
        {
            var mania = new TestWorkingBeatmap(CreateMap(3)).GetPlayableBeatmap(RulesetFor(3).RulesetInfo,
                new Mod[] { new osu.Game.Rulesets.Mania.Mods.ManiaModMirror() });
            Assert.That(mania.HitObjects.Single().NestedHitObjects.Cast<Note>().All(n => n.Column == 2), Is.True);
            var standard = new TestWorkingBeatmap(CreateMap(0)).GetPlayableBeatmap(RulesetFor(0).RulesetInfo,
                new Mod[] { new osu.Game.Rulesets.Osu.Mods.OsuModHardRock() });
            var parent = (MultitapCircle)standard.HitObjects.Single();
            Assert.That(parent.NestedHitObjects.Cast<HitCircle>().All(n => n.Position == parent.Position), Is.True);
            var swap = new TestWorkingBeatmap(CreateMap(1)).GetPlayableBeatmap(RulesetFor(1).RulesetInfo,
                new Mod[] { new osu.Game.Rulesets.Taiko.Mods.TaikoModSwap() });
            Assert.That(swap.HitObjects.Single().NestedHitObjects.Cast<Hit>().All(n => n.Type == HitType.Centre && n.IsStrong), Is.True);
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void TestStandardMultitapConvertsWithoutBecomingAHoldOrSpinner(int mode)
        {
            var working = new TestWorkingBeatmap(CreateMap(0));
            var converted = working.GetPlayableBeatmap(RulesetFor(mode).RulesetInfo);
            Assert.That(converted.HitObjects.Single(), Is.InstanceOf<IHasMultitap>());
            Assert.That(converted.HitObjects.Single().NestedHitObjects.Count, Is.EqualTo(12));
        }
    }
}
