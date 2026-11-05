// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Components;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    public partial class TestSceneMultitap : TestSceneOsuEditor
    {
        [SetUp]
        public void UseLocalAvatar() => Schedule(() => API.LocalUser.Value.AvatarUrl = "Online/avatar-guest");

        private HitCircle circle = null!;
        private HitCircleSelectionBlueprint blueprint => Editor.ChildrenOfType<HitCircleSelectionBlueprint>().Single(b => b.HitObject == circle);
        private MultitapPopover popover => Editor.ChildrenOfType<MultitapPopover>().Single(p => p.IsPresent);

        private void selectCircle()
        {
            AddStep("select circle", () =>
            {
                circle = EditorBeatmap.HitObjects.OfType<HitCircle>().First();
                EditorClock.Seek(circle.StartTime);
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(circle);
            });
            AddUntilStep("blueprint available", () => Editor.ChildrenOfType<HitCircleSelectionBlueprint>().Any(b => b.HitObject == circle));
        }

        private void openSettings()
        {
            AddStep("configure multitap", () => blueprint.ContextMenuItems.Single(m => m.Text.Value == "Configure multitap").Action.Value!.Invoke());
            AddUntilStep("settings shown", () => Editor.ChildrenOfType<MultitapPopover>().Any(p => p.IsPresent));
        }

        [Test]
        public void TestConfigureCancelAndUndo()
        {
            selectCircle();
            openSettings();
            AddStep("cancel", () => popover.ChildrenOfType<RoundedButton>().Single(b => b.Text == "Cancel").TriggerClick());
            AddAssert("original retained", () => EditorBeatmap.HitObjects.Contains(circle) && circle is not MultitapCircle);
            AddWaitStep("popover closes", 3);
            openSettings();
            AddStep("enter three taps in 3/8", () =>
            {
                var inputs = popover.ChildrenOfType<OsuTextBox>().ToArray();
                inputs[0].Text = "3";
                inputs[1].Text = "3";
                inputs[2].Text = "8";
                inputs[3].Text = "2";
            });
            AddStep("accept", () => popover.ChildrenOfType<RoundedButton>().Single(b => b.Text == "Accept").TriggerClick());
            AddAssert("one multitap created", () => !EditorBeatmap.HitObjects.Contains(circle) && EditorBeatmap.SelectedHitObjects.Single() is MultitapCircle m
                                                   && m.Multitap.Count == 3 && m.Multitap.BeatsPerBar == 3 && m.Multitap.BeatUnit == 8 && m.Multitap.Subdivision == 2);
            AddStep("undo", () => Editor.Undo());
            AddAssert("normal circle restored", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Any(h => h.StartTime == circle.StartTime && h.Position == circle.Position && h is not MultitapCircle));
            AddStep("redo", () => Editor.Redo());
            AddAssert("multitap rhythm restored", () => EditorBeatmap.HitObjects.OfType<MultitapCircle>().Any(m => m.StartTime == circle.StartTime && m.Multitap.Count == 3 && m.Multitap.BeatUnit == 8));
        }

        [Test]
        public void TestInvalidInputDoesNotChangeMap()
        {
            selectCircle();
            openSettings();
            AddStep("enter invalid count", () => popover.ChildrenOfType<OsuTextBox>().First().Text = "0");
            AddStep("accept invalid input", () => popover.ChildrenOfType<RoundedButton>().Single(b => b.Text == "Accept").TriggerClick());
            AddAssert("map unchanged and popover open", () => popover.IsPresent && EditorBeatmap.HitObjects.Contains(circle));
        }

        [Test]
        public void TestStackedStreamConversionAndUndo()
        {
            HitCircle[] circles = null!;
            AddStep("create stacked stream", () =>
            {
                double start = EditorBeatmap.HitObjects.Last().StartTime + 2000;
                double interval = EditorBeatmap.ControlPointInfo.TimingPointAt(start).BeatLength / 4;
                circles = Enumerable.Range(0, 4).Select(i => new HitCircle { StartTime = start + interval * i, Position = new Vector2(256, 192) }).ToArray();
                EditorBeatmap.BeginChange();
                foreach (var h in circles) EditorBeatmap.Add(h);
                EditorBeatmap.EndChange();
                EditorClock.Seek(start);
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(circles);
            });
            AddUntilStep("stream blueprints available", () => Editor.ChildrenOfType<HitCircleSelectionBlueprint>().Any(b => b.HitObject == circles[0]));
            AddStep("hover selection", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<HitCircleSelectionBlueprint>().Single(b => b.HitObject == circles[0])));
            AddStep("convert stream", () => Editor.ChildrenOfType<OsuSelectionHandler>().Single().ContextMenuItems
                .Single(m => m.Text.Value == "Convert stacked stream to multitap").Action.Value!.Invoke());
            AddAssert("single multitap selected", () => EditorBeatmap.SelectedHitObjects.Single() is MultitapCircle m && m.Multitap.Count == 4 && circles.All(h => !EditorBeatmap.HitObjects.Contains(h)));
            AddStep("undo conversion", () => Editor.Undo());
            AddAssert("four circles restored", () => circles.All(h => EditorBeatmap.HitObjects.OfType<HitCircle>().Any(r => r is not MultitapCircle && r.StartTime == h.StartTime && r.Position == h.Position)));
        }
    }
}
