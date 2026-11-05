// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Localisation;
using osu.Game.Overlays.Settings.Sections.Maintenance;

namespace osu.Game.Overlays.FirstRunSetup
{
    [LocalisableDescription(typeof(ScreenImportFromLazer), nameof(Title))]
    public partial class ScreenImportFromLazer : WizardScreen
    {
        public static LocalisableString Title => "Import from osu!lazer";

        [BackgroundDependencyLoader]
        private void load() => Content.Add(new LazerLibraryImportPanel());
    }
}
