// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Rulesets.Edit;
using osuTK;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Edit.Blueprints
{
    public partial class TaikoSelectionBlueprint : HitObjectSelectionBlueprint, IHasPopover
    {
        [Resolved]
        private EditorBeatmap? editorBeatmap { get; set; }

        public override MenuItem[] ContextMenuItems => Item is Hit ? new MenuItem[]
        {
            new OsuMenuItem("Configure multitap", MenuItemType.Standard, () => this.ShowPopover()),
        } : base.ContextMenuItems;

        public Popover GetPopover() => new MultitapPopover(Item.Multitap ?? new MultitapSettings
        {
            BeatsPerBar = editorBeatmap?.ControlPointInfo.TimingPointAt(Item.StartTime).TimeSignature.Numerator ?? 4,
        }, settings =>
        {
            if (editorBeatmap == null) return;
            MultitapEditor.Apply(editorBeatmap, Item, settings, multi => multi
                ? new MultitapHit { IsStrong = ((Hit)Item).IsStrong }
                : new Hit { IsStrong = ((Hit)Item).IsStrong });
        });

        public TaikoSelectionBlueprint(HitObject hitObject)
            : base(hitObject)
        {
            RelativeSizeAxes = Axes.None;

            AddInternal(new HitPiece
            {
                RelativeSizeAxes = Axes.Both,
                Origin = Anchor.TopLeft
            });
        }

        protected override void Update()
        {
            base.Update();

            // Move the rectangle to cover the hitobjects
            var topLeft = new Vector2(float.MaxValue, float.MaxValue);
            var bottomRight = new Vector2(float.MinValue, float.MinValue);

            topLeft = Vector2.ComponentMin(topLeft, Parent!.ToLocalSpace(DrawableObject.ScreenSpaceDrawQuad.TopLeft));
            bottomRight = Vector2.ComponentMax(bottomRight, Parent!.ToLocalSpace(DrawableObject.ScreenSpaceDrawQuad.BottomRight));

            Size = bottomRight - topLeft;
            Position = topLeft;
        }
    }
}
