// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Rulesets.Replays
{
    /// <summary>Seeded, smooth value noise: reproducible imperfections without a repeating sine-wave path.</summary>
    public static class HumanPlayNoise
    {
        public static double At(int index, int seed, int channel)
        {
            uint value = unchecked((uint)index * 0x9e3779b9u + (uint)seed * 0x85ebca6bu + (uint)channel * 0xc2b2ae35u);
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            value ^= value >> 16;
            return value / (double)uint.MaxValue * 2 - 1;
        }

        public static double Sample(double time, double period, int seed, int channel)
        {
            double point = time / period;
            int index = unchecked((int)Math.Floor(point));
            double fraction = point - Math.Floor(point);
            fraction *= fraction * (3 - 2 * fraction);
            return At(index, seed, channel) * (1 - fraction) + At(unchecked(index + 1), seed, channel) * fraction;
        }
    }
}
