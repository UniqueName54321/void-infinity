// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Replays;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Replays;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Mods;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Replays;
using osuTK;

namespace osu.Game.Tests.Mods
{
    [TestFixture]
    public class AutoplayCustomizationTest
    {
        private static OsuBeatmap osuMap()
        {
            var map = new OsuBeatmap();
            map.HitObjects.Add(new HitCircle { StartTime = 1000, Position = new Vector2(100, 150) });
            map.HitObjects.Add(new HitCircle { StartTime = 2000, Position = new Vector2(400, 150) });
            map.HitObjects.Add(new HitCircle { StartTime = 3000, Position = new Vector2(100, 150) });
            defaults(map);
            return map;
        }

        private static void defaults(IBeatmap map)
        {
            foreach (var h in map.HitObjects) h.ApplyDefaults(map.ControlPointInfo, map.Difficulty);
        }

        private static Replay generate(ModAutoplay mod, IBeatmap map) => mod.CreateReplayData(map, Array.Empty<Mod>()).Replay;

        [Test]
        public void TestCursorDanceIsDeterministicAndPreservesClickPositions()
        {
            var map = osuMap();
            var dance = new OsuModAutoplay();
            dance.CursorStyle.Value = AutoplayCursorStyle.Dance;
            var frames = generate(dance, map).Frames.Cast<OsuReplayFrame>().ToArray();
            Assert.That(frames.Any(f => f.Time > 1100 && f.Time < 1900 && Math.Abs(f.Position.Y - 150) > 1), Is.True);
            foreach (var h in map.HitObjects)
                Assert.That(frames.Any(f => f.Time == h.StartTime && f.Position == h.Position && f.Actions.Count > 0), Is.True);
            Assert.That(generate(dance, map).Frames.Cast<OsuReplayFrame>().Select(f => f.Position), Is.EqualTo(frames.Select(f => f.Position)));
            dance.Seed.Value = 7;
            Assert.That(generate(dance, map).Frames.Cast<OsuReplayFrame>().Select(f => f.Position), Is.Not.EqualTo(frames.Select(f => f.Position)));
        }

        [Test]
        public void TestTimingOffsetAndAlternation()
        {
            var map = osuMap();
            var mod = new OsuModAutoplay();
            mod.TappingStyle.Value = AutoplayTapStyle.Alternate;
            var baseline = generate(mod, map);
            mod.TimingOffset.Value = 10;
            var shifted = generate(mod, map);
            Assert.That(shifted.Frames.Select(f => f.Time), Is.EqualTo(baseline.Frames.Select(f => f.Time + 10)));
            Assert.That(shifted.Frames.Cast<OsuReplayFrame>().Where(f => f.Actions.Count > 0).SelectMany(f => f.Actions).Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void TestTaikoSingleHandAndStrongHits()
        {
            var map = new TaikoBeatmap();
            map.HitObjects.Add(new Hit { StartTime = 1000 });
            map.HitObjects.Add(new Hit { StartTime = 2000, IsStrong = true });
            map.HitObjects.Add(new Hit { StartTime = 3000 });
            defaults(map);
            var mod = new TaikoModAutoplay();
            mod.AlternateHands.Value = false;
            mod.StartRight.Value = true;
            mod.TimingOffset.Value = -5;
            var frames = generate(mod, map).Frames.Cast<TaikoReplayFrame>().ToArray();
            Assert.That(frames.Single(f => f.Time == 995).Actions, Is.EqualTo(new[] { osu.Game.Rulesets.Taiko.TaikoAction.RightCentre }));
            Assert.That(frames.Single(f => f.Time == 1995).Actions.Count, Is.EqualTo(2));
            Assert.That(frames.Single(f => f.Time == 2995).Actions, Is.EqualTo(new[] { osu.Game.Rulesets.Taiko.TaikoAction.RightCentre }));
        }

        [Test]
        public void TestManiaTapDurationAndChordSpread()
        {
            var map = new ManiaBeatmap(new StageDefinition(4));
            map.HitObjects.Add(new Note { StartTime = 1000, Column = 0 });
            map.HitObjects.Add(new Note { StartTime = 1000, Column = 1 });
            map.HitObjects.Add(new HoldNote { StartTime = 2000, EndTime = 2500, Column = 2 });
            defaults(map);
            var mod = new ManiaModAutoplay();
            mod.TapDuration.Value = 80;
            mod.ChordSpread.Value = 10;
            var frames = generate(mod, map).Frames.Cast<ManiaReplayFrame>().ToArray();
            Assert.That(frames.Any(f => f.Time == 1010 && f.Actions.Count == 2), Is.True);
            Assert.That(frames.Any(f => f.Time == 1090 && f.Actions.Count == 0), Is.True);
            Assert.That(frames.Single(f => f.Time == 2500).Actions.Count, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void TestHumanPlayIsDeterministicImperfectAndStillMarkedAsAutoplay(int mode)
        {
            ModAutoplay mod;
            IBeatmap map;
            switch (mode)
            {
                case 0:
                    mod = new OsuModAutoplay(); map = osuMap(); break;
                case 1:
                    mod = new TaikoModAutoplay();
                    map = new TaikoBeatmap { HitObjects = { new Hit { StartTime = 1000 }, new Hit { StartTime = 2000 } } }; break;
                case 2:
                    mod = new ManiaModAutoplay();
                    map = new ManiaBeatmap(new StageDefinition(4)) { HitObjects = { new Note { StartTime = 1000 }, new Note { StartTime = 2000, Column = 1 } } }; break;
                default:
                    mod = new CatchModAutoplay();
                    map = new CatchBeatmap { HitObjects = { new Fruit { StartTime = 1000, X = 100 }, new Fruit { StartTime = 2000, X = 450 } } }; break;
            }
            defaults(map);
            var perfectTimes = generate(mod, map).Frames.Select(f => f.Time).ToArray();
            mod.HumanPlay.Value = true;
            var human = generate(mod, map);
            Assert.That(human.Frames.Select(f => f.Time), Is.Not.EqualTo(perfectTimes));
            Assert.That(human.Frames.Select(f => f.Time), Is.Ordered);
            Assert.That(generate(mod, map).Frames.Select(f => f.Time), Is.EqualTo(human.Frames.Select(f => f.Time)));
            Assert.That(mod.UserPlayable, Is.False);
            Assert.That(mod.ValidForMultiplayer, Is.False);
            Assert.That(mod.GetSettingsSourceProperties().Count(p => p.Item2.Name == "HumanPlay"), Is.EqualTo(1));
            Assert.That(((ModAutoplay)mod.DeepClone()).HumanPlay.Value, Is.True);
        }

        [Test]
        public void TestHumanCursorHasSeededArcsAndRestingDrift()
        {
            var mod = new OsuModAutoplay();
            mod.HumanPlay.Value = true;
            var frames = generate(mod, osuMap()).Frames.Cast<OsuReplayFrame>().ToArray();
            Assert.That(frames.Where(f => f.Time > 1100 && f.Time < 1900).Max(f => Math.Abs(f.Position.Y - 150)), Is.GreaterThan(8));
            Assert.That(frames.Where(f => f.Time < 0).Select(f => f.Position).Distinct().Count(), Is.GreaterThan(3));
            Assert.That(generate(mod, osuMap()).Frames.Cast<OsuReplayFrame>().Select(f => f.Position), Is.EqualTo(frames.Select(f => f.Position)));
            mod.Seed.Value = 72;
            Assert.That(generate(mod, osuMap()).Frames.Cast<OsuReplayFrame>().Select(f => f.Position), Is.Not.EqualTo(frames.Select(f => f.Position)));
            Assert.That(frames.All(f => float.IsFinite(f.Position.X) && float.IsFinite(f.Position.Y)), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(72)]
        public void TestHumanManiaDenseJacksKeepPressReleasePairs(int seed)
        {
            var map = new ManiaBeatmap(new StageDefinition(4));
            for (int i = 0; i < 30; i++) map.HitObjects.Add(new Note { StartTime = 1000 + i * 50, Column = 0 });
            defaults(map);
            var mod = new ManiaModAutoplay();
            mod.HumanPlay.Value = true;
            mod.Seed.Value = seed;
            var frames = generate(mod, map).Frames.Cast<ManiaReplayFrame>().ToArray();
            Assert.That(frames.Select(f => f.Time), Is.Ordered);
            Assert.That(frames.Count(f => f.Actions.Count > 0), Is.EqualTo(30));
            Assert.That(frames.Count(f => f.Actions.Count == 0), Is.EqualTo(30));
        }

        [Test]
        public void TestSpinnerSpeedAndDirection()
        {
            var map = new OsuBeatmap();
            map.HitObjects.Add(new Spinner { StartTime = 1000, EndTime = 3000 });
            defaults(map);
            var mod = new OsuModAutoplay();
            mod.SpinnerDirection.Value = AutoplaySpinDirection.Clockwise;
            var clockwise = generate(mod, map).Frames.Cast<OsuReplayFrame>().First(f => f.Time > 1000);
            mod.SpinnerDirection.Value = AutoplaySpinDirection.Counterclockwise;
            var counterclockwise = generate(mod, map).Frames.Cast<OsuReplayFrame>().First(f => f.Time > 1000);
            Assert.That(counterclockwise.Position, Is.Not.EqualTo(clockwise.Position));
            mod.SpinnerRPM.Value = 700;
            Assert.That(generate(mod, map).Frames.Cast<OsuReplayFrame>().First(f => f.Time > 1000).Position, Is.Not.EqualTo(counterclockwise.Position));
        }

        [Test]
        public void TestExtraSingleHandRollHitsReleaseBetweenPresses()
        {
            var map = new TaikoBeatmap();
            map.HitObjects.Add(new DrumRoll { StartTime = 1000, Duration = 1000 });
            defaults(map);
            var mod = new TaikoModAutoplay();
            var baseline = generate(mod, map).Frames.Cast<TaikoReplayFrame>().Count(f => f.Actions.Count > 0);
            mod.ExtraRollHits.Value = 2;
            mod.AlternateHands.Value = false;
            var frames = generate(mod, map).Frames.Cast<TaikoReplayFrame>().ToArray();
            Assert.That(frames.Count(f => f.Actions.Count > 0), Is.GreaterThan(baseline));
            for (int i = 1; i < frames.Length; i++)
                if (frames[i].Actions.Count > 0)
                    Assert.That(frames[i - 1].Actions.Count, Is.Zero);
        }

        [Test]
        public void TestUniversalSettingsBelongToPlayableAutoplayModes()
        {
            ModAutoplay[] mods = { new OsuModAutoplay(), new TaikoModAutoplay(), new ManiaModAutoplay(), new CatchModAutoplay() };
            foreach (var mod in mods)
                Assert.That(mod.GetSettingsSourceProperties().Select(s => s.Item2.Name), Does.Contain("TimingOffset").And.Contain("Seed"));
            // Cinema inherits ModAutoplay but does not apply these controls to its separate replay pipeline.
            Assert.That(new OsuModCinema().GetSettingsSourceProperties().Any(s => s.Item2.Name == "TimingOffset"), Is.False);
        }

        [Test]
        public void TestCustomSettingsSurviveCloning()
        {
            var original = new OsuModAutoplay();
            original.CursorStyle.Value = AutoplayCursorStyle.Dance;
            original.DanceStrength.Value = 90;
            original.TimingOffset.Value = 5;
            var clone = (OsuModAutoplay)original.DeepClone();
            original.DanceStrength.Value = 10;
            Assert.That(clone.CursorStyle.Value, Is.EqualTo(AutoplayCursorStyle.Dance));
            Assert.That(clone.DanceStrength.Value, Is.EqualTo(90));
            Assert.That(clone.TimingOffset.Value, Is.EqualTo(5));
        }

        [Test]
        public void TestCatchAimAndDash()
        {
            var map = new CatchBeatmap();
            map.HitObjects.Add(new Fruit { StartTime = 1000, X = 450 });
            defaults(map);
            var mod = new CatchModAutoplay();
            mod.AimOffset.Value = 0.5;
            mod.AlwaysDash.Value = true;
            mod.MoveEarly.Value = true;
            var frames = generate(mod, map).Frames.Cast<CatchReplayFrame>().ToArray();
            Assert.That(frames.Last().Position, Is.GreaterThan(450));
            Assert.That(frames.All(f => f.Dashing), Is.True);
        }
    }
}
