// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Replays;

namespace osu.Game.Rulesets.Osu.Mods
{
    public enum AutoplayCursorStyle { Natural, Linear, Dance }
    public enum AutoplayTapStyle { Adaptive, Alternate, SingleTap }
    public enum AutoplaySpinDirection { Automatic, Clockwise, Counterclockwise }

    public class OsuModAutoplay : ModAutoplay
    {
        [SettingSource("Human Play", "Human-like movement and imperfect timing. A mandatory 95%-opacity autoplay marker remains in the bottom-left corner of gameplay.")]
        public override BindableBool HumanPlay => base.HumanPlay;

        [SettingSource("Timing offset", "Shift all replay input by this many milliseconds. Nonzero values can affect accuracy.")]
        public override BindableDouble TimingOffset => base.TimingOffset;

        [SettingSource("Random seed", "Reproduce randomized autoplay choices with the same seed.")]
        public override BindableInt Seed => base.Seed;

        [SettingSource("Cursor style", "Natural movement, straight lines, or curved cursor dancing between objects.")]
        public Bindable<AutoplayCursorStyle> CursorStyle { get; } = new Bindable<AutoplayCursorStyle>();

        [SettingSource("Tapping style", "Adaptive alternation, full alternation, or single tapping. Overlaps still alternate when needed.")]
        public Bindable<AutoplayTapStyle> TappingStyle { get; } = new Bindable<AutoplayTapStyle>();

        [SettingSource("Spinner direction", "Choose the direction of spinner movement.")]
        public Bindable<AutoplaySpinDirection> SpinnerDirection { get; } = new Bindable<AutoplaySpinDirection>();

        [SettingSource("Dance strength", "Width of cursor dance arcs in playfield pixels.")]
        public BindableDouble DanceStrength { get; } = new BindableDouble(60) { MinValue = 0, MaxValue = 200, Precision = 1 };

        [SettingSource("Spinner speed", "Spinner rotations per minute. Low speeds may miss spinner requirements.")]
        public BindableDouble SpinnerRPM { get; } = new BindableDouble(477.464829) { MinValue = 100, MaxValue = 1000 };

        [SettingSource("Reaction time", "Delay before moving towards a newly visible object, in milliseconds.")]
        public BindableDouble ReactionTime { get; } = new BindableDouble(100) { MinValue = 0, MaxValue = 300, Precision = 1 };

        [SettingSource("Delayed movement", "Stay on objects longer before moving to the next one.")]
        public BindableBool DelayedMovement { get; } = new BindableBool(false);

        public override Type[] IncompatibleMods => base.IncompatibleMods.Concat(new[] { typeof(OsuModMagnetised), typeof(OsuModRepel), typeof(OsuModAutopilot), typeof(OsuModSpunOut), typeof(OsuModAlternate), typeof(OsuModSingleTap) }).ToArray();

        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => WithReplaySettings(new OsuAutoGenerator(beatmap, mods)
            {
                HumanPlay = HumanPlay.Value,
                CursorStyle = CursorStyle.Value,
                TappingStyle = TappingStyle.Value,
                DanceStrength = DanceStrength.Value,
                SpinnerRPM = SpinnerRPM.Value,
                SpinnerDirection = SpinnerDirection.Value,
                ReactionTime = ReactionTime.Value,
                DelayedMovements = DelayedMovement.Value,
                Seed = Seed.Value,
            }.Generate(), "Autoplay");
    }
}
