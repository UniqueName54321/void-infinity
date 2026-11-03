// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Components;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    public partial class TestSceneVoidInfinityEvents : TestSceneOsuEditor
    {
        [Test]
        public void TestEditEventThroughInspector()
        {
            BeatmapEvent e = null!;
            HitCircle circle = null!;
            float initialScale = 0;
            AddStep("add event and circle", () =>
            {
                EditorBeatmap.BeginChange();
                EditorBeatmap.Clear();
                double time = EditorBeatmap.ControlPointInfo.TimingPoints.First().Time;
                e = new BeatmapEvent { StartTime = time, Position = new Vector2(100) };
                circle = new HitCircle { StartTime = time + 500, Position = new Vector2(300, 200) };
                EditorBeatmap.Add(e);
                EditorBeatmap.Add(circle);
                EditorBeatmap.EndChange();
                EditorClock.Seek(time);
                initialScale = circle.Scale;
            });
            AddUntilStep("event blueprint present", () => Editor.ChildrenOfType<EventSelectionBlueprint>().Any(b => b.IsPresent));
            AddStep("select event", () => EditorBeatmap.SelectedHitObjects.Add(e));
            AddStep("open inspector controls", () => Editor.ChildrenOfType<EditorToolButton>().Single(b => b.Text == "Edit event").TriggerClick());
            AddUntilStep("popover loaded", () => Editor.ChildrenOfType<BeatmapEventPopover>().Any(p => p.IsPresent));
            AddStep("change circle size", () => Editor.ChildrenOfType<BeatmapEventPopover>().Single().ChildrenOfType<OsuTextBox>().Single(t => t.Name == "Circle size").Text = "7");
            AddStep("accept", () => Editor.ChildrenOfType<BeatmapEventPopover>().Single().ChildrenOfType<RoundedButton>().Single().TriggerClick());
            AddAssert("event updated", () => e.Change.CircleSize == 7);
            AddAssert("following circle resized", () => circle.Scale < initialScale);
            AddStep("undo", () => Editor.Undo());
            AddAssert("event restored", () => EditorBeatmap.HitObjects.OfType<BeatmapEvent>().Single().Change.CircleSize == null);
            AddAssert("circle size restored", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Single().Scale == initialScale);
        }
    }
}
