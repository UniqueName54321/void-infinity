// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Osu.Edit.Blueprints;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Edit
{
    public class EventCompositionTool : CompositionTool
    {
        public EventCompositionTool() : base("Event") { }
        public override Drawable CreateIcon() => new OsuSpriteText { Text = "E" };
        public override HitObjectPlacementBlueprint CreatePlacementBlueprint() => new EventPlacementBlueprint();
    }

    public partial class EventPlacementBlueprint : HitObjectPlacementBlueprint
    {
        public EventPlacementBlueprint() : base(new BeatmapEvent())
        {
            InternalChild = new OsuSpriteText { Text = "[Event]", Origin = Anchor.Centre };
        }
        protected override void LoadComplete()
        {
            base.LoadComplete();
            BeginPlacement();
        }
        public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
        {
            var result = base.UpdateTimeAndPosition(screenSpacePosition, fallbackTime);
            ((BeatmapEvent)HitObject).Position = ToLocalSpace(screenSpacePosition);
            InternalChild.Position = ((BeatmapEvent)HitObject).Position;
            return result;
        }
        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left) return false;
            EndPlacement(true);
            return true;
        }
    }

    public partial class EventSelectionBlueprint : OsuSelectionBlueprint<BeatmapEvent>
    {
        private readonly OsuSpriteText marker;
        public EventSelectionBlueprint(BeatmapEvent e) : base(e)
        {
            InternalChild = marker = new OsuSpriteText { Text = "[Event]", Origin = Anchor.Centre };
        }
        protected override void Update()
        {
            base.Update();
            marker.Position = HitObject.Position;
            marker.Colour = IsSelected ? Colour4.Yellow : Colour4.White;
        }
        protected override bool ShouldBeAlive => true;
        public override bool IsSelectable => true;
        public override Vector2 ScreenSpaceSelectionPoint => marker.ScreenSpaceDrawQuad.Centre;
        public override Quad SelectionQuad => marker.ScreenSpaceDrawQuad;
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => marker.ReceivePositionalInputAt(screenSpacePos);
    }
}
