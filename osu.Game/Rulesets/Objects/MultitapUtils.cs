// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Rulesets.Objects
{
    public static class MultitapUtils
    {
        public static IEnumerable<HitObject> CreateTaps(HitObject owner, ControlPointInfo timing, Func<HitObject> create)
        {
            var times = owner.Multitap.GetTapTimes(owner.StartTime, timing);
            for (int i = 0; i < times.Length; i++)
            {
                var tap = create();
                tap.StartTime = times[i];
                tap.Samples = (owner.Multitap.Samples.Count == times.Length ? owner.Multitap.Samples[i] : owner.Samples).ToList();
                if (owner is osu.Game.Rulesets.Objects.Types.IHasCombo source && tap is osu.Game.Rulesets.Objects.Types.IHasComboInformation combo)
                {
                    combo.NewCombo = i == 0 && source.NewCombo;
                    combo.ComboOffset = i == 0 ? source.ComboOffset : 0;
                }
                yield return tap;
            }
        }

        public static IEnumerable<HitObject> PlayableObjects(IEnumerable<HitObject> objects) => objects
            .SelectMany(h => h is IHasMultitap ? h.NestedHitObjects.ToArray() : new[] { h }).OrderBy(h => h.StartTime);

        public static void ReplaceWithStreams(IBeatmap beatmap, bool useNestedObjects = false)
        {
            if (!beatmap.HitObjects.Any(h => h is IHasMultitap)) return;
            var expanded = beatmap.HitObjects.SelectMany(h => h is IHasMultitap multi
                    ? useNestedObjects ? h.NestedHitObjects.ToArray() : multi.Expand(beatmap.ControlPointInfo)
                    : new[] { h }).OrderBy(h => h.StartTime).ToArray();
            var target = (IList)beatmap.HitObjects;
            target.Clear();
            foreach (var hitObject in expanded) target.Add(hitObject);
        }
    }
}
