// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Objects
{
    public class MultitapCircle : HitCircle, IHasDuration, IHasMultitap
    {
        [JsonIgnore]
        public double[] TapTimes { get; private set; } = Array.Empty<double>();
        private double duration;
        public double EndTime => StartTime + duration;
        [JsonIgnore]
        public double Duration { get => EndTime - StartTime; set => throw new NotSupportedException("Adjust the multitap rhythm instead."); }

        public override osuTK.Vector2 Position
        {
            get => base.Position;
            set
            {
                base.Position = value;
                foreach (var tap in NestedHitObjects.OfType<MultitapHitCircle>()) tap.Position = value;
            }
        }

        public System.Collections.Generic.IEnumerable<HitObject> Expand(ControlPointInfo timing) =>
            MultitapUtils.CreateTaps(this, timing, () => new HitCircle { Position = Position });

        public MultitapCircle() => Multitap = new MultitapSettings();
        public override Judgement CreateJudgement() => new IgnoreJudgement();
        protected override HitWindows CreateHitWindows() => HitWindows.Empty;

        protected override void ApplyDefaultsToSelf(ControlPointInfo controlPointInfo, IBeatmapDifficultyInfo difficulty)
        {
            base.ApplyDefaultsToSelf(controlPointInfo, difficulty);
            TapTimes = Multitap.GetTapTimes(StartTime, controlPointInfo);
            duration = TapTimes[^1] - StartTime;
        }

        protected override void CreateNestedHitObjects(CancellationToken cancellationToken)
        {
            base.CreateNestedHitObjects(cancellationToken);
            for (int i = 0; i < TapTimes.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var tap = new MultitapHitCircle
                {
                    StartTime = TapTimes[i], Position = Position,
                    Samples = Multitap.Samples.Count == TapTimes.Length ? Multitap.Samples[i] : Samples,
                    NewCombo = i == 0 && NewCombo, ComboOffset = i == 0 ? ComboOffset : 0,
                };
                tap.StackHeightBindable.BindTo(StackHeightBindable);
                AddNested(tap);
            }
        }
    }

    public class MultitapHitCircle : HitCircle
    {
    }
}
