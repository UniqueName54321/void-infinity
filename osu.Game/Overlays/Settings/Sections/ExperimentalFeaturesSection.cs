// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Overlays.Settings.Sections
{
    public partial class ExperimentalFeaturesSection : SettingsSection
    {
        public override LocalisableString Header => "Experimental Features";
        public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.Flask };
        public ExperimentalFeaturesSection() => Add(new ExperimentalFeaturesSettings());
    }

    public partial class ExperimentalFeaturesSettings : SettingsSubsection
    {
        protected override LocalisableString Header => "Experimental Features";
        private readonly BindableBool requested = new BindableBool();
        private IBindable<bool> enabled = null!;
        private SettingsCheckbox test = null!;
        private bool updating;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, IDialogOverlay? dialogs)
        {
            enabled = config.GetBindable<bool>(OsuSetting.EnableExperimentalFeatures);
            test = new SettingsCheckbox { LabelText = "Test", Current = config.GetBindable<bool>(OsuSetting.ExperimentalTest), TooltipText = "A placeholder experimental feature. Enabling it does nothing." };
            Add(new SettingsCheckbox { LabelText = "Enable Experimental Features", Current = requested });
            requested.BindValueChanged(e =>
            {
                if (updating) return;
                if (!e.NewValue) { config.SetValue(OsuSetting.EnableExperimentalFeatures, false); return; }
                updating = true;
                requested.Value = false;
                updating = false;
                dialogs?.Push(new ExperimentalFeaturesWarning(() => config.SetValue(OsuSetting.EnableExperimentalFeatures, true)));
            });
            enabled.BindValueChanged(e =>
            {
                updating = true;
                requested.Value = e.NewValue;
                updating = false;
                if (e.NewValue && test.Parent == null) Add(test);
                else if (!e.NewValue && test.Parent != null) Remove(test, false);
            }, true);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (test?.Parent == null) test?.Dispose();
            base.Dispose(isDisposing);
        }
    }

    public partial class ExperimentalFeaturesWarning : PopupDialog
    {
        public ExperimentalFeaturesWarning(Action enable)
        {
            HeaderText = "Enable experimental features?";
            BodyText = "Experimental features are unfinished and may cause bugs, crashes, or data loss. Enable them only if you accept these risks. Each feature is controlled separately.";
            Icon = FontAwesome.Solid.ExclamationTriangle;
            Buttons = new PopupDialogButton[]
            {
                new PopupDialogDangerousButton { Text = "Enable experimental features", Action = enable },
                new PopupDialogCancelButton { Text = "Cancel" },
            };
        }
    }
}
