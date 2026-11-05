// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Replays;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Mods
{
    public abstract class ModAutoplay : Mod, ICreateReplayData
    {
        public virtual BindableBool HumanPlay { get; } = new BindableBool();

        protected virtual double HumanTimingScale => 1;

        public virtual BindableDouble TimingOffset { get; } = new BindableDouble(0) { MinValue = -50, MaxValue = 50, Precision = 1 };

        public virtual BindableInt Seed { get; } = new BindableInt(1) { MinValue = 0, MaxValue = 2147483647 };

        public override string Name => "Autoplay";
        public override string Acronym => "AT";
        public override IconUsage? Icon => OsuIcon.ModAutoplay;
        public override ModType Type => ModType.Automation;
        public override LocalisableString Description => "Watch a customizable automated play through the song.";
        public override double ScoreMultiplier => 1;

        public sealed override bool UserPlayable => false;
        public sealed override bool ValidForMultiplayer => false;
        public sealed override bool ValidForMultiplayerAsFreeMod => false;

        public override Type[] IncompatibleMods => new[] { typeof(ModCinema), typeof(ModRelax), typeof(ModAdaptiveSpeed), typeof(ModTouchDevice) };

        public override bool HasImplementation => GetType().GenericTypeArguments.Length == 0;

        protected ModReplayData WithReplaySettings(Replay replay, string username)
        {
            foreach (var frame in replay.Frames)
            {
                // Smooth, bounded timing drift preserves frame order and press/release relationships.
                // Its derivative stays well below one, so the resulting timeline remains monotonic.
                double drift = HumanPlay.Value ? HumanTimingScale * (12 * HumanPlayNoise.Sample(frame.Time, 1600, Seed.Value, 1)
                    + 10 * HumanPlayNoise.Sample(frame.Time, 230, Seed.Value, 2)
                    + 4 * HumanPlayNoise.Sample(frame.Time, 47, Seed.Value, 3)) : 0;
                frame.Time += TimingOffset.Value + drift;
            }
            return new ModReplayData(replay, new ModCreatedUser { Username = username });
        }

        public virtual ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods) => new ModReplayData(new Replay(), new ModCreatedUser { Username = @"autoplay" });
    }
}
