// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Screens.Edit.Components
{
    public static class MultitapEditor
    {
        public static void Apply(EditorBeatmap beatmap, HitObject original, MultitapSettings settings, Func<bool, HitObject> create)
        {
            if (!beatmap.HitObjects.Contains(original)) return;
            var replacement = create(settings.Count > 1);
            replacement.StartTime = original.StartTime;
            replacement.Samples = original.Samples.ToList();
            replacement.Multitap = settings.Count > 1 ? settings : null!;
            beatmap.BeginChange();
            beatmap.Remove(original);
            beatmap.Add(replacement);
            beatmap.SelectedHitObjects.Clear();
            beatmap.SelectedHitObjects.Add(replacement);
            beatmap.EndChange();
        }
    }
}
