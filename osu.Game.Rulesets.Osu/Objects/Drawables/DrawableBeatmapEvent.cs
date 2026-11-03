// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public partial class DrawableBeatmapEvent : DrawableOsuHitObject
    {
        public DrawableBeatmapEvent() : base(null!) { }
        public override bool HandlePositionalInput => false;
        public override void PlaySamples() { }
        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (timeOffset >= 0) ApplyResult(HitResult.IgnoreHit);
        }
    }
}
