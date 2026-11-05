// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Screens.Play
{
    /// <summary>Unskinnable automation disclosure, deliberately outside the hideable gameplay HUD.</summary>
    public sealed partial class HumanPlayIndicator : CompositeDrawable
    {
        public override bool HandlePositionalInput => false;
        public override bool HandleNonPositionalInput => false;

        public HumanPlayIndicator()
        {
            Anchor = Anchor.BottomLeft;
            Origin = Anchor.BottomLeft;
            Position = new Vector2(12, -12);
            Size = new Vector2(194, 32);
            Alpha = 0.95f;
            AlwaysPresent = true;
            Depth = float.MinValue;
            InternalChildren = new Drawable[]
            {
                new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black.Opacity(0.7f) },
                new SpriteIcon { Icon = OsuIcon.ModAutoplay, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, X = 6, Size = new Vector2(24) },
                new OsuSpriteText { Text = "AUTOPLAY · HUMAN PLAY", Font = OsuFont.Default.With(size: 14, weight: FontWeight.Bold), Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, X = 36 },
            };
        }

        protected override void Update()
        {
            base.Update();
            // HUD toggles, skin opacity and replay settings cannot fade this disclosure away.
            Alpha = 0.95f;
        }
    }
}
