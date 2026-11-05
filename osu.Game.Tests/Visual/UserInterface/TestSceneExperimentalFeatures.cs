// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Settings.Sections;

namespace osu.Game.Tests.Visual.UserInterface
{
    public partial class TestSceneExperimentalFeatures : ScreenTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colours = new OverlayColourProvider(OverlayColourScheme.Purple);

        private OsuConfigManager config = null!;
        private ExperimentalFeaturesSection section = null!;
        protected override bool UseFreshStoragePerRun => true;

        [BackgroundDependencyLoader]
        private void load()
        {
            Dependencies.Cache(config = new OsuConfigManager(LocalStorage));
        }

        [Test]
        public void TestGateRequiresConfirmationAndHidesFeaturesWhenDisabled()
        {
            AddStep("load section", () => Child = section = new ExperimentalFeaturesSection());
            AddUntilStep("master loaded", () => section.ChildrenOfType<SettingsCheckbox>().Any());
            AddAssert("features initially absent", () => !section.ChildrenOfType<SettingsCheckbox>().Any(c => c.LabelText == "Test"));
            AddStep("request enable", () => section.ChildrenOfType<SettingsCheckbox>().Single().Current.Value = true);
            AddUntilStep("warning shown", () => DialogOverlay.ChildrenOfType<ExperimentalFeaturesWarning>().Any(d => d.IsPresent));
            AddAssert("gate stays off pending confirmation", () => !config.Get<bool>(OsuSetting.EnableExperimentalFeatures));
            AddStep("cancel warning", () => DialogOverlay.ChildrenOfType<PopupDialogCancelButton>().Single(b => b.IsPresent).TriggerClick());
            AddAssert("features remain absent", () => !section.ChildrenOfType<SettingsCheckbox>().Any(c => c.LabelText == "Test"));
            AddStep("request again", () => section.ChildrenOfType<SettingsCheckbox>().Single().Current.Value = true);
            AddUntilStep("new warning ready", () => DialogOverlay.ChildrenOfType<PopupDialogDangerousButton>().Any(b => b.IsPresent));
            AddStep("confirm", () => DialogOverlay.ChildrenOfType<PopupDialogDangerousButton>().Last(b => b.IsPresent).TriggerClick());
            AddUntilStep("test option visible", () => section.ChildrenOfType<SettingsCheckbox>().Any(c => c.LabelText == "Test"));
            AddAssert("test starts disabled", () => !config.Get<bool>(OsuSetting.ExperimentalTest));
            AddStep("enable test directly", () => section.ChildrenOfType<SettingsCheckbox>().Single(c => c.LabelText == "Test").Current.Value = true);
            AddAssert("test enabled without warning", () => config.Get<bool>(OsuSetting.ExperimentalTest) && DialogOverlay.CurrentDialog == null);
            AddStep("disable gate", () => section.ChildrenOfType<SettingsCheckbox>().Single(c => c.LabelText == "Enable Experimental Features").Current.Value = false);
            AddAssert("test removed from list", () => !section.ChildrenOfType<SettingsCheckbox>().Any(c => c.LabelText == "Test"));
        }

        protected override void Dispose(bool isDisposing)
        {
            config?.Dispose();
            base.Dispose(isDisposing);
        }
    }
}
