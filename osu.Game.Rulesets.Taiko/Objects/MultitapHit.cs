// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Taiko.Objects
{
    public class MultitapHit : Hit, IHasDuration, IHasMultitap
    {
        private ControlPointInfo timing = null!;
        private double duration;
        public double EndTime => StartTime + duration;
        [JsonIgnore]
        public double Duration { get => duration; set => throw new NotSupportedException("Adjust the multitap rhythm instead."); }
        public MultitapHit() => Multitap = new MultitapSettings();
        public override Judgement CreateJudgement() => new IgnoreJudgement();
        protected override HitWindows CreateHitWindows() => HitWindows.Empty;

        public IEnumerable<HitObject> Expand(ControlPointInfo controlPoints) =>
            MultitapUtils.CreateTaps(this, controlPoints, () => new Hit { IsStrong = IsStrong });

        protected override void ApplyDefaultsToSelf(ControlPointInfo controlPoints, IBeatmapDifficultyInfo difficulty)
        {
            base.ApplyDefaultsToSelf(controlPoints, difficulty);
            timing = controlPoints;
            duration = Multitap.GetTapTimes(StartTime, timing)[^1] - StartTime;
        }

        protected override void CreateNestedHitObjects(CancellationToken cancellationToken)
        {
            foreach (var tap in Expand(timing))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddNested(tap);
            }
        }
    }
}
