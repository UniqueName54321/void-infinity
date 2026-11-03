// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects.Legacy;

namespace osu.Game.Rulesets.Objects
{
    public enum BeatmapEventType
    {
        ChangeAttributes
    }

    public interface IHasBeatmapEvent
    {
        BeatmapAttributeChange Change { get; set; }
    }

    /// <summary>Null means leave the current value unchanged. Changes persist until another event overrides them.</summary>
    public class BeatmapAttributeChange
    {
        public BeatmapEventType Type { get; set; }
        public float? CircleSize { get; set; }
        public float? ApproachRate { get; set; }
        public float? OverallDifficulty { get; set; }
        public float? DrainRate { get; set; }
        public double? SliderMultiplier { get; set; }
        public double? SliderTickRate { get; set; }

        public void Validate()
        {
            if (Type != BeatmapEventType.ChangeAttributes) throw new ArgumentOutOfRangeException(nameof(Type));
            foreach (var value in new[] { CircleSize, ApproachRate, OverallDifficulty, DrainRate })
                if (value.HasValue && (!float.IsFinite(value.Value) || value < 0 || value > 10))
                    throw new ArgumentOutOfRangeException(nameof(value), "Difficulty must be between 0 and 10.");
            foreach (var value in new[] { SliderMultiplier, SliderTickRate })
                if (value.HasValue && (!double.IsFinite(value.Value) || value <= 0 || value > 100))
                    throw new ArgumentOutOfRangeException(nameof(value), "Slider settings must be in (0, 100].");
        }

        public void ApplyTo(BeatmapDifficulty difficulty)
        {
            Validate();
            difficulty.CircleSize = CircleSize ?? difficulty.CircleSize;
            difficulty.ApproachRate = ApproachRate ?? difficulty.ApproachRate;
            difficulty.OverallDifficulty = OverallDifficulty ?? difficulty.OverallDifficulty;
            difficulty.DrainRate = DrainRate ?? difficulty.DrainRate;
            difficulty.SliderMultiplier = SliderMultiplier ?? difficulty.SliderMultiplier;
            difficulty.SliderTickRate = SliderTickRate ?? difficulty.SliderTickRate;
        }

        public static BeatmapDifficulty DifficultyAt(IBeatmap beatmap, double time)
        {
            var result = beatmap.Difficulty.Clone();
            foreach (var e in beatmap.HitObjects.Where(h => h.StartTime <= time).OfType<IHasBeatmapEvent>())
                e.Change.ApplyTo(result);
            return result;
        }
    }

    public class BeatmapAttributeTimeline
    {
        private readonly double[] times;
        private readonly BeatmapDifficulty[] difficulties;
        private readonly BeatmapDifficulty initial;

        public BeatmapAttributeTimeline(IBeatmap beatmap)
        {
            initial = beatmap.Difficulty;
            var events = beatmap.HitObjects.Where(h => h is IHasBeatmapEvent).OrderBy(h => h.StartTime).ToArray();
            times = events.Select(h => h.StartTime).ToArray();
            difficulties = new BeatmapDifficulty[events.Length];
            var current = initial;
            for (int i = 0; i < events.Length; i++)
            {
                current = current.Clone();
                ((IHasBeatmapEvent)events[i]).Change.ApplyTo(current);
                difficulties[i] = current;
            }
        }

        public BeatmapDifficulty At(double time)
        {
            int low = 0;
            int high = times.Length;
            while (low < high)
            {
                int mid = low + (high - low) / 2;
                if (times[mid] <= time) low = mid + 1;
                else high = mid;
            }
            return low == 0 ? initial : difficulties[low - 1];
        }
    }

    internal class ConvertBeatmapEvent : ConvertHitObject, IHasBeatmapEvent
    {
        public BeatmapAttributeChange Change { get; set; } = new BeatmapAttributeChange();
    }
}
