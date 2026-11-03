// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Framework.Extensions;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    public partial class BeatmapEventPopover : OsuPopover
    {
        private readonly BeatmapEvent beatmapEvent;
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        public BeatmapEventPopover(BeatmapEvent beatmapEvent) => this.beatmapEvent = beatmapEvent;

        [BackgroundDependencyLoader]
        private void load()
        {
            var fields = new List<OsuTextBox>();
            var flow = new FillFlowContainer { Width = 280, AutoSizeAxes = Axes.Y, Direction = FillDirection.Vertical, Spacing = new Vector2(4) };
            var type = new FormEnumDropdown<BeatmapEventType> { Caption = "Event type" };
            type.Current.Value = beatmapEvent.Change.Type;
            flow.Add(type);
            flow.Add(new OsuSpriteText { Text = "Blank = keep the current value", Font = osu.Game.Graphics.OsuFont.Default.With(size: 14) });
            string[] names = { "Circle size", "Approach rate", "Overall difficulty", "HP drain", "Slider multiplier", "Slider tick rate" };
            double?[] values = { beatmapEvent.Change.CircleSize, beatmapEvent.Change.ApproachRate, beatmapEvent.Change.OverallDifficulty,
                beatmapEvent.Change.DrainRate, beatmapEvent.Change.SliderMultiplier, beatmapEvent.Change.SliderTickRate };
            for (int i = 0; i < names.Length; i++)
            {
                flow.Add(new OsuSpriteText { Text = names[i], Font = osu.Game.Graphics.OsuFont.Default.With(size: 14) });
                var field = new OsuTextBox { Name = names[i], RelativeSizeAxes = Axes.X, Height = 28, Text = values[i]?.ToString(CultureInfo.InvariantCulture) ?? string.Empty };
                flow.Add(field);
                fields.Add(field);
            }
            var error = new OsuSpriteText { Font = osu.Game.Graphics.OsuFont.Default.With(size: 13), Colour = Colour4.Red };
            flow.Add(error);
            flow.Add(new RoundedButton
            {
                Text = "Accept", Size = new Vector2(130, 30), Action = () =>
                {
                    var parsed = new double?[6];
                    for (int i = 0; i < parsed.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(fields[i].Text)) continue;
                        if (!double.TryParse(fields[i].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value))
                        { error.Text = "Enter numbers or leave fields blank."; return; }
                        parsed[i] = value;
                    }
                    var change = new BeatmapAttributeChange { Type = type.Current.Value, CircleSize = (float?)parsed[0], ApproachRate = (float?)parsed[1],
                        OverallDifficulty = (float?)parsed[2], DrainRate = (float?)parsed[3], SliderMultiplier = parsed[4], SliderTickRate = parsed[5] };
                    try { change.Validate(); }
                    catch (ArgumentOutOfRangeException) { error.Text = "Difficulty: 0–10; slider settings: (0, 100]."; return; }
                    editorBeatmap.BeginChange();
                    beatmapEvent.Change = change;
                    editorBeatmap.UpdateAllHitObjects();
                    editorBeatmap.EndChange();
                    this.HidePopover();
                }
            });
            Child = flow;
        }
    }
}
