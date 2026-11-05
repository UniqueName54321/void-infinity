// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Collections.Generic;
using osu.Game.Audio;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Rulesets.Objects
{
    /// <summary>Native circle rhythm. A tap is one subdivision of the chosen beat unit.</summary>
    public class MultitapSettings
    {
        public int Count { get; set; } = 2;
        public int BeatsPerBar { get; set; } = 4;
        public int BeatUnit { get; set; } = 4;
        public int Subdivision { get; set; } = 4;
        public List<List<HitSampleInfo>> Samples { get; set; } = new List<List<HitSampleInfo>>();

        public void Validate()
        {
            if (Count < 1 || Count > 1024 || BeatsPerBar < 1 || BeatsPerBar > 32
                || BeatUnit < 1 || BeatUnit > 64 || (BeatUnit & (BeatUnit - 1)) != 0
                || Subdivision < 1 || Subdivision > 64 || (Samples.Count != 0 && Samples.Count != Count))
                throw new InvalidDataException("Invalid multitap rhythm (1–1024 taps, 1–32 beats/bar, power-of-two beat unit 1–64, 1–64 taps/beat).");
        }

        public double[] GetTapTimes(double startTime, ControlPointInfo timing)
        {
            Validate();
            if (!double.IsFinite(startTime)) throw new InvalidDataException("Invalid multitap start time.");
            var times = new double[Count];
            times[0] = startTime;
            for (int i = 1; i < Count; i++)
            {
                double time = times[i - 1];
                double remaining = 4.0 / BeatUnit / Subdivision;
                while (remaining > 0)
                {
                    double beatLength = timing.TimingPointAt(time).BeatLength;
                    if (!double.IsFinite(beatLength) || beatLength <= 0) throw new InvalidDataException("Invalid multitap tempo.");
                    var next = timing.TimingPointAfter(time);
                    double end = time + remaining * beatLength;
                    if (next == null || next.Time >= end)
                    {
                        time = end;
                        break;
                    }
                    remaining -= (next.Time - time) / beatLength;
                    time = next.Time;
                }
                if (!double.IsFinite(time) || time <= times[i - 1]) throw new InvalidDataException("Invalid multitap interval.");
                times[i] = time;
            }
            return times;
        }
    }
}
