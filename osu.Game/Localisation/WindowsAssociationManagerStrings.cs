// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class WindowsAssociationManagerStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.WindowsAssociationManager";

        /// <summary>
        /// "void!infinity Beatmap"
        /// </summary>
        public static LocalisableString OsuBeatmap => new TranslatableString(getKey(@"osu_beatmap"), @"void!infinity Beatmap");

        /// <summary>
        /// "void!infinity Replay"
        /// </summary>
        public static LocalisableString OsuReplay => new TranslatableString(getKey(@"osu_replay"), @"void!infinity Replay");

        /// <summary>
        /// "void!infinity Skin"
        /// </summary>
        public static LocalisableString OsuSkin => new TranslatableString(getKey(@"osu_skin"), @"void!infinity Skin");

        /// <summary>
        /// "void!infinity"
        /// </summary>
        public static LocalisableString OsuProtocol => new TranslatableString(getKey(@"osu_protocol"), @"void!infinity");

        /// <summary>
        /// "void!infinity Multiplayer"
        /// </summary>
        public static LocalisableString OsuMultiplayer => new TranslatableString(getKey(@"osu_multiplayer"), @"void!infinity Multiplayer");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
