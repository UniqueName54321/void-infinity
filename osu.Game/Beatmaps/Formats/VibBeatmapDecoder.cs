// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using osu.Game.IO;
using osu.Game.IO.Serialization;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Beatmaps.Formats
{
    public class VibBeatmapDecoder : LegacyBeatmapDecoder
    {
        public VibBeatmapDecoder() : base(LegacyBeatmapEncoder.FIRST_LAZER_VERSION)
        {
        }

        public new static void Register() => AddDecoder<Beatmap>(VibBeatmapEncoder.MAGIC, _ => new VibBeatmapDecoder());

        protected override void ParseStreamInto(LineBufferedReader stream, Beatmap output)
        {
            if (stream.ReadLine()?.Trim() != VibBeatmapEncoder.MAGIC)
                throw new InvalidDataException("Unsupported void!infinity beatmap version.");
            var document = JsonConvert.DeserializeObject<VibDocument>(stream.ReadToEnd(), JsonSerializableExtensions.CreateGlobalSettings())
                           ?? throw new InvalidDataException("Missing beatmap data.");
            using var payload = new MemoryStream(Encoding.UTF8.GetBytes(document.Legacy));
            using var reader = new LineBufferedReader(payload);
            base.ParseStreamInto(reader, output);
            var paths = output.HitObjects.OfType<IHasPath>().ToArray();
            if (paths.Length != document.Paths.Length)
                throw new InvalidDataException("Slider extension data does not match the beatmap.");
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i].Path.ControlPoints.Clear();
                paths[i].Path.ControlPoints.AddRange(document.Paths[i]);
            }
            var seenMultitaps = new System.Collections.Generic.HashSet<int>();
            foreach (var m in document.Multitaps)
            {
                if (m.Index < 0 || m.Index >= output.HitObjects.Count || !seenMultitaps.Add(m.Index) || m.Settings == null
                    || output.HitObjects[m.Index] is IHasPath or IHasDuration || output.BeatmapInfo.Ruleset.OnlineID is < 0 or > 3)
                    throw new InvalidDataException("Invalid multitap extension target.");
                m.Settings.Validate();
                output.HitObjects[m.Index].Multitap = m.Settings;
            }
            output.ClampSliderMovement = document.ClampSliderMovement;
            foreach (var e in document.Events)
            {
                if (!double.IsFinite(e.Time)) throw new InvalidDataException("Invalid event time.");
                e.Change.Validate();
                output.HitObjects.Add(new ConvertBeatmapEvent { StartTime = e.Time, Position = e.Position, Change = e.Change });
            }
            output.HitObjects = output.HitObjects.OrderBy(h => h.StartTime).ToList();
            foreach (var h in output.HitObjects)
                h.ApplyDefaults(output.ControlPointInfo, output.Difficulty);
        }
    }
}
