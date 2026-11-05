// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Catch.Replays
{
    internal class CatchAutoGenerator : AutoGenerator<CatchReplayFrame>
    {
        public new CatchBeatmap Beatmap => (CatchBeatmap)base.Beatmap;

        private readonly float halfCatcherWidth;
        public bool HumanPlay { get; set; }
        public bool RandomizeAim { get; set; }
        public int Seed { get; set; } = 1;
        public double AimOffset { get; set; }
        public bool MoveEarly { get; set; }
        public bool AlwaysDash { get; set; }

        public CatchAutoGenerator(IBeatmap beatmap)
            : base(beatmap)
        {
            halfCatcherWidth = Catcher.CalculateCatchWidth(beatmap.Difficulty) * 0.5f;
        }

        protected override void GenerateFrames()
        {
            if (Beatmap.HitObjects.Count == 0)
                return;

            var random = new Random(Seed);
            float lastPosition = CatchPlayfield.CENTER_X;
            double lastTime = 0;

            void moveToNext(PalpableCatchHitObject h)
            {
                double offset = RandomizeAim ? (random.NextDouble() * 2 - 1) * (AimOffset == 0 ? 0.5 : Math.Abs(AimOffset)) : AimOffset;
                if (HumanPlay)
                {
                    offset = Math.Clamp(offset + 0.35 * HumanPlayNoise.Sample(h.StartTime, 210, Seed, 50), -0.85, 0.85);
                    // Catch has binary judgements: occasional reach mistakes supply the human accuracy cost.
                    if (random.NextDouble() < 0.015) offset = random.Next(2) == 0 ? -1.15 : 1.15;
                }
                float targetPosition = Math.Clamp(h.EffectiveX + (float)offset * halfCatcherWidth, 0, CatchPlayfield.WIDTH);
                float positionChange = Math.Abs(lastPosition - targetPosition);
                double timeAvailable = h.StartTime - lastTime;

                if (timeAvailable < 0)
                {
                    return;
                }

                // So we can either make it there without a dash or not.
                // If positionChange is 0, we don't need to move, so speedRequired should also be 0 (could be NaN if timeAvailable is 0 too)
                // The case where positionChange > 0 and timeAvailable == 0 results in PositiveInfinity which provides expected beheaviour.
                double speedRequired = positionChange == 0 ? 0 : positionChange / timeAvailable;

                bool dashRequired = speedRequired > Catcher.BASE_WALK_SPEED;
                bool impossibleJump = speedRequired > Catcher.BASE_DASH_SPEED;

                if (!HumanPlay && !RandomizeAim && AimOffset == 0 && lastPosition - halfCatcherWidth < h.EffectiveX && lastPosition + halfCatcherWidth > h.EffectiveX)
                {
                    // we are already in the correct range.
                    lastTime = h.StartTime;
                    addFrame(h.StartTime, lastPosition);
                    return;
                }

                if (impossibleJump)
                {
                    addFrame(h.StartTime, targetPosition);
                }
                else if (h.HyperDash || MoveEarly || AlwaysDash)
                {
                    addFrame(h.StartTime - timeAvailable, lastPosition);
                    addFrame(h.StartTime, targetPosition);
                }
                else if (dashRequired)
                {
                    // we do a movement in two parts - the dash part then the normal part...
                    double timeAtNormalSpeed = positionChange / Catcher.BASE_WALK_SPEED;
                    double timeWeNeedToSave = timeAtNormalSpeed - timeAvailable;
                    double timeAtDashSpeed = timeWeNeedToSave / 2;

                    float midPosition = (float)Interpolation.Lerp(lastPosition, targetPosition, (float)timeAtDashSpeed / timeAvailable);

                    // dash movement
                    addFrame(h.StartTime - timeAvailable + 1, lastPosition, true);
                    addFrame(h.StartTime - timeAvailable + timeAtDashSpeed, midPosition);
                    addFrame(h.StartTime, targetPosition);
                }
                else
                {
                    double timeBefore = Math.Min(timeAvailable, positionChange / (Catcher.BASE_WALK_SPEED * (HumanPlay ? 0.8 + random.NextDouble() * 0.2 : 1)));

                    double movementStart = h.StartTime - timeBefore;
                    addFrame(movementStart, lastPosition);
                    if (HumanPlay && timeBefore > 100)
                    {
                        // Slightly overshoot and correct on relaxed reaches, while respecting available travel time.
                        double correctionTime = Math.Min(45, timeBefore / 4);
                        float overshoot = (float)(HumanPlayNoise.At((int)h.StartTime, Seed, 51) * Math.Min(halfCatcherWidth * 0.2, correctionTime * Catcher.BASE_WALK_SPEED));
                        addFrame(h.StartTime - correctionTime, Math.Clamp(targetPosition + overshoot, 0, CatchPlayfield.WIDTH));
                    }
                    addFrame(h.StartTime, targetPosition);
                }

                lastTime = h.StartTime;
                lastPosition = targetPosition;
            }

            foreach (var obj in CatchBeatmap.GetPalpableObjects(Beatmap.HitObjects))
                moveToNext(obj);
        }

        private void addFrame(double time, float? position = null, bool dashing = false)
        {
            Frames.Add(new CatchReplayFrame(time, position, dashing || AlwaysDash, LastFrame));
        }
    }
}
