// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    public static class MultitapConversion
    {
        /// <summary>Only merge a uniform, coincident stream; never silently retime or move notes.</summary>
        public static MultitapCircle? FromStream(HitCircle[] circles, ControlPointInfo timing)
        {
            circles = circles.OrderBy(h => h.StartTime).ToArray();
            if (circles.Length < 2 || circles.Length > 1024) return null;
            var first = circles[0];
            if (circles.Skip(1).Any(h => Vector2.DistanceSquared(h.Position, first.Position) > 0.0001f || h.NewCombo || h.ComboOffset != 0)) return null;
            for (int divisor = 1; divisor <= 64; divisor++)
            {
                var rhythm = new MultitapSettings { Count = circles.Length, Subdivision = divisor,
                    BeatsPerBar = timing.TimingPointAt(first.StartTime).TimeSignature.Numerator,
                    Samples = circles.Select(h => h.Samples.ToList()).ToList() };
                double[] times = rhythm.GetTapTimes(first.StartTime, timing);
                if (circles.Where((h, i) => Math.Abs(h.StartTime - times[i]) > 0.01).Any()) continue;
                return new MultitapCircle
                {
                    StartTime = first.StartTime, Position = first.Position, Samples = first.Samples,
                    NewCombo = first.NewCombo, ComboOffset = first.ComboOffset, Multitap = rhythm,
                };
            }
            return null;
        }
    }
}
