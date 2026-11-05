// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Catch.Mods
{
    public class CatchModAutoplay : ModAutoplay
    {
        [SettingSource("Human Play", "Human-like movement and imperfect timing. A mandatory 95%-opacity autoplay marker remains in the bottom-left corner of gameplay.")]
        public override BindableBool HumanPlay => base.HumanPlay;

        [SettingSource("Timing offset", "Shift all replay input by this many milliseconds. Nonzero values can affect accuracy.")]
        public override BindableDouble TimingOffset => base.TimingOffset;

        [SettingSource("Random seed", "Reproduce randomized autoplay choices with the same seed.")]
        public override BindableInt Seed => base.Seed;

        [SettingSource("Catcher aim offset", "Aim away from fruit centres, as a fraction of catcher half-width.")]
        public BindableDouble AimOffset { get; } = new BindableDouble(0) { MinValue = -0.8, MaxValue = 0.8, Precision = 0.05 };

        [SettingSource("Move early", "Start travelling to the next fruit immediately rather than waiting.")]
        public BindableBool MoveEarly { get; } = new BindableBool(false);

        [SettingSource("Always dash", "Hold dash whenever travelling between fruit.")]
        public BindableBool AlwaysDash { get; } = new BindableBool(false);

        [SettingSource("Randomize aim", "Vary aim within the catcher using the shared random seed. Aim offset sets the variation width, or half the catcher radius when zero.")]
        public BindableBool RandomizeAim { get; } = new BindableBool();

        protected override double HumanTimingScale => 0.25;

        public override Type[] IncompatibleMods => base.IncompatibleMods.Concat(new[] { typeof(CatchModMovingFast) }).ToArray();

        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => WithReplaySettings(new CatchAutoGenerator(beatmap) { HumanPlay = HumanPlay.Value, AimOffset = AimOffset.Value, MoveEarly = MoveEarly.Value, AlwaysDash = AlwaysDash.Value, RandomizeAim = RandomizeAim.Value, Seed = Seed.Value }.Generate(), "osu!salad");
    }
}
