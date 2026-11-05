// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Replays;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public class TaikoModAutoplay : ModAutoplay
    {
        [SettingSource("Human Play", "Human-like movement and imperfect timing. A mandatory 95%-opacity autoplay marker remains in the bottom-left corner of gameplay.")]
        public override BindableBool HumanPlay => base.HumanPlay;

        [SettingSource("Timing offset", "Shift all replay input by this many milliseconds. Nonzero values can affect accuracy.")]
        public override BindableDouble TimingOffset => base.TimingOffset;

        [SettingSource("Random seed", "Reproduce randomized autoplay choices with the same seed.")]
        public override BindableInt Seed => base.Seed;

        [SettingSource("Alternate hands", "Alternate ordinary hits between left and right hands.")]
        public BindableBool AlternateHands { get; } = new BindableBool(true);

        [SettingSource("Start with right hand", "Use the right hand first, or throughout single-hand play.")]
        public BindableBool StartRight { get; } = new BindableBool(false);

        [SettingSource("Swell hit interval", "Milliseconds between swell hits; autoplay accelerates when required to finish the swell.")]
        public BindableDouble SwellInterval { get; } = new BindableDouble(50) { MinValue = 10, MaxValue = 100, Precision = 1 };

        [SettingSource("Extra drumroll hits", "Additional evenly spaced hits between drumroll ticks.")]
        public BindableInt ExtraRollHits { get; } = new BindableInt(0) { MinValue = 0, MaxValue = 4 };

        [SettingSource("Randomize hands", "Choose the hand for ordinary hits using the shared random seed.")]
        public BindableBool RandomizeHands { get; } = new BindableBool();

        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => WithReplaySettings(new TaikoAutoGenerator(beatmap) { HumanPlay = HumanPlay.Value, AlternateHands = AlternateHands.Value, StartRight = StartRight.Value, SwellInterval = SwellInterval.Value, ExtraRollHits = ExtraRollHits.Value, RandomizeHands = RandomizeHands.Value, Seed = Seed.Value }.Generate(), "mekkadosu!");

        public override Type[] IncompatibleMods => base.IncompatibleMods.Concat(new[] { typeof(TaikoModSingleTap) }).ToArray();
    }
}
