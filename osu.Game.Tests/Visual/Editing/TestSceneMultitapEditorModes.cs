// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Components;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Visual.Editing
{
    [TestFixture(1)] [TestFixture(2)] [TestFixture(3)]
    public partial class TestSceneMultitapEditorModes : EditorTestScene
    {
        private readonly int mode;
        public TestSceneMultitapEditorModes(int mode) => this.mode = mode;
        [SetUp]
        public void UseLocalAvatar() => Schedule(() => API.LocalUser.Value.AvatarUrl = "Online/avatar-guest");
        protected override Ruleset CreateEditorRuleset() => MultitapModesTest.RulesetFor(mode);
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var map = MultitapModesTest.CreateMap(mode, 1);
            var note = ((IHasMultitap)map.HitObjects.Single()).Expand(map.ControlPointInfo).Single();
            map.HitObjects.Clear();
            map.HitObjects.Add(note);
            return map;
        }
        private MultitapPopover popover => Editor.ChildrenOfType<MultitapPopover>().Single(p => p.State.Value == Visibility.Visible);

        [Test]
        public void TestConfigureUndoRedoAndRestoreNormalNote()
        {
            AddStep("select note", () =>
            {
                var note = EditorBeatmap.HitObjects.Single();
                EditorClock.Seek(note.StartTime);
                EditorBeatmap.SelectedHitObjects.Add(note);
            });
            openSettings();
            AddStep("set four taps", () => popover.ChildrenOfType<OsuTextBox>().First().Text = "4");
            accept();
            AddAssert("four taps created", () => EditorBeatmap.HitObjects.Single() is IHasMultitap && EditorBeatmap.HitObjects.Single().NestedHitObjects.Count == 4);
            AddStep("undo", () => Editor.Undo());
            AddAssert("normal note restored", () => EditorBeatmap.HitObjects.Single() is not IHasMultitap);
            AddStep("redo", () => Editor.Redo());
            AddAssert("rhythm restored", () => EditorBeatmap.HitObjects.Single().Multitap.Count == 4);
            AddStep("select restored group", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects.Single());
            });
            openSettings();
            AddStep("set one tap", () => popover.ChildrenOfType<OsuTextBox>().First().Text = "1");
            accept();
            AddAssert("normal note selected", () => EditorBeatmap.SelectedHitObjects.Single() is not IHasMultitap && EditorBeatmap.HitObjects.Single().Multitap == null);
        }
        private void openSettings()
        {
            AddUntilStep("blueprint ready", () => Editor.ChildrenOfType<HitObjectSelectionBlueprint>().Any(b => b.Item == EditorBeatmap.HitObjects.Single()));
            AddStep("configure multitap", () => Editor.ChildrenOfType<HitObjectSelectionBlueprint>().Single(b => b.Item == EditorBeatmap.HitObjects.Single())
                .ContextMenuItems.Single(i => i.Text.Value == "Configure multitap").Action.Value!.Invoke());
            AddUntilStep("popover open", () => Editor.ChildrenOfType<MultitapPopover>().Any(p => p.State.Value == Visibility.Visible));
        }
        private void accept() => AddStep("accept", () => popover.ChildrenOfType<RoundedButton>().Single(b => b.Text == "Accept").TriggerClick());
    }
}
