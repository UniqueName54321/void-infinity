// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Replays;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.Mods
{
    public class ManiaModAutoplay : ModAutoplay
    {
        [SettingSource("Human Play", "Human-like movement and imperfect timing. A mandatory 95%-opacity autoplay marker remains in the bottom-left corner of gameplay.")]
        public override BindableBool HumanPlay => base.HumanPlay;

        [SettingSource("Timing offset", "Shift all replay input by this many milliseconds. Nonzero values can affect accuracy.")]
        public override BindableDouble TimingOffset => base.TimingOffset;

        [SettingSource("Random seed", "Reproduce randomized autoplay choices with the same seed.")]
        public override BindableInt Seed => base.Seed;

        [SettingSource("Tap duration", "How long to hold ordinary notes, in milliseconds. Dense notes still release before the next press.")]
        public BindableDouble TapDuration { get; } = new BindableDouble(20) { MinValue = 1, MaxValue = 150, Precision = 1 };

        [SettingSource("Chord spread", "Spread simultaneous key presses across this many milliseconds. Can affect accuracy.")]
        public BindableDouble ChordSpread { get; } = new BindableDouble(0) { MinValue = 0, MaxValue = 30, Precision = 1 };

        [SettingSource("Hold release offset", "Release long notes earlier or later, in milliseconds. Can affect accuracy.")]
        public BindableDouble HoldReleaseOffset { get; } = new BindableDouble(0) { MinValue = -30, MaxValue = 30, Precision = 1 };

        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => WithReplaySettings(new ManiaAutoGenerator((ManiaBeatmap)beatmap) { HumanPlay = HumanPlay.Value, TapDuration = TapDuration.Value, ChordSpread = ChordSpread.Value, HoldReleaseOffset = HoldReleaseOffset.Value, Seed = Seed.Value }.Generate(), "osu!topus");
    }
}
