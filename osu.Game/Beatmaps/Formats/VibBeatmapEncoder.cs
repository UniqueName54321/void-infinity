// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Game.IO.Serialization;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Skinning;

namespace osu.Game.Beatmaps.Formats
{
    public class VibBeatmapEncoder
    {
        public const string MAGIC = "void!infinity beatmap v1";
        private readonly IBeatmap beatmap;
        private readonly ISkin? skin;

        public VibBeatmapEncoder(IBeatmap beatmap, ISkin? skin = null)
        {
            this.beatmap = beatmap;
            this.skin = skin;
        }

        public void Encode(TextWriter writer)
        {
            using var legacy = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            new LegacyBeatmapEncoder(beatmap, skin).Encode(legacy);
            writer.WriteLine(MAGIC);
            writer.Write(JsonConvert.SerializeObject(new VibDocument
            {
                Legacy = legacy.ToString(),
                ClampSliderMovement = beatmap.ClampSliderMovement,
                Paths = beatmap.HitObjects.OfType<IHasPath>().Select(p => p.Path.ControlPoints.Select(c => c.Clone()).ToArray()).ToArray(),
                Events = beatmap.HitObjects.OfType<IHasBeatmapEvent>().Select(e => new VibEvent
                {
                    Time = ((HitObject)e).StartTime,
                    Position = ((IHasPosition)e).Position,
                    Change = e.Change
                }).ToArray()
            }, JsonSerializableExtensions.CreateGlobalSettings()));
        }
    }

    internal class VibDocument
    {
        public string Legacy { get; set; } = string.Empty;
        [System.ComponentModel.DefaultValue(true)]
        public bool ClampSliderMovement { get; set; } = true;
        public PathControlPoint[][] Paths { get; set; } = System.Array.Empty<PathControlPoint[]>();
        public VibEvent[] Events { get; set; } = System.Array.Empty<VibEvent>();
    }

    internal class VibEvent
    {
        public double Time { get; set; }
        public osuTK.Vector2 Position { get; set; }
        public BeatmapAttributeChange Change { get; set; } = new BeatmapAttributeChange();
    }
}
