// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Configuration;
using osu.Game.Skinning;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osuTK;

namespace osu.Game.Overlays.Settings.Sections.Maintenance
{
    public partial class LazerLibraryImportPanel : FillFlowContainer
    {
        private readonly BindableBool importBeatmaps = new BindableBool(true);
        private readonly BindableBool importSkins = new BindableBool();
        private readonly BindableBool importOptions = new BindableBool();
        private FormTextBox source = null!;
        private OsuTextFlowContainer status = null!;
        private RoundedButton import = null!;
        private LibraryFolderButton browse = null!;
        private RoundedButton cancel = null!;
        private CancellationTokenSource? cancellation;

        public LazerLibraryImportPanel()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, 8);
        }

        [BackgroundDependencyLoader]
        private void load(BeatmapManager beatmaps, SkinManager skins, OsuConfigManager config, Storage storage)
        {
            Children = new Drawable[]
            {
                new OsuSpriteText { Text = "Close osu!lazer first. Its original library stays unchanged.", Font = osu.Game.Graphics.OsuFont.Default.With(size: 14) },
                source = new FormTextBox
                {
                    Caption = "osu!lazer data folder",
                    HintText = "The folder containing client.realm and files. Assets are copied; enough free disk space is required.",
                    PlaceholderText = "Paste the full path to your lazer library",
                },
                browse = new LibraryFolderButton(path => source.Current.Value = path)
                {
                    Text = "Choose folder…", RelativeSizeAxes = Axes.X, Height = 35,
                },
                new FormCheckBox { Caption = "Beatmaps", Current = importBeatmaps },
                new FormCheckBox { Caption = "Skins", Current = importSkins },
                new FormCheckBox { Caption = "Options", HintText = "Replaces supported local preferences. Accounts, online settings, experimental switches and skin selection are excluded.", Current = importOptions },
                import = new RoundedButton
                {
                    Text = "Import selected content from osu!lazer", RelativeSizeAxes = Axes.X, Height = 40,
                    Action = () => startImport(beatmaps, skins, config, storage.GetFullPath(string.Empty)),
                },
                cancel = new RoundedButton { Text = "Cancel import", RelativeSizeAxes = Axes.X, Height = 35, Action = () => cancellation?.Cancel(), Alpha = 0 },
                status = new OsuTextFlowContainer(t => t.Font = osu.Game.Graphics.OsuFont.Default.With(size: 14)) { RelativeSizeAxes = Axes.X, AutoSizeAxes = Axes.Y },
            };
        }

        private void startImport(BeatmapManager beatmaps, SkinManager skins, OsuConfigManager config, string destination)
        {
            if (cancellation != null) return;
            string path = source.Current.Value.Trim();
            cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var selection = new LazerLibraryImportSelection { Beatmaps = importBeatmaps.Value, Skins = importSkins.Value, Options = importOptions.Value };
            importBeatmaps.Disabled = importSkins.Disabled = importOptions.Disabled = true;
            source.ReadOnly = true;
            browse.Enabled.Value = false;
            import.Enabled.Value = false;
            cancel.Show();
            var progress = new Progress<string>(text => Schedule(() => status.Text = text));
            Task.Run(async () =>
            {
                string message;
                try
                {
                    var result = await new LazerLibraryImporter(beatmaps, skins, lines => applyOptions(config, lines, token), config.SettingsFilename).ImportAsync(path, destination, progress, token, selection).ConfigureAwait(false);
                    message = $"Imported or already present: {result.Imported} beatmap sets, {result.SkinsImported} skins, {result.OptionsImported} options. Failed: {result.Failed}.";
                }
                catch (OperationCanceledException) { message = "Import cancelled. Completed imports were kept."; }
                catch (Exception ex) { message = $"Import failed: {ex.Message}"; }
                Schedule(() =>
                {
                    status.Text = message;
                    importBeatmaps.Disabled = importSkins.Disabled = importOptions.Disabled = false;
                    source.ReadOnly = false;
                    browse.Enabled.Value = true;
                    import.Enabled.Value = true;
                    cancel.Hide();
                    cancellation?.Dispose();
                    cancellation = null;
                });
            });
        }

        private Task<int> applyOptions(OsuConfigManager config, string[] lines, CancellationToken token)
        {
            var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            Schedule(() =>
            {
                if (token.IsCancellationRequested || IsDisposed)
                {
                    completion.TrySetCanceled(token);
                    return;
                }
                try { completion.SetResult(config.ImportLocalPreferences(lines)); }
                catch (Exception ex) { completion.SetException(ex); }
            });
            return completion.Task;
        }

        private partial class LibraryFolderButton : RoundedButton, IHasPopover
        {
            private readonly Action<string> selected;

            public LibraryFolderButton(Action<string> selected)
            {
                this.selected = selected;
                Action = this.ShowPopover;
            }

            public Popover GetPopover()
            {
                var directory = new Bindable<DirectoryInfo?>();
                var popover = new OsuPopover();
                var useFolder = new RoundedButton { Text = "Use this folder", RelativeSizeAxes = Axes.X, Height = 35 };
                useFolder.Enabled.Value = false;
                directory.BindValueChanged(e => useFolder.Enabled.Value = e.NewValue != null && LazerLibraryImporter.IsLibrary(e.NewValue.FullName));
                useFolder.Action = () =>
                {
                    if (directory.Value == null || !LazerLibraryImporter.IsLibrary(directory.Value.FullName)) return;
                    selected(directory.Value.FullName);
                    this.HidePopover();
                };
                useFolder.Enabled.Value = false;
                popover.Child = new FillFlowContainer
                {
                    Width = 550, AutoSizeAxes = Axes.Y, Direction = FillDirection.Vertical, Spacing = new Vector2(0, 8),
                    Children = new Drawable[]
                    {
                        new OsuDirectorySelector { RelativeSizeAxes = Axes.X, Height = 320, CurrentPath = { BindTarget = directory } },
                        useFolder,
                    }
                };
                return popover;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            cancellation?.Cancel();
            base.Dispose(isDisposing);
        }
    }
}
