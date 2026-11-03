// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Text;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.IO;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Beatmaps.Formats;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Screens.Edit
{
    public partial class BeatmapEditorChangeHandler : EditorChangeHandler
    {
        private readonly LegacyEditorBeatmapPatcher patcher;
        private readonly EditorBeatmap editorBeatmap;

        /// <summary>
        /// Creates a new <see cref="EditorChangeHandler"/>.
        /// </summary>
        /// <param name="editorBeatmap">The <see cref="EditorBeatmap"/> to track the <see cref="HitObject"/>s of.</param>
        public BeatmapEditorChangeHandler(EditorBeatmap editorBeatmap)
        {
            this.editorBeatmap = editorBeatmap;

            editorBeatmap.TransactionBegan += BeginChange;
            editorBeatmap.TransactionEnded += EndChange;
            editorBeatmap.SaveStateTriggered += SaveState;

            patcher = new LegacyEditorBeatmapPatcher(editorBeatmap);
        }

        protected override void WriteCurrentStateToStream(MemoryStream stream)
        {
            using (var sw = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                new VibBeatmapEncoder(editorBeatmap, editorBeatmap.BeatmapSkin).Encode(sw);
        }

        protected override void ApplyStateChange(byte[] previousState, byte[] newState)
        {
            var previous = read(previousState);
            var next = read(newState);
            editorBeatmap.BeginChange();
            try
            {
                // Legacy patch indices refer only to playable objects.
                foreach (var e in editorBeatmap.HitObjects.Where(h => h is IHasBeatmapEvent).ToArray())
                    editorBeatmap.Remove(e);
                patcher.Patch(legacyBytes(previous), legacyBytes(next));
                var oldPaths = editorBeatmap.HitObjects.OfType<IHasPath>().ToArray();
                var newPaths = next.HitObjects.OfType<IHasPath>().ToArray();
                for (int i = 0; i < oldPaths.Length; i++)
                {
                    oldPaths[i].Path.ControlPoints.Clear();
                    oldPaths[i].Path.ControlPoints.AddRange(newPaths[i].Path.ControlPoints.Select(p => p.Clone()));
                }
                next.BeatmapInfo.Ruleset = editorBeatmap.BeatmapInfo.Ruleset;
                var converted = next.BeatmapInfo.Ruleset.CreateInstance().CreateBeatmapConverter(next).Convert();
                foreach (var e in converted.HitObjects.Where(h => h is IHasBeatmapEvent))
                    editorBeatmap.Add(e);
                editorBeatmap.ClampSliderMovement = next.ClampSliderMovement;
                editorBeatmap.UpdateAllHitObjects();
            }
            finally { editorBeatmap.EndChange(); }
        }

        private static Beatmap read(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new LineBufferedReader(stream, true);
            return osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
        }

        private static byte[] legacyBytes(IBeatmap beatmap)
        {
            using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            new LegacyBeatmapEncoder(beatmap, null).Encode(writer);
            return Encoding.UTF8.GetBytes(writer.ToString());
        }
    }
}
