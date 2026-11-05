// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Rulesets.Mania.Edit.Blueprints.Components;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.UI.Scrolling;
using osuTK;

namespace osu.Game.Rulesets.Mania.Edit.Blueprints
{
    public partial class NoteSelectionBlueprint : ManiaSelectionBlueprint<Note>, IHasPopover
    {
        private readonly EditNotePiece notePiece;

        [Resolved]
        private EditorBeatmap? editorBeatmap { get; set; }

        public override MenuItem[] ContextMenuItems => true ? new MenuItem[]
        {
            new OsuMenuItem("Configure multitap", MenuItemType.Standard, () => this.ShowPopover()),
        } : base.ContextMenuItems;

        public Popover GetPopover() => new MultitapPopover(HitObject.Multitap ?? new MultitapSettings
        {
            BeatsPerBar = editorBeatmap?.ControlPointInfo.TimingPointAt(HitObject.StartTime).TimeSignature.Numerator ?? 4,
        }, settings =>
        {
            if (editorBeatmap == null) return;
            MultitapEditor.Apply(editorBeatmap, HitObject, settings, multi => multi
                ? new MultitapNote { Column = ((Note)HitObject).Column }
                : new Note { Column = ((Note)HitObject).Column });
        });

        public NoteSelectionBlueprint(Note note)
            : base(note)
        {
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            AddInternal(notePiece = new EditNotePiece
            {
                RelativeSizeAxes = Axes.X,
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
            });
        }

        protected override void Update()
        {
            base.Update();

            notePiece.Height = DrawableObject.DrawHeight;
        }

        protected override void OnDirectionChanged(ValueChangedEvent<ScrollingDirection> direction)
        {
            notePiece.Scale = new Vector2(1, direction.NewValue == ScrollingDirection.Down ? 1 : -1);
        }
    }
}
