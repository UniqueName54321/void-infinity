// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using Decoder = osu.Game.Beatmaps.Formats.Decoder;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public class VibBeatmapCompatibilityTest
    {
        [Test]
        public void TestPreMultitapMapRetainsCirclesSlidersAndEvents()
        {
            var map = createMap();
            var slider = new Slider
            {
                StartTime = 2000, Position = new Vector2(100),
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(100, 0)) { Velocity = 0.5, Movement = new Vector2(0, 20) },
                }),
            };
            slider.ApplyDefaults(map.ControlPointInfo, map.Difficulty);
            map.HitObjects.Add(slider);
            map.HitObjects.Add(new BeatmapEvent { StartTime = 2500, Change = new BeatmapAttributeChange { CircleSize = 6 } });
            map.ClampSliderMovement = false;
            var document = encode(map);
            document.Remove("multitaps"); // This field did not exist before v0.1.2.2.

            var decoded = decode(document);
            var converted = new OsuBeatmapConverter(decoded, new OsuRuleset()).Convert();
            Assert.That(converted.HitObjects.Count, Is.EqualTo(3));
            Assert.That(converted.HitObjects.OfType<HitCircle>().Single().StartTime, Is.EqualTo(1000));
            var restoredSlider = converted.HitObjects.OfType<Slider>().Single();
            Assert.That(restoredSlider.Path.ControlPoints.Last().Velocity, Is.EqualTo(0.5));
            Assert.That(restoredSlider.Path.ControlPoints.Last().Movement, Is.EqualTo(new Vector2(0, 20)));
            Assert.That(converted.HitObjects.OfType<BeatmapEvent>().Single().Change.CircleSize, Is.EqualTo(6));
            Assert.That(converted.ClampSliderMovement, Is.False);
            Assert.That(converted.HitObjects.All(h => h.Multitap == null), Is.True);
        }

        [TestCase("multitaps", false)]
        [TestCase("multitaps", true)]
        [TestCase("events", false)]
        [TestCase("events", true)]
        [TestCase("paths", false)]
        [TestCase("paths", true)]
        public void TestAbsentOrNullOptionalCollections(string field, bool explicitNull)
        {
            var document = encode(createMap());
            if (explicitNull) document[field] = JValue.CreateNull();
            else document.Remove(field);

            var decoded = decode(document);
            Assert.That(decoded.HitObjects.Count, Is.EqualTo(1));
            Assert.That(decoded.HitObjects.Single().StartTime, Is.EqualTo(1000));
        }

        [Test]
        public void TestAllOptionalCollectionsAbsent()
        {
            var document = encode(createMap());
            document.Remove("multitaps");
            document.Remove("paths");
            document.Remove("events");
            document.Remove("clamp_slider_movement");
            var decoded = decode(document);
            Assert.That(decoded.HitObjects.Count, Is.EqualTo(1));
            Assert.That(decoded.ClampSliderMovement, Is.True);
        }

        [Test]
        public void TestMissingSliderExtensionStillReportsMismatchedData()
        {
            var map = createMap();
            var slider = new Slider
            {
                StartTime = 2000, Position = new Vector2(100),
                Path = new SliderPath(new[] { new PathControlPoint(Vector2.Zero, PathType.LINEAR), new PathControlPoint(new Vector2(100, 0)) }),
            };
            slider.ApplyDefaults(map.ControlPointInfo, map.Difficulty);
            map.HitObjects.Add(slider);
            var document = encode(map);
            document.Remove("paths");
            Assert.Throws<InvalidDataException>(() => decode(document));
        }

        private static OsuBeatmap createMap()
        {
            var map = new OsuBeatmap();
            map.BeatmapInfo.Ruleset = new OsuRuleset().RulesetInfo;
            map.HitObjects.Add(new HitCircle { StartTime = 1000, Position = new Vector2(256, 192) });
            return map;
        }

        private static JObject encode(OsuBeatmap map)
        {
            using var writer = new StringWriter();
            new VibBeatmapEncoder(map).Encode(writer);
            using var reader = new StringReader(writer.ToString());
            reader.ReadLine();
            return JObject.Parse(reader.ReadToEnd());
        }

        private static Beatmap decode(JObject document)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(VibBeatmapEncoder.MAGIC + "\n" + document));
            using var reader = new LineBufferedReader(stream);
            return Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
        }
    }
}
