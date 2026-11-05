// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Taiko.Replays
{
    public class TaikoAutoGenerator : AutoGenerator<TaikoReplayFrame>
    {
        public new TaikoBeatmap Beatmap => (TaikoBeatmap)base.Beatmap;

        public bool HumanPlay { get; set; }
        public bool RandomizeHands { get; set; }
        public int Seed { get; set; } = 1;
        public bool AlternateHands { get; set; } = true;
        public bool StartRight { get; set; }
        public double SwellInterval { get; set; } = 50;
        public int ExtraRollHits { get; set; }

        public TaikoAutoGenerator(IBeatmap beatmap)
            : base(beatmap)
        {
        }

        private System.Collections.Generic.List<TaikoHitObject> objects = null!;

        protected override void GenerateFrames()
        {
            objects = MultitapUtils.PlayableObjects(Beatmap.HitObjects).Cast<TaikoHitObject>().ToList();
            if (objects.Count == 0)
                return;

            bool hitButton = !StartRight;
            var random = new Random(Seed);

            Frames.Add(new TaikoReplayFrame(objects[0].StartTime - 1000));

            for (int i = 0; i < objects.Count; i++)
            {
                TaikoHitObject h = objects[i];
                double endTime = h.GetEndTime();

                switch (h)
                {
                    case Swell swell:
                    {
                        int d = 0;
                        int count = 0;
                        int req = swell.RequiredHits;
                        double hitRate = Math.Min(HumanPlay ? SwellInterval * (0.9 + random.NextDouble() * 0.2) : SwellInterval, swell.Duration / req);

                        for (double j = h.StartTime; j < endTime; j += hitRate * (HumanPlay ? 0.88 + random.NextDouble() * 0.24 : 1))
                        {
                            TaikoAction action;

                            switch (d)
                            {
                                default:
                                case 0:
                                    action = TaikoAction.LeftCentre;
                                    break;

                                case 1:
                                    action = TaikoAction.LeftRim;
                                    break;

                                case 2:
                                    action = TaikoAction.RightCentre;
                                    break;

                                case 3:
                                    action = TaikoAction.RightRim;
                                    break;
                            }

                            Frames.Add(new TaikoReplayFrame(j, action));
                            d = (d + 1) % 4;
                            if (++count == req)
                                break;
                        }

                        break;
                    }

                    case DrumRoll drumRoll:
                    {
                        var ticks = drumRoll.NestedHitObjects.OfType<DrumRollTick>().ToArray();
                        for (int tickIndex = 0; tickIndex < ticks.Length; tickIndex++)
                        {
                            var tick = ticks[tickIndex];
                            double nextTime = tickIndex + 1 < ticks.Length ? ticks[tickIndex + 1].StartTime : endTime;
                            int hits = nextTime > tick.StartTime ? ExtraRollHits + 1 : 1;
                            for (int extra = 0; extra < hits; extra++)
                            {
                                double time = tick.StartTime + Math.Max(0, nextTime - tick.StartTime) * extra / hits;
                                if (ExtraRollHits > 0 || !AlternateHands)
                                    Frames.Add(new TaikoReplayFrame(time - 0.001));
                                Frames.Add(new TaikoReplayFrame(time, hitButton ? TaikoAction.LeftCentre : TaikoAction.RightCentre));
                                if (AlternateHands) hitButton = !hitButton;
                            }
                        }

                        break;
                    }

                    case Hit hit:
                    {
                        if (RandomizeHands || (HumanPlay && random.NextDouble() < 0.08)) hitButton = random.Next(2) == 0;
                        TaikoAction[] actions;

                        if (hit.Type == HitType.Centre)
                        {
                            actions = hit.IsStrong
                                ? new[] { TaikoAction.LeftCentre, TaikoAction.RightCentre }
                                : new[] { hitButton ? TaikoAction.LeftCentre : TaikoAction.RightCentre };
                        }
                        else
                        {
                            actions = hit.IsStrong
                                ? new[] { TaikoAction.LeftRim, TaikoAction.RightRim }
                                : new[] { hitButton ? TaikoAction.LeftRim : TaikoAction.RightRim };
                        }

                        Frames.Add(new TaikoReplayFrame(h.StartTime, actions));
                        break;
                    }

                    default:
                        throw new InvalidOperationException("Unknown hit object type.");
                }

                var nextHitObject = GetNextObject(i); // Get the next object that requires pressing the same button

                bool canDelayKeyUp = nextHitObject == null || nextHitObject.StartTime > endTime + KEY_UP_DELAY;
                double calculatedDelay = canDelayKeyUp ? KEY_UP_DELAY : (nextHitObject.AsNonNull().StartTime - endTime) * 0.9;
                Frames.Add(new TaikoReplayFrame(endTime + calculatedDelay));

                if (AlternateHands) hitButton = !hitButton;
            }
        }
    }
}
