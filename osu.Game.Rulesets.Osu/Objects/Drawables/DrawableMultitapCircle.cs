// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public partial class DrawableMultitapCircle : DrawableOsuHitObject
    {
        public new MultitapCircle HitObject => (MultitapCircle)base.HitObject;
        private readonly Container taps;
        private readonly OsuSpriteText remaining;

        public DrawableMultitapCircle() : base(null!)
        {
            Origin = Anchor.Centre;
            Size = OsuHitObject.OBJECT_DIMENSIONS;
            AddRangeInternal(new Drawable[]
            {
                taps = new Container { RelativeSizeAxes = Axes.Both },
                remaining = new OsuSpriteText
                {
                    Anchor = Anchor.TopCentre, Origin = Anchor.BottomCentre,
                    Font = OsuFont.Default.With(size: 20, weight: FontWeight.Bold),
                },
            });
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            PositionBindable.BindValueChanged(_ => updatePosition());
            StackHeightBindable.BindValueChanged(_ => updatePosition());
        }

        protected override void OnApply()
        {
            base.OnApply();
            updatePosition();
        }

        private void updatePosition() => Position = HitObject.StackedPosition;

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

        protected override DrawableHitObject CreateNestedHitObject(HitObject hitObject) => new DrawableMultitapHitCircle((MultitapHitCircle)hitObject);

        protected override void Update()
        {
            base.Update();
            remaining.Text = $"×{NestedHitObjects.Count(h => !h.Judged)}";
        }

        public override void PlaySamples() { }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (NestedHitObjects.All(h => h.Judged)) ApplyResult(HitResult.IgnoreHit);
        }

        protected override void UpdateInitialTransforms()
        {
            base.UpdateInitialTransforms();
            this.FadeIn(HitObject.TimeFadeIn);
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            if (state != ArmedState.Idle) this.FadeOut(100).Expire();
        }
    }

    public partial class DrawableMultitapHitCircle : DrawableHitCircle
    {
        public DrawableMultitapHitCircle() : base() => Anchor = Anchor.Centre;
        public DrawableMultitapHitCircle(MultitapHitCircle h) : base(h) => Anchor = Anchor.Centre;
        protected override void OnApply()
        {
            base.OnApply();
            // Future overlapping taps must not swallow input intended for the next pending tap.
            HitArea.CanBeHit = () => !AllJudged && ParentHitObject.NestedHitObjects
                .TakeWhile(h => h != this).All(h => h.Judged);
        }

        protected override void UpdatePosition() => Position = Vector2.Zero;
    }
}
