using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Core.Tests.Support;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class ShellViewModelCommandsTests
    {
        private sealed class InMemorySnipStore(SnipStoreDocument document) : ISnipStore
        {
            public SnipStoreDocument Document { get; private set; } = document;

            public int SaveCount { get; private set; }

            public string FilePath => "in-memory";

            public Task<SnipStoreDocument> LoadAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(Document);

            public Task SaveAsync(SnipStoreDocument document, CancellationToken cancellationToken = default)
            {
                Document = document;
                SaveCount++;
                return Task.CompletedTask;
            }
        }

        private static async Task<(ShellViewModel vm, InMemorySnipStore store, FakeClipboardService clip, FakeShellInteractions ix, FakeClock clock)> BuildAsync(Action<SnipStoreDocument>? configure = null)
        {
            var doc = new SnipStoreDocument();
            configure?.Invoke(doc);
            var store = new InMemorySnipStore(doc);
            var clip = new FakeClipboardService();
            var ix = new FakeShellInteractions();
            var clock = new FakeClock(new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero));
            var vm = new ShellViewModel(store, clip, clock, ix, new FakeIconAssetStorage(), new FakeExternalLinkService());
            await vm.LoadAsync();
            return (vm, store, clip, ix, clock);
        }

        private static (Cli cli, Snip snip) SeedOneCliOneSnip(SnipStoreDocument doc)
        {
            var cli = new Cli { Name = "pl-app" };
            var snip = new Snip { CliId = cli.Id, Title = "List", CommandTemplate = "pl-app list" };
            doc.Clis.Add(cli);
            doc.Snips.Add(snip);
            return (cli, snip);
        }

        [Fact]
        public async Task CopySnip_with_no_parameters_writes_clipboard_directly_and_bumps_usage()
        {
            Cli cli = null!;
            Snip snip = null!;
            var (vm, store, clip, _, clock) = await BuildAsync(d => (cli, snip) = SeedOneCliOneSnip(d));

            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);
            var card = ((CliViewModel)vm.CurrentContent!).Snips[0];

            await vm.CopySnipCommand.ExecuteAsync(card);

            Assert.Equal("pl-app list", clip.LastText);
            Assert.Equal(1, store.Document.Snips[0].UsageCount);
            Assert.Equal(clock.UtcNow, store.Document.Snips[0].LastUsedAt);
        }

        [Fact]
        public async Task CopySnip_opens_the_flyout_for_a_described_snip_even_without_parameters()
        {
            Cli cli = null!;
            var (vm, _, clip, ix, _) = await BuildAsync(d =>
            {
                cli = new Cli { Name = "pl-app" };
                d.Clis.Add(cli);
                // No parameters, but a description worth reading before copy.
                d.Snips.Add(new Snip
                {
                    CliId = cli.Id,
                    Title = "Status",
                    CommandTemplate = "pl-app status",
                    Description = "Shows **cluster** status.",
                });
            });

            ix.NextParameterFillResult = new ParameterFillResult("pl-app status");
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);
            var card = ((CliViewModel)vm.CurrentContent!).Snips[0];

            await vm.CopySnipCommand.ExecuteAsync(card);

            // The flyout was shown (so the description renders) rather than copying directly.
            Assert.NotNull(ix.LastFilledSnip);
            Assert.Equal("pl-app status", clip.LastText);
        }

        [Fact]
        public async Task CopySnip_with_parameters_uses_resolved_command_from_interactions()
        {
            Cli cli = null!;
            Snip snip = null!;
            var (vm, _, clip, ix, _) = await BuildAsync(d =>
            {
                cli = new Cli { Name = "pl-app" };
                snip = new Snip
                {
                    CliId = cli.Id,
                    Title = "Echo",
                    CommandTemplate = "echo {name}",
                    Parameters = [new Parameter { Name = "name" }],
                };
                d.Clis.Add(cli);
                d.Snips.Add(snip);
            });

            ix.NextParameterFillResult = new ParameterFillResult("echo hello");
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);
            var card = ((CliViewModel)vm.CurrentContent!).Snips[0];

            await vm.CopySnipCommand.ExecuteAsync(card);

            Assert.Equal("echo hello", clip.LastText);
            Assert.Same(snip, ix.LastFilledSnip);
        }

        [Fact]
        public async Task CopySnip_does_nothing_when_user_cancels_the_fill_flyout()
        {
            Cli cli = null!;
            var (vm, store, clip, ix, _) = await BuildAsync(d =>
            {
                cli = new Cli { Name = "pl-app" };
                d.Clis.Add(cli);
                d.Snips.Add(new Snip
                {
                    CliId = cli.Id,
                    Title = "Echo",
                    CommandTemplate = "echo {name}",
                    Parameters = [new Parameter { Name = "name" }],
                });
            });

            ix.NextParameterFillResult = null;
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);
            var card = ((CliViewModel)vm.CurrentContent!).Snips[0];

            await vm.CopySnipCommand.ExecuteAsync(card);

            Assert.Null(clip.LastText);
            Assert.Equal(0, store.Document.Snips[0].UsageCount);
        }

        [Fact]
        public async Task EditSnip_replaces_the_snip_in_the_store_when_interactions_returns_a_result()
        {
            Cli cli = null!;
            Snip snip = null!;
            var (vm, store, _, ix, _) = await BuildAsync(d => (cli, snip) = SeedOneCliOneSnip(d));

            ix.NextSnipEditResult = new SnipEditResult(new Snip
            {
                Id = snip.Id,
                CliId = snip.CliId,
                Title = "Renamed",
                CommandTemplate = "pl-app list --json",
            });
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);
            var card = ((CliViewModel)vm.CurrentContent!).Snips[0];

            await vm.EditSnipCommand.ExecuteAsync(card);

            Assert.Equal("Renamed", store.Document.Snips[0].Title);
            Assert.Equal("pl-app list --json", store.Document.Snips[0].CommandTemplate);
            Assert.Equal(1, store.SaveCount);
        }

        [Fact]
        public async Task DeleteSnip_marks_as_trash_only_when_confirmed()
        {
            Cli cli = null!;
            Snip snip = null!;
            var (vm, store, _, ix, _) = await BuildAsync(d => (cli, snip) = SeedOneCliOneSnip(d));

            ix.NextConfirmResult = false;
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);
            var card = ((CliViewModel)vm.CurrentContent!).Snips[0];

            await vm.DeleteSnipCommand.ExecuteAsync(card);

            Assert.False(store.Document.Snips[0].IsTrash);

            ix.NextConfirmResult = true;
            await vm.DeleteSnipCommand.ExecuteAsync(card);

            Assert.True(store.Document.Snips[0].IsTrash);
        }

        [Fact]
        public async Task ToggleFavourite_flips_the_flag_and_saves()
        {
            Cli cli = null!;
            Snip snip = null!;
            var (vm, store, _, _, _) = await BuildAsync(d => (cli, snip) = SeedOneCliOneSnip(d));

            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);
            var card = ((CliViewModel)vm.CurrentContent!).Snips[0];

            await vm.ToggleFavouriteCommand.ExecuteAsync(card);
            Assert.True(store.Document.Snips[0].IsFavourite);

            await vm.ToggleFavouriteCommand.ExecuteAsync(card);
            Assert.False(store.Document.Snips[0].IsFavourite);
        }

        [Fact]
        public async Task NewSnip_only_acts_when_a_CLI_is_selected()
        {
            Cli cli = null!;
            var (vm, store, _, ix, _) = await BuildAsync(d =>
            {
                cli = new Cli { Name = "pl-app" };
                d.Clis.Add(cli);
            });

            ix.NextSnipEditResult = new SnipEditResult(new Snip
            {
                CliId = cli.Id,
                Title = "New",
                CommandTemplate = "echo new",
            });

            // No CLI selected (Home) — should no-op
            await vm.NewSnipCommand.ExecuteAsync(null);
            Assert.Empty(store.Document.Snips);

            // Now select the CLI and try again
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);
            await vm.NewSnipCommand.ExecuteAsync(null);

            Assert.Single(store.Document.Snips);
            Assert.Equal("New", store.Document.Snips[0].Title);
        }

        [Fact]
        public async Task DeleteCurrentCli_is_blocked_when_the_cli_has_active_snips()
        {
            Cli cli = null!;
            var (vm, store, _, ix, _) = await BuildAsync(d => (cli, _) = SeedOneCliOneSnip(d));

            // Even with confirmation primed, the must-be-empty guard fires first.
            ix.NextConfirmResult = true;
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);

            await vm.DeleteCurrentCliCommand.ExecuteAsync(null);

            Assert.Single(store.Document.Clis);
            Assert.Equal(1, ix.NotifyCount);
            Assert.Equal(0, store.SaveCount);
        }

        [Fact]
        public async Task DeleteCurrentCli_does_nothing_when_not_confirmed()
        {
            Cli cli = null!;
            var (vm, store, _, ix, _) = await BuildAsync(d =>
            {
                cli = new Cli { Name = "empty-app" };
                d.Clis.Add(cli);
            });

            ix.NextConfirmResult = false;
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);

            await vm.DeleteCurrentCliCommand.ExecuteAsync(null);

            Assert.Single(store.Document.Clis);
            Assert.Equal(0, ix.NotifyCount);
            Assert.Equal(0, store.SaveCount);
        }

        [Fact]
        public async Task DeleteCurrentCli_removes_cli_and_its_trashed_snips_then_falls_back_to_home()
        {
            Cli cli = null!;
            var (vm, store, _, ix, _) = await BuildAsync(d =>
            {
                cli = new Cli { Name = "pl-app" };
                d.Clis.Add(cli);
                // A trashed snip must not block deletion, and must be removed with the CLI.
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "old", CommandTemplate = "x", IsTrash = true });
            });

            ix.NextConfirmResult = true;
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);

            await vm.DeleteCurrentCliCommand.ExecuteAsync(null);

            Assert.Empty(store.Document.Clis);
            Assert.Empty(store.Document.Snips);
            Assert.Equal(1, store.SaveCount);
            Assert.True(vm.SelectedCliChoice?.IsAll);
        }

        [Fact]
        public async Task DeleteCurrentCli_deletes_the_icon_asset()
        {
            var icons = new FakeIconAssetStorage();
            var cli = new Cli { Name = "pl-app", IconRef = "icons/abc.png" };
            var doc = new SnipStoreDocument();
            doc.Clis.Add(cli);
            var store = new InMemorySnipStore(doc);
            var ix = new FakeShellInteractions { NextConfirmResult = true };
            var vm = new ShellViewModel(store, new FakeClipboardService(), new FakeClock(DateTimeOffset.UtcNow), ix, icons, new FakeExternalLinkService());
            await vm.LoadAsync();
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);

            await vm.DeleteCurrentCliCommand.ExecuteAsync(null);

            Assert.Contains("icons/abc.png", icons.Deleted);
            Assert.Empty(store.Document.Clis);
        }

        [Fact]
        public async Task DeleteCurrentCli_no_ops_on_home()
        {
            var (vm, store, _, ix, _) = await BuildAsync(d => d.Clis.Add(new Cli { Name = "pl-app" }));

            // Selection defaults to the Home entry after load.
            ix.NextConfirmResult = true;
            await vm.DeleteCurrentCliCommand.ExecuteAsync(null);

            Assert.Single(store.Document.Clis);
            Assert.Equal(0, ix.NotifyCount);
            Assert.Equal(0, store.SaveCount);
        }

        [Fact]
        public async Task SaveTagIcons_persists_glyphs_and_refreshes_the_nav()
        {
            Cli cli = null!;
            var (vm, store, _, _, _) = await BuildAsync(d =>
            {
                cli = new Cli { Name = "pl-app" };
                d.Clis.Add(cli);
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "Deploy", CommandTemplate = "x", Tags = ["deploy"] });
            });

            // Select the CLI so its tags populate the nav (default tag glyph).
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);
            Assert.Equal(TagItemViewModel.DefaultGlyph, vm.Tags.Single(t => t.Name == "deploy").Glyph);

            vm.OpenTagIcons();
            var tagsVm = Assert.IsType<TagIconsViewModel>(vm.CurrentContent);
            tagsVm.Rows.Single(r => r.TagName == "deploy").Glyph = "X";

            await vm.SaveTagIconsCommand.ExecuteAsync(null);

            Assert.Equal("X", store.Document.TagIcons["deploy"]);
            // Nav glyph refreshed in place; the view stays on the Tags editor.
            Assert.Equal("X", vm.Tags.Single(t => t.Name == "deploy").Glyph);
            Assert.IsType<TagIconsViewModel>(vm.CurrentContent);
        }

        [Fact]
        public async Task SaveGlobalParameters_persists_edited_rows_to_the_document()
        {
            var (vm, store, _, _, _) = await BuildAsync();

            vm.OpenGlobalParameters();
            var globals = Assert.IsType<GlobalParametersViewModel>(vm.CurrentContent);
            globals.AddParameterCommand.Execute(null);
            globals.Parameters[0].Name = "tenant";
            globals.Parameters[0].Default = "acme";

            await vm.SaveGlobalParametersCommand.ExecuteAsync(null);

            var saved = Assert.Single(store.Document.GlobalParameters);
            Assert.Equal("tenant", saved.Name);
            Assert.Equal("acme", saved.Default);
            Assert.Equal("Saved.", globals.StatusMessage);
        }

        [Fact]
        public async Task OpenTrash_shows_only_trashed_snips()
        {
            Cli cli = null!;
            var (vm, _, _, _, _) = await BuildAsync(d =>
            {
                cli = new Cli { Name = "pl-app" };
                d.Clis.Add(cli);
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "Active", CommandTemplate = "a" });
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "Binned", CommandTemplate = "b", IsTrash = true });
            });

            vm.OpenTrash();

            var trash = Assert.IsType<TrashViewModel>(vm.CurrentContent);
            Assert.Single(trash.Snips);
            Assert.Equal("Binned", trash.Snips[0].Title);
        }

        [Fact]
        public async Task RestoreSnip_clears_trash_flag_saves_and_drops_it_from_the_trash_view()
        {
            Cli cli = null!;
            var (vm, store, _, _, _) = await BuildAsync(d =>
            {
                cli = new Cli { Name = "pl-app" };
                d.Clis.Add(cli);
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "Binned", CommandTemplate = "b", IsTrash = true });
            });

            vm.OpenTrash();
            var card = ((TrashViewModel)vm.CurrentContent!).Snips[0];

            await vm.RestoreSnipCommand.ExecuteAsync(card);

            Assert.False(store.Document.Snips[0].IsTrash);
            Assert.Equal(1, store.SaveCount);
            var trash = Assert.IsType<TrashViewModel>(vm.CurrentContent);
            Assert.Empty(trash.Snips);
        }

        [Fact]
        public async Task RestoreSnip_refreshes_the_pane_tags_for_the_selected_cli()
        {
            Cli cli = null!;
            var (vm, _, _, _, _) = await BuildAsync(d =>
            {
                cli = new Cli { Name = "pl-app" };
                d.Clis.Add(cli);
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "Active", CommandTemplate = "a" });
                // Trashed, and carrying a tag no visible snip has.
                d.Snips.Add(new Snip
                {
                    CliId = cli.Id,
                    Title = "Binned",
                    CommandTemplate = "b",
                    Tags = ["incident"],
                    IsTrash = true,
                });
            });

            // Select the CLI: its pane tags should not yet include the trashed snip's tag.
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);
            Assert.DoesNotContain("incident", vm.Tags.Select(t => t.Name));

            // Restore from Trash while that CLI is still the selected one.
            vm.OpenTrash();
            var card = ((TrashViewModel)vm.CurrentContent!).Snips[0];
            await vm.RestoreSnipCommand.ExecuteAsync(card);

            // The pane tag list must now reflect the restored snip, and we stay on Trash.
            Assert.Contains("incident", vm.Tags.Select(t => t.Name));
            Assert.IsType<TrashViewModel>(vm.CurrentContent);
        }

        [Fact]
        public async Task DeleteForever_only_removes_the_snip_when_confirmed()
        {
            Cli cli = null!;
            var (vm, store, _, ix, _) = await BuildAsync(d =>
            {
                cli = new Cli { Name = "pl-app" };
                d.Clis.Add(cli);
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "Binned", CommandTemplate = "b", IsTrash = true });
            });

            vm.OpenTrash();
            var card = ((TrashViewModel)vm.CurrentContent!).Snips[0];

            ix.NextConfirmResult = false;
            await vm.DeleteForeverCommand.ExecuteAsync(card);
            Assert.Single(store.Document.Snips);
            Assert.Equal(0, store.SaveCount);

            ix.NextConfirmResult = true;
            await vm.DeleteForeverCommand.ExecuteAsync(card);
            Assert.Empty(store.Document.Snips);
            Assert.Equal(1, store.SaveCount);
            var trash = Assert.IsType<TrashViewModel>(vm.CurrentContent);
            Assert.Empty(trash.Snips);
        }

        [Fact]
        public async Task NewCli_adds_the_cli_and_writes_icon_bytes_when_provided()
        {
            var (vm, store, _, ix, _) = await BuildAsync();
            var icons = new FakeIconAssetStorage();
            var newCli = new Cli { Name = "inv-app" };
            ix.NextCliEditResult = new CliEditResult(newCli, [0x89, 0x50, 0x4E, 0x47]);

            // Replace the icon storage so we can inspect — rebuild the VM
            var clip = new FakeClipboardService();
            var clock = new FakeClock(DateTimeOffset.UtcNow);
            var vmWithIcons = new ShellViewModel(store, clip, clock, ix, icons, new FakeExternalLinkService());
            await vmWithIcons.LoadAsync();

            await vmWithIcons.NewCliCommand.ExecuteAsync(null);

            Assert.Single(store.Document.Clis);
            Assert.Equal("inv-app", store.Document.Clis[0].Name);
            Assert.True(icons.Saved.ContainsKey(newCli.Id));
            Assert.StartsWith("icons/", store.Document.Clis[0].IconRef);
        }
    }
}
