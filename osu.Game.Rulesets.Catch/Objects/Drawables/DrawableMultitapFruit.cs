// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Catch.Objects.Drawables
{
    public partial class DrawableMultitapFruit : DrawableCatchHitObject
    {
        public override bool DisplayResult => false;
        private readonly Container taps;
        private readonly OsuSpriteText remaining;
        public DrawableMultitapFruit() : base(null!)
        {
            RelativeSizeAxes = Axes.X;
            Origin = Anchor.BottomLeft;
            AddRangeInternal(new Drawable[]
            {
                taps = new NestedFruitContainer { RelativeSizeAxes = Axes.Both },
                remaining = new OsuSpriteText { Text = "MULTI", Font = OsuFont.Default.With(size: 14, weight: FontWeight.Bold) },
            });
        }

        protected override void AddNestedHitObject(DrawableHitObject hitObject)
        {
            base.AddNestedHitObject(hitObject);
            taps.Add(hitObject);
        }
        protected override void ClearNestedHitObjects()
        {
            base.ClearNestedHitObjects();
            taps.Clear(false);
        }
        protected override void Update()
        {
            base.Update();
            remaining.Text = $"×{NestedHitObjects.Count(h => !h.Judged)}";
        }
        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (NestedHitObjects.All(h => h.Judged)) ApplyResult(HitResult.IgnoreHit);
        }
        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            if (state != ArmedState.Idle) this.FadeOut(100).Expire();
        }
    }
}
