// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Mania.Replays
{
    internal class ManiaAutoGenerator : AutoGenerator<ManiaReplayFrame>
    {
        public const double RELEASE_DELAY = 20;
        public bool HumanPlay { get; set; }
        public double TapDuration { get; set; } = RELEASE_DELAY;
        public double ChordSpread { get; set; }
        public double HoldReleaseOffset { get; set; }
        public int Seed { get; set; } = 1;

        public new ManiaBeatmap Beatmap => (ManiaBeatmap)base.Beatmap;

        public ManiaAutoGenerator(ManiaBeatmap beatmap)
            : base(beatmap)
        {
        }

        private System.Collections.Generic.List<ManiaHitObject> objects = null!;

        protected override void GenerateFrames()
        {
            objects = MultitapUtils.PlayableObjects(Beatmap.HitObjects).Cast<ManiaHitObject>().ToList();
            if (objects.Count == 0)
                return;

            var pointGroups = generateActionPoints().GroupBy(a => a.Time).OrderBy(g => g.First().Time);

            var actions = new List<ManiaAction>();

            foreach (var group in pointGroups)
            {
                foreach (var point in group)
                {
                    switch (point)
                    {
                        case HitPoint:
                            actions.Add(ManiaAction.Key1 + point.Column);
                            break;

                        case ReleasePoint:
                            actions.Remove(ManiaAction.Key1 + point.Column);
                            break;
                    }
                }

                Frames.Add(new ManiaReplayFrame(group.First().Time, actions.ToArray()));
            }
        }

        private IEnumerable<IActionPoint> generateActionPoints()
        {
            var random = new Random(Seed);
            var offsets = new Dictionary<ManiaHitObject, double>();
            foreach (var chord in objects.GroupBy(h => h.StartTime))
            {
                var notes = chord.OrderBy(_ => random.Next()).ToArray();
                for (int index = 0; index < notes.Length; index++)
                    offsets[notes[index]] = notes.Length <= 1 ? 0 : (HumanPlay ? Math.Max(ChordSpread, 7) : ChordSpread) * index / (notes.Length - 1);
            }
            if (HumanPlay)
            {
                foreach (var column in objects.GroupBy(h => h.Column))
                {
                    var notes = column.OrderBy(h => h.StartTime).ToArray();
                    for (int i = 0; i < notes.Length; i++)
                    {
                        var note = notes[i];
                        double variation = 4 * HumanPlayNoise.At(note.Column, Seed, 30)
                                           + 5 * HumanPlayNoise.Sample(note.StartTime, 95, Seed, 31 + note.Column);
                        // Keep repeated-key presses ordered even in very dense jacks.
                        double minimum = i == 0 ? double.NegativeInfinity : -(note.StartTime - notes[i - 1].StartTime) * 0.45;
                        double maximum = i == notes.Length - 1 ? double.PositiveInfinity : (notes[i + 1].StartTime - note.StartTime) * 0.45;
                        offsets[note] = Math.Clamp(offsets[note] + variation, minimum, maximum);
                    }
                }
            }
            for (int i = 0; i < objects.Count; i++)
            {
                var currentObject = objects[i];
                var nextObjectInColumn = GetNextObject(i); // Get the next object that requires pressing the same button
                double offset = offsets[currentObject];
                if (!HumanPlay && nextObjectInColumn != null)
                    offset = Math.Min(offset, Math.Max(0, (nextObjectInColumn.StartTime - currentObject.GetEndTime()) / 2));
                double releaseTime = calculateReleaseTime(currentObject, nextObjectInColumn) + offset;
                if (nextObjectInColumn != null)
                    releaseTime = Math.Min(releaseTime, nextObjectInColumn.StartTime + (HumanPlay ? offsets[(ManiaHitObject)nextObjectInColumn] : 0) - 0.01);

                yield return new HitPoint { Time = currentObject.StartTime + offset, Column = currentObject.Column };

                yield return new ReleasePoint { Time = Math.Max(currentObject.StartTime + offset, releaseTime), Column = currentObject.Column };
            }
        }

        private double calculateReleaseTime(HitObject currentObject, HitObject? nextObject)
        {
            double endTime = currentObject.GetEndTime();
            double releaseDelay = HumanPlay ? Math.Max(35, TapDuration) * (1 + 0.3 * HumanPlayNoise.Sample(currentObject.StartTime, 83, Seed, 40)) : TapDuration;

            if (currentObject is HoldNote hold)
            {
                if (hold.Duration > 0)
                    // hold note releases must be timed exactly.
                    return Math.Max(currentObject.StartTime, endTime + HoldReleaseOffset + (HumanPlay ? 8 * HumanPlayNoise.Sample(endTime, 130, Seed, 41) : 0));

                // Special case for super short hold notes
                releaseDelay = 1;
            }

            bool canDelayKeyUpFully = nextObject == null ||
                                      nextObject.StartTime > endTime + releaseDelay;

            return endTime + (canDelayKeyUpFully ? releaseDelay : (nextObject.AsNonNull().StartTime - endTime) * 0.9);
        }

        protected override HitObject? GetNextObject(int currentIndex)
        {
            int desiredColumn = objects[currentIndex].Column;

            for (int i = currentIndex + 1; i < objects.Count; i++)
            {
                if (objects[i].Column == desiredColumn)
                    return objects[i];
            }

            return null;
        }

        private interface IActionPoint
        {
            double Time { get; set; }
            int Column { get; set; }
        }

        private struct HitPoint : IActionPoint
        {
            public double Time { get; set; }
            public int Column { get; set; }
        }

        private struct ReleasePoint : IActionPoint
        {
            public double Time { get; set; }
            public int Column { get; set; }
        }
    }
}
