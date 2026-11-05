// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Rulesets.Objects
{
    public interface IHasMultitap
    {
        /// <summary>Create independent, ordinary notes without changing the authored group.</summary>
        IEnumerable<HitObject> Expand(ControlPointInfo timing);
    }
}
