// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Screens.Edit.Components;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles
{
    public partial class HitCircleSelectionBlueprint : OsuSelectionBlueprint<HitCircle>, IHasPopover
    {
        [Resolved]
        private EditorBeatmap? editorBeatmap { get; set; }

        public override MenuItem[] ContextMenuItems => new MenuItem[]
        {
            new OsuMenuItem("Configure multitap", MenuItemType.Standard, () => this.ShowPopover()),
        };

        public Popover GetPopover() => new MultitapPopover(HitObject.Multitap ?? new MultitapSettings
        {
            BeatsPerBar = editorBeatmap?.ControlPointInfo.TimingPointAt(HitObject.StartTime).TimeSignature.Numerator ?? 4,
        }, settings =>
        {
            if (editorBeatmap == null || !editorBeatmap.HitObjects.Contains(HitObject)) return;
            HitCircle replacement = settings.Count == 1 ? new HitCircle() : new MultitapCircle { Multitap = settings };
            replacement.StartTime = HitObject.StartTime;
            replacement.Position = HitObject.Position;
            replacement.NewCombo = HitObject.NewCombo;
            replacement.ComboOffset = HitObject.ComboOffset;
            replacement.Samples = HitObject.Samples;
            editorBeatmap.BeginChange();
            editorBeatmap.Remove(HitObject);
            editorBeatmap.Add(replacement);
            editorBeatmap.SelectedHitObjects.Clear();
            editorBeatmap.SelectedHitObjects.Add(replacement);
            editorBeatmap.EndChange();
        });

        protected readonly HitCirclePiece CirclePiece;
        private readonly HitCircleOverlapMarker marker;
        private readonly Bindable<bool> showHitMarkers = new Bindable<bool>();

        public HitCircleSelectionBlueprint(HitCircle circle)
            : base(circle)
        {
            InternalChildren = new Drawable[]
            {
                marker = new HitCircleOverlapMarker(),
                CirclePiece = new HitCirclePiece(),
            };
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorShowHitMarkers, showHitMarkers);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            showHitMarkers.BindValueChanged(_ =>
            {
                if (!showHitMarkers.Value)
                    (DrawableObject as DrawableHitCircle)?.RestoreHitAnimations();
            });
        }

        protected override void Update()
        {
            base.Update();

            CirclePiece.UpdateFrom(HitObject);
            marker.UpdateFrom(HitObject);

            if (showHitMarkers.Value)
                (DrawableObject as DrawableHitCircle)?.SuppressHitAnimations();
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => CirclePiece.ReceivePositionalInputAt(screenSpacePos);

        public override Quad SelectionQuad => CirclePiece.ScreenSpaceDrawQuad;
    }
}
