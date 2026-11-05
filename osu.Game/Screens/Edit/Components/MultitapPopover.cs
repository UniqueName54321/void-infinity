// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Objects;
using osuTK;

namespace osu.Game.Screens.Edit.Components
{
    public partial class MultitapPopover : OsuPopover
    {
        public MultitapPopover(MultitapSettings? initial, Action<MultitapSettings> accept)
        {
            initial ??= new MultitapSettings();
            var count = input(initial.Count);
            var beats = input(initial.BeatsPerBar);
            var unit = input(initial.BeatUnit);
            var subdivision = input(initial.Subdivision);
            var error = new OsuSpriteText { Font = osu.Game.Graphics.OsuFont.Default.With(size: 14) };
            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Width = 440, AutoSizeAxes = Axes.Y, Direction = FillDirection.Vertical, Spacing = new Vector2(0, 6),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText { Text = "Multitap (.vib only) · 1 tap restores a normal note" },
                        new OsuSpriteText { Text = "Number of taps (1–1024)" }, count,
                        new OsuSpriteText { Text = "Time signature: beats per bar (1–32)" }, beats,
                        new OsuSpriteText { Text = "Time signature: beat unit (1, 2, 4, 8, 16, 32, 64)" }, unit,
                        new OsuSpriteText { Text = "Taps per beat (1–64; 4 = quarter-beat stream in 4/4)" }, subdivision,
                        error,
                        new RoundedButton
                        {
                            Text = "Accept", Size = new Vector2(130, 30),
                            Action = () =>
                            {
                                if (!int.TryParse(count.Text, out int c) || !int.TryParse(beats.Text, out int b)
                                    || !int.TryParse(unit.Text, out int u) || !int.TryParse(subdivision.Text, out int d))
                                {
                                    error.Text = "Enter whole numbers in all fields.";
                                    return;
                                }
                                var settings = new MultitapSettings { Count = c, BeatsPerBar = b, BeatUnit = u, Subdivision = d };
                                try { settings.Validate(); }
                                catch (InvalidDataException) { error.Text = "Check the ranges and use a power-of-two beat unit."; return; }
                                // Retain per-tap samples when only the rhythm is edited.
                                if (initial.Samples.Count == c) settings.Samples = initial.Samples;
                                accept(settings);
                                Hide();
                            },
                        },
                        new RoundedButton { Text = "Cancel", Size = new Vector2(130, 30), Action = Hide },
                    },
                },
            };
        }

        private static OsuTextBox input(int value) => new OsuTextBox
        {
            RelativeSizeAxes = Axes.X, Height = 30, Text = value.ToString(CultureInfo.InvariantCulture),
        };
    }
}
