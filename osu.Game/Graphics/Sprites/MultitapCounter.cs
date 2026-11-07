// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Graphics.Sprites
{
    /// <summary>A group counter which must not reveal notes hidden by a visibility mod.</summary>
    public partial class MultitapCounter : OsuSpriteText
    {
        [BackgroundDependencyLoader(true)]
        private void load(IReadOnlyList<Mod>? mods)
        {
            if (mods?.Any(m => m is ModHidden) == true)
                Alpha = 0;
        }
    }
}
