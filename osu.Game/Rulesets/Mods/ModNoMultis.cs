// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Mods
{
    public class ModNoMultis : Mod, IApplicableAfterBeatmapConversion
    {
        public override string Name => "No Multis";
        public override string Acronym => "NMS";
        public override LocalisableString Description => "Replace multitaps with ordinary streams, keeping their rhythm and hitsounds.";
        public override IconUsage? Icon => FontAwesome.Solid.Stream;
        public override ModType Type => ModType.Conversion;
        public override double ScoreMultiplier => 1;
        public override bool ValidForMultiplayer => false;
        public override bool ValidForMultiplayerAsFreeMod => false;
        public void ApplyToBeatmap(IBeatmap beatmap) => MultitapUtils.ReplaceWithStreams(beatmap);
    }
}
