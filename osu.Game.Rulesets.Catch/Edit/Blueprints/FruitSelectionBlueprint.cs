// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Rulesets.Catch.Edit.Blueprints.Components;
using osu.Game.Rulesets.Catch.Objects;

namespace osu.Game.Rulesets.Catch.Edit.Blueprints
{
    public partial class FruitSelectionBlueprint : CatchSelectionBlueprint<Fruit>, IHasPopover
    {
        private readonly FruitOutline outline;

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
                ? new MultitapFruit { X = ((Fruit)HitObject).OriginalX, LegacyConvertedY = ((Fruit)HitObject).LegacyConvertedY, NewCombo = ((Fruit)HitObject).NewCombo, ComboOffset = ((Fruit)HitObject).ComboOffset }
                : new Fruit { X = ((Fruit)HitObject).OriginalX, LegacyConvertedY = ((Fruit)HitObject).LegacyConvertedY, NewCombo = ((Fruit)HitObject).NewCombo, ComboOffset = ((Fruit)HitObject).ComboOffset });
        });

        public FruitSelectionBlueprint(Fruit hitObject)
            : base(hitObject)
        {
            InternalChild = outline = new FruitOutline();
        }

        protected override void Update()
        {
            base.Update();

            if (!IsSelected) return;

            outline.Position = CatchHitObjectUtils.GetStartPosition(HitObjectContainer, HitObject);
            outline.UpdateFrom(HitObject);
        }
    }
}
