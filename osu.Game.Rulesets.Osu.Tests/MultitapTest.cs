// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using Decoder = osu.Game.Beatmaps.Formats.Decoder;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public class MultitapTest
    {
        [Test]
        public void TestTempoChangesInsideTapInterval()
        {
            var timing = new ControlPointInfo();
            timing.Add(0, new TimingControlPoint { BeatLength = 500 });
            timing.Add(1062.5, new TimingControlPoint { BeatLength = 250 });
            var rhythm = new MultitapSettings { Count = 4, Subdivision = 4 };
            Assert.That(rhythm.GetTapTimes(1000, timing), Is.EqualTo(new[] { 1000d, 1093.75, 1156.25, 1218.75 }));
        }

        [Test]
        public void TestTimeSignatureAndSubdivision()
        {
            var rhythm = new MultitapSettings { Count = 4, BeatsPerBar = 3, BeatUnit = 8, Subdivision = 2 };
            var timing = new ControlPointInfo();
            timing.Add(0, new TimingControlPoint { BeatLength = 500 });
            Assert.That(rhythm.GetTapTimes(1000, timing), Is.EqualTo(new[] { 1000d, 1125, 1250, 1375 }));
        }

        [TestCase(0, 4, 4, 4)]
        [TestCase(1025, 4, 4, 4)]
        [TestCase(2, 0, 4, 4)]
        [TestCase(2, 4, 3, 4)]
        [TestCase(2, 4, 4, 0)]
        public void TestRejectsInvalidRhythms(int count, int beats, int unit, int subdivision)
            => Assert.Throws<InvalidDataException>(() => new MultitapSettings { Count = count, BeatsPerBar = beats, BeatUnit = unit, Subdivision = subdivision }.Validate());

        [Test]
        public void TestNativeRoundTripAndNestedTapDefaults()
        {
            var map = new OsuBeatmap();
            map.BeatmapInfo.Ruleset = new OsuRuleset().RulesetInfo;
            map.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            map.HitObjects.Add(new BeatmapEvent { StartTime = 500 });
            map.HitObjects.Add(new HitCircle { StartTime = 750, Position = new Vector2(50) });
            var multi = new MultitapCircle { StartTime = 1000, Position = new Vector2(200), Multitap = new MultitapSettings { Count = 3, BeatsPerBar = 3, BeatUnit = 8, Subdivision = 2 } };
            multi.Multitap.Samples = new[] { HitSampleInfo.HIT_NORMAL, HitSampleInfo.HIT_CLAP, HitSampleInfo.HIT_WHISTLE }
                .Select(name => new System.Collections.Generic.List<HitSampleInfo> { new HitSampleInfo(name) }).ToList();
            map.HitObjects.Add(multi);
            multi.ApplyDefaults(map.ControlPointInfo, map.Difficulty);
            using var writer = new StringWriter();
            new VibBeatmapEncoder(map).Encode(writer);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(writer.ToString()));
            using var reader = new LineBufferedReader(stream);
            var decoded = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
            var converted = new OsuBeatmapConverter(decoded, new OsuRuleset()).Convert();
            var restored = converted.HitObjects.OfType<MultitapCircle>().Single();
            restored.ApplyDefaults(converted.ControlPointInfo, converted.Difficulty);
            Assert.That(converted.HitObjects.Count, Is.EqualTo(3));
            Assert.That(restored.Multitap.BeatsPerBar, Is.EqualTo(3));
            Assert.That(restored.Multitap.BeatUnit, Is.EqualTo(8));
            Assert.That(restored.NestedHitObjects.Select(h => h.Samples.Single().Name), Is.EqualTo(new[] { HitSampleInfo.HIT_NORMAL, HitSampleInfo.HIT_CLAP, HitSampleInfo.HIT_WHISTLE }));
            Assert.That(restored.NestedHitObjects.Select(h => h.StartTime), Is.EqualTo(new[] { 1000d, 1125, 1250 }));
            Assert.That(restored.NestedHitObjects.All(h => h.CreateJudgement().MaxResult == osu.Game.Rulesets.Scoring.HitResult.Great), Is.True);
            Assert.That(restored.CreateJudgement().MaxResult, Is.EqualTo(osu.Game.Rulesets.Scoring.HitResult.IgnoreHit));
            restored.StartTime += 500;
            Assert.That(restored.EndTime, Is.EqualTo(1750));
            Assert.That(restored.NestedHitObjects.Last().StartTime, Is.EqualTo(1750));
            restored.Position = new Vector2(300);
            restored.StackHeight = 2;
            Assert.That(restored.NestedHitObjects.Cast<HitCircle>().All(h => h.Position == restored.Position && h.StackHeight == 2), Is.True);
        }

        [Test]
        public void TestConversionPreservesTimesAndSoundsAndRefusesLossySelections()
        {
            var timing = new ControlPointInfo();
            timing.Add(0, new TimingControlPoint { BeatLength = 500 });
            var circles = Enumerable.Range(0, 5).Select(i => new HitCircle { StartTime = 1000 + i * 125, Position = new Vector2(200),
                Samples = new[] { new HitSampleInfo(i % 2 == 0 ? HitSampleInfo.HIT_NORMAL : HitSampleInfo.HIT_CLAP) } }).ToArray();
            var converted = MultitapConversion.FromStream(circles.Reverse().ToArray(), timing)!;
            converted.ApplyDefaults(timing, new BeatmapDifficulty());
            Assert.That(converted.NestedHitObjects.Select(h => h.StartTime), Is.EqualTo(circles.Select(h => h.StartTime)));
            Assert.That(converted.NestedHitObjects.Select(h => h.Samples.Single().Name), Is.EqualTo(circles.Select(h => h.Samples.Single().Name)));
            circles[2].StartTime += 10;
            Assert.That(MultitapConversion.FromStream(circles, timing), Is.Null);
            circles[2].StartTime -= 10;
            circles[2].Position += Vector2.UnitX;
            Assert.That(MultitapConversion.FromStream(circles, timing), Is.Null);
            circles[2].Position -= Vector2.UnitX;
            circles[2].NewCombo = true;
            Assert.That(MultitapConversion.FromStream(circles, timing), Is.Null);
        }
    }
}
