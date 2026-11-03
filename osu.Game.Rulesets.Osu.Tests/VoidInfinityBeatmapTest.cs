// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Skinning;
using osuTK;
using Decoder = osu.Game.Beatmaps.Formats.Decoder;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public class VoidInfinityBeatmapTest
    {
        private static Slider slider(params double[] velocities) => new Slider
        {
            StartTime = 1000,
            Position = new Vector2(100),
            RepeatCount = 1,
            Path = new SliderPath(velocities.Select((v, i) => new PathControlPoint(new Vector2(i * 100, 0), i == 0 ? PathType.LINEAR : null)
            {
                Velocity = v,
            }).ToArray())
        };

        [Test]
        public void TestMultipleVelocityChangesAndReverseSpans()
        {
            var s = slider(1, 0.5, 2, 0.75);
            s.ApplyDefaults(new osu.Game.Beatmaps.ControlPoints.ControlPointInfo(), new BeatmapDifficulty());
            var profile = s.VelocityProfile;
            for (int i = 0; i <= 100; i++)
            {
                double progress = i / 100d;
                Assert.That(profile.ProgressAt(profile.TimeAt(progress)), Is.EqualTo(progress).Within(1e-8));
                Assert.That(s.PathProgressAt((1 + profile.TimeAt(progress)) / 2), Is.EqualTo(profile.ProgressAt(1 - profile.TimeAt(progress))).Within(1e-8));
            }
            Assert.That(s.PathProgressAt(0.5), Is.EqualTo(1));
            Assert.That(s.PathProgressAt(1), Is.EqualTo(0));
            foreach (var tick in s.NestedHitObjects.OfType<SliderTick>())
                Assert.That(s.PathProgressAt((tick.StartTime - s.StartTime) / s.Duration), Is.EqualTo(tick.PathProgress).Within(1e-8));
        }

        [Test]
        public void TestSlowingDownIncreasesDuration()
        {
            var s = slider(1, 0.5);
            var profile = s.VelocityProfile;
            Assert.That(profile.DurationMultiplier, Is.EqualTo(2 * Math.Log(2)).Within(1e-8));
            Assert.That(profile.ProgressAt(0.5), Is.GreaterThan(0.5));
            s.Path.ControlPoints[1].Velocity = 1;
            Assert.That(s.VelocityProfile.DurationMultiplier, Is.EqualTo(1));
        }

        [Test]
        public void TestMovementAndRewindDoNotMutateAuthoredPath()
        {
            var s = slider(1, 1);
            s.Path.ControlPoints[1].Movement = new Vector2(0, 64);
            Assert.That(s.PathAt(0.5).ControlPoints[1].Position, Is.EqualTo(new Vector2(100, 32)));
            Assert.That(s.PathAt(1).ControlPoints[1].Position, Is.EqualTo(new Vector2(100, 64)));
            Assert.That(s.PathAt(0).ControlPoints[1].Position, Is.EqualTo(new Vector2(100, 0)));
            Assert.That(s.Path.ControlPoints[1].Position, Is.EqualTo(new Vector2(100, 0)));
        }

        [Test]
        public void TestMovingTrimmedPathStartsAtAuthoredLength()
        {
            var s = slider(1, 1);
            s.Path.ExpectedDistance.Value = 50;
            s.Path.ControlPoints[1].Movement = new Vector2(0, 64);
            Assert.That(s.PathAt(0).Distance, Is.EqualTo(s.Path.Distance).Within(1e-6));
            Assert.That(s.PathAt(0).PositionAt(1), Is.EqualTo(s.Path.PositionAt(1)));
        }

        [Test]
        public void TestNativeRoundTripPreservesExtensionsAndEvents()
        {
            var map = new OsuBeatmap { ClampSliderMovement = false };
            map.BeatmapInfo.Ruleset = new OsuRuleset().RulesetInfo;
            var s = slider(1, 0.5, 2);
            s.Path.ControlPoints[1].Movement = new Vector2(20, -30);
            map.HitObjects.Add(s);
            map.HitObjects.Add(new BeatmapEvent { StartTime = 1100, Position = new Vector2(200), Change = new BeatmapAttributeChange { CircleSize = 7, SliderMultiplier = 2 } });
            s.ApplyDefaults(map.ControlPointInfo, map.Difficulty);
            using var writer = new StringWriter();
            new VibBeatmapEncoder(map).Encode(writer);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(writer.ToString()));
            using var reader = new LineBufferedReader(stream);
            var decoded = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
            var converted = new OsuBeatmapConverter(decoded, new OsuRuleset()).Convert();
            Assert.That(converted.ClampSliderMovement, Is.False);
            Assert.That(converted.HitObjects.Count, Is.EqualTo(2));
            var restored = converted.HitObjects.OfType<Slider>().Single();
            Assert.That(restored.Path.ControlPoints.Select(p => p.Velocity), Is.EqualTo(new[] { 1d, 0.5, 2 }));
            Assert.That(restored.Path.ControlPoints[1].Movement, Is.EqualTo(new Vector2(20, -30)));
            Assert.That(restored.Path.ControlPoints[0].Type, Is.EqualTo(PathType.LINEAR));
            Assert.That(converted.HitObjects.OfType<BeatmapEvent>().Single().Change.CircleSize, Is.EqualTo(7));
        }

        [Test]
        public void TestAttributeTimelinePersistsChangesAndSupportsSeeking()
        {
            var map = new OsuBeatmap();
            map.HitObjects.Add(new BeatmapEvent { StartTime = 1000, Change = new BeatmapAttributeChange { CircleSize = 7, SliderMultiplier = 2 } });
            map.HitObjects.Add(new BeatmapEvent { StartTime = 2000, Change = new BeatmapAttributeChange { ApproachRate = 9 } });
            var timeline = new BeatmapAttributeTimeline(map);
            Assert.That(timeline.At(2000).CircleSize, Is.EqualTo(7));
            Assert.That(timeline.At(2000).ApproachRate, Is.EqualTo(9));
            Assert.That(timeline.At(1500).SliderMultiplier, Is.EqualTo(2));
            Assert.That(timeline.At(999).CircleSize, Is.EqualTo(map.Difficulty.CircleSize));
            Assert.That(map.Difficulty.CircleSize, Is.EqualTo(5));
        }

        [Test]
        public void TestUndoRedoPreservesMovementVelocityAndEvents()
        {
            var map = new OsuBeatmap();
            map.BeatmapInfo.Ruleset = new OsuRuleset().RulesetInfo;
            var initial = slider(1, 1);
            initial.ApplyDefaults(map.ControlPointInfo, map.Difficulty);
            map.HitObjects.Add(initial);
            using var editor = new osu.Game.Screens.Edit.EditorBeatmap(map);
            using var history = new osu.Game.Screens.Edit.BeatmapEditorChangeHandler(editor);
            history.SaveState();
            editor.BeginChange();
            initial.Path.ControlPoints[1].Velocity = 0.5;
            initial.Path.ControlPoints[1].Movement = new Vector2(0, 50);
            editor.ClampSliderMovement = false;
            editor.Add(new BeatmapEvent { StartTime = 1050, Change = new BeatmapAttributeChange { CircleSize = 8 } });
            editor.UpdateAllHitObjects();
            editor.EndChange();
            history.RestoreState(-1);
            Assert.That(editor.HitObjects.Count, Is.EqualTo(1));
            var restored = editor.HitObjects.OfType<Slider>().Single();
            Assert.That(restored.Path.ControlPoints[1].Velocity, Is.EqualTo(1));
            Assert.That(restored.Path.ControlPoints[1].Movement, Is.EqualTo(Vector2.Zero));
            Assert.That(editor.ClampSliderMovement, Is.True);
            history.RestoreState(1);
            Assert.That(editor.HitObjects.OfType<BeatmapEvent>().Single().Change.CircleSize, Is.EqualTo(8));
            restored = editor.HitObjects.OfType<Slider>().Single();
            Assert.That(restored.Path.ControlPoints[1].Velocity, Is.EqualTo(0.5));
            Assert.That(restored.Path.ControlPoints[1].Movement, Is.EqualTo(new Vector2(0, 50)));
            Assert.That(editor.ClampSliderMovement, Is.False);
        }

        [Test]
        public void TestNativeStoryboardAndColours()
        {
            var map = new OsuBeatmap();
            map.BeatmapInfo.Ruleset = new OsuRuleset().RulesetInfo;
            map.UnhandledEventLines.Add("Video,0,\"video.avi\"");
            var skin = new LegacySkin(new SkinInfo(), null!);
            skin.Configuration.CustomComboColours.Add(osuTK.Graphics.Color4.Green);
            using var writer = new StringWriter();
            new VibBeatmapEncoder(map, skin).Encode(writer);
            byte[] data = Encoding.UTF8.GetBytes(writer.ToString());
            using var stream = new MemoryStream(data);
            using var reader = new LineBufferedReader(stream);
            var storyboard = Decoder.GetDecoder<osu.Game.Storyboards.Storyboard>(reader).Decode(reader);
            Assert.That(storyboard.Layers.Single(l => l.Name == "Video").Elements.Count, Is.EqualTo(1));
            using var skinStream = new MemoryStream(data);
            using var skinReader = new LineBufferedReader(skinStream);
            var restored = new LegacySkinDecoder().Decode(skinReader);
            Assert.That(restored.CustomComboColours.Single(), Is.EqualTo(osuTK.Graphics.Color4.Green));
        }

        [Test]
        public void TestPlayableDefaultsUseEvents()
        {
            var map = new OsuBeatmap();
            map.BeatmapInfo.Ruleset = new OsuRuleset().RulesetInfo;
            map.HitObjects.Add(new HitCircle { StartTime = 500 });
            map.HitObjects.Add(new BeatmapEvent { StartTime = 1000, Change = new BeatmapAttributeChange { CircleSize = 8, ApproachRate = 9 } });
            map.HitObjects.Add(new HitCircle { StartTime = 1000 });
            var working = new osu.Game.Tests.Beatmaps.TestWorkingBeatmap(map);
            var playable = working.GetPlayableBeatmap(map.BeatmapInfo.Ruleset);
            var circles = playable.HitObjects.OfType<HitCircle>().ToArray();
            Assert.That(circles[1].Scale, Is.LessThan(circles[0].Scale));
            Assert.That(circles[1].TimePreempt, Is.LessThan(circles[0].TimePreempt));
            Assert.That(circles[1].IndexInCurrentCombo, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void TestInvalidVelocityRejected(double value)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new PathControlPoint { Velocity = value });

        [Test]
        public void TestNativeDefaultVelocityIsOne()
        {
            var map = new OsuBeatmap();
            map.BeatmapInfo.Ruleset = new OsuRuleset().RulesetInfo;
            map.HitObjects.Add(slider(1, 1));
            map.HitObjects[0].ApplyDefaults(map.ControlPointInfo, map.Difficulty);
            using var writer = new StringWriter();
            new VibBeatmapEncoder(map).Encode(writer);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(writer.ToString()));
            using var reader = new LineBufferedReader(stream);
            var decoded = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
            Assert.That(decoded.HitObjects.OfType<IHasPath>().Single().Path.ControlPoints.All(p => p.Velocity == 1), Is.True);
            Assert.That(decoded.ClampSliderMovement, Is.True);
        }
    }
}
