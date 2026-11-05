// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Testing;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.FirstRunSetup;
using osu.Game.Overlays.Settings.Sections.Maintenance;

namespace osu.Game.Tests.Visual.UserInterface
{
    public partial class TestSceneLazerLibraryImport : OsuManualInputManagerTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        [Test]
        public void TestSettingsPanelReportsInvalidPathAndRemainsUsable()
        {
            AddStep("load settings", () => Child = new PopoverContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new LazerImportSettings { Width = 500, RelativeSizeAxes = Axes.None },
            });
            AddUntilStep("panel ready", () => this.ChildrenOfType<FormTextBox>().Any());
            AddStep("invalid path", () => this.ChildrenOfType<FormTextBox>().Single().Current.Value = "/missing-void-fixture");
            AddStep("import", () => this.ChildrenOfType<RoundedButton>().Single(b => b.Text == "Import selected content from osu!lazer").TriggerClick());
            AddUntilStep("error displayed", () => this.ChildrenOfType<OsuTextFlowContainer>().Any(t => t.ChildrenOfType<OsuSpriteText>().Any(text => text.Text.ToString().Contains("failed", StringComparison.Ordinal))));
            AddAssert("can correct path", () => !this.ChildrenOfType<FormTextBox>().Single().ReadOnly);
            AddAssert("import enabled", () => this.ChildrenOfType<RoundedButton>().Single(b => b.Text == "Import selected content from osu!lazer").Enabled.Value);
            AddStep("open chooser", () => this.ChildrenOfType<RoundedButton>().Single(b => b.Text == "Choose folder…").TriggerClick());
            AddUntilStep("chooser loaded", () => this.ChildrenOfType<OsuDirectorySelector>().Any());
        }

        [Test]
        public void TestFirstRunScreenHasSameImportControls()
        {
            AddStep("load first run screen", () => Child = new PopoverContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new osu.Framework.Screens.ScreenStack(new ScreenImportFromLazer()),
            });
            AddUntilStep("panel loaded", () => this.ChildrenOfType<LazerLibraryImportPanel>().Any(p => p.IsLoaded));
            AddAssert("independent content toggles", () => this.ChildrenOfType<FormCheckBox>().Count() == 3);
            AddAssert("source selector present", () => this.ChildrenOfType<FormTextBox>().Any());
            AddAssert("import action present", () => this.ChildrenOfType<RoundedButton>().Any(b => b.Text == "Import selected content from osu!lazer"));
        }
    }
}
