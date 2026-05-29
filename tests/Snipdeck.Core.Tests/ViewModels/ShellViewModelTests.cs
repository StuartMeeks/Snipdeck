using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Core.Tests.Support;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class ShellViewModelTests
    {
        private sealed class InMemorySnipStore(SnipStoreDocument document) : ISnipStore
        {
            private SnipStoreDocument _document = document;

            public string FilePath => "in-memory";

            public Task<SnipStoreDocument> LoadAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(_document);

            public Task SaveAsync(SnipStoreDocument document, CancellationToken cancellationToken = default)
            {
                _document = document;
                return Task.CompletedTask;
            }
        }

        private static SettingsViewModel NewSettingsViewModel()
        {
            return new SettingsViewModel(
                new FakeSettingsStore(),
                new FakeThemeApplier(),
                new FakeUpdateService(),
                new FakePathProvider(),
                new AppConfig());
        }

        private static ShellViewModel NewShellViewModel(
            ISnipStore store,
            FakeClipboardService? clipboard = null,
            FakeShellInteractions? interactions = null,
            FakeIconAssetStorage? icons = null,
            FakeClock? clock = null)
        {
            return new ShellViewModel(
                store,
                clipboard ?? new FakeClipboardService(),
                clock ?? new FakeClock(DateTimeOffset.UtcNow),
                interactions ?? new FakeShellInteractions(),
                icons ?? new FakeIconAssetStorage());
        }

        private static SnipStoreDocument SampleDocument(out Guid plAppId, out Guid mptAppId)
        {
            plAppId = Guid.NewGuid();
            mptAppId = Guid.NewGuid();
            var captured1 = plAppId;
            var captured2 = mptAppId;
            return new SnipStoreDocument
            {
                Clis =
                {
                    new Cli { Id = captured1, Name = "pl-app" },
                    new Cli { Id = captured2, Name = "mpt-app" },
                },
                Snips =
                {
                    new Snip { Id = Guid.NewGuid(), CliId = captured1, Title = "List orgs", Tags = ["read", "orgs"] },
                    new Snip { Id = Guid.NewGuid(), CliId = captured1, Title = "Deploy", Tags = ["deploy"] },
                    new Snip { Id = Guid.NewGuid(), CliId = captured2, Title = "Users", Tags = ["users"] },
                },
            };
        }

        [Fact]
        public async Task After_LoadAsync_home_choice_is_selected_and_content_is_a_HomeViewModel()
        {
            var doc = SampleDocument(out _, out _);
            var vm = NewShellViewModel(new InMemorySnipStore(doc));

            await vm.LoadAsync();

            Assert.NotNull(vm.SelectedCliChoice);
            Assert.True(vm.SelectedCliChoice!.IsHome);
            _ = Assert.IsType<HomeViewModel>(vm.CurrentContent);
        }

        [Fact]
        public async Task CliChoices_contains_home_followed_by_cli_choices_in_alphabetical_order()
        {
            var doc = SampleDocument(out _, out _);
            var vm = NewShellViewModel(new InMemorySnipStore(doc));

            await vm.LoadAsync();

            Assert.Equal(3, vm.CliChoices.Count);
            Assert.True(vm.CliChoices[0].IsHome);
            Assert.Equal("mpt-app", vm.CliChoices[1].Display);
            Assert.Equal("pl-app", vm.CliChoices[2].Display);
        }

        [Fact]
        public async Task Selecting_a_cli_swaps_content_to_a_CliViewModel_and_rebuilds_tags()
        {
            var doc = SampleDocument(out var plAppId, out _);
            var vm = NewShellViewModel(new InMemorySnipStore(doc));
            await vm.LoadAsync();

            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == plAppId);

            var cliVm = Assert.IsType<CliViewModel>(vm.CurrentContent);
            Assert.Equal("pl-app", cliVm.Name);
            Assert.Contains(ShellViewModel.AllTagsSentinel, vm.Tags);
            Assert.Contains("read", vm.Tags);
            Assert.Contains("deploy", vm.Tags);
            Assert.Contains("orgs", vm.Tags);
            Assert.Equal(ShellViewModel.AllTagsSentinel, vm.SelectedTag);
        }

        [Fact]
        public async Task Selecting_home_clears_tags_and_resets_to_home_content()
        {
            var doc = SampleDocument(out var plAppId, out _);
            var vm = NewShellViewModel(new InMemorySnipStore(doc));
            await vm.LoadAsync();

            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == plAppId);
            vm.GoHome();

            Assert.Empty(vm.Tags);
            _ = Assert.IsType<HomeViewModel>(vm.CurrentContent);
        }

        [Fact]
        public async Task Changing_search_text_rebuilds_content()
        {
            var doc = SampleDocument(out _, out _);
            var vm = NewShellViewModel(new InMemorySnipStore(doc));
            await vm.LoadAsync();

            var first = vm.CurrentContent;
            vm.SearchText = "deploy";

            Assert.NotSame(first, vm.CurrentContent);
            _ = Assert.IsType<HomeViewModel>(vm.CurrentContent);
        }

        [Fact]
        public async Task OpenSettings_swaps_content_for_a_SettingsViewModel()
        {
            var doc = SampleDocument(out _, out _);
            var vm = NewShellViewModel(new InMemorySnipStore(doc));
            await vm.LoadAsync();

            vm.OpenSettings(NewSettingsViewModel());

            _ = Assert.IsType<SettingsViewModel>(vm.CurrentContent);
        }

        [Fact]
        public async Task Changing_cli_after_OpenSettings_returns_to_shell_content()
        {
            var doc = SampleDocument(out var plAppId, out _);
            var vm = NewShellViewModel(new InMemorySnipStore(doc));
            await vm.LoadAsync();

            vm.OpenSettings(NewSettingsViewModel());
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == plAppId);

            _ = Assert.IsType<CliViewModel>(vm.CurrentContent);
        }
    }
}
