// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Rulesets.Objects
{
    /// <summary>Integrates a piecewise linear velocity profile. Control polygon distance defines point progress.</summary>
    public class SliderVelocityProfile
    {
        private readonly double[] positions;
        private readonly double[] velocities;
        private readonly double[] times;
        public double DurationMultiplier => times[^1];

        public SliderVelocityProfile(IReadOnlyList<PathControlPoint> points)
        {
            int count = Math.Max(2, points.Count);
            positions = new double[count];
            velocities = Enumerable.Repeat(1d, count).ToArray();
            times = new double[count];
            if (points.Count >= 2)
            {
                for (int i = 0; i < count; i++)
                {
                    velocities[i] = points[i].Velocity;
                    if (i > 0) positions[i] = positions[i - 1] + (points[i].Position - points[i - 1].Position).Length;
                }
            }
            double length = positions[^1];
            for (int i = 0; i < count; i++)
                positions[i] = length > 0 ? positions[i] / length : (double)i / (count - 1);
            for (int i = 1; i < count; i++)
                times[i] = times[i - 1] + integrate(i, 1);
        }

        private double integrate(int i, double fraction)
        {
            double width = positions[i] - positions[i - 1];
            double a = velocities[i - 1];
            double delta = velocities[i] - a;
            return Math.Abs(delta) < 1e-8 ? width * fraction / a : width * Math.Log((a + delta * fraction) / a) / delta;
        }

        public double TimeAt(double progress)
        {
            progress = Math.Clamp(progress, 0, 1);
            for (int i = 1; i < positions.Length; i++)
            {
                if (positions[i] < progress || positions[i] == positions[i - 1]) continue;
                return (times[i - 1] + integrate(i, (progress - positions[i - 1]) / (positions[i] - positions[i - 1]))) / DurationMultiplier;
            }
            return 1;
        }

        public double ProgressAt(double time)
        {
            double target = Math.Clamp(time, 0, 1) * DurationMultiplier;
            for (int i = 1; i < times.Length; i++)
            {
                double width = positions[i] - positions[i - 1];
                if (times[i] < target || width == 0) continue;
                double delta = velocities[i] - velocities[i - 1];
                double t = target - times[i - 1];
                double fraction = Math.Abs(delta) < 1e-8 ? t * velocities[i - 1] / width
                    : velocities[i - 1] * (Math.Exp(t * delta / width) - 1) / delta;
                return Math.Clamp(positions[i - 1] + fraction * width, 0, 1);
            }
            return 1;
        }
    }
}
