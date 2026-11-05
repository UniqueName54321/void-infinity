// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Overlays.Settings.Sections.Maintenance
{
    public partial class LazerImportSettings : SettingsSubsection
    {
        protected override LocalisableString Header => "Import osu!lazer library";
        public LazerImportSettings() => Child = new LazerLibraryImportPanel();
    }
}
