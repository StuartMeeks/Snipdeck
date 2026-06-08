using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Core.Tests.Support;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class ShellViewModelRunTests
    {
        private sealed class InMemorySnipStore(SnipStoreDocument document) : ISnipStore
        {
            public SnipStoreDocument Document { get; } = document;

            public int SaveCount { get; private set; }

            public string FilePath => "in-memory";

            public Task<SnipStoreDocument> LoadAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(Document);

            public Task SaveAsync(SnipStoreDocument document, CancellationToken cancellationToken = default)
            {
                SaveCount++;
                return Task.CompletedTask;
            }
        }

        private static async Task<(ShellViewModel vm, FakeRunCoordinator runner, InMemorySnipStore store, FakeShellInteractions ix, Cli cli, Snip snip)> BuildAsync()
        {
            var doc = new SnipStoreDocument();
            var cli = new Cli { Name = "pl-app" };
            var snip = new Snip { CliId = cli.Id, Title = "List", CommandTemplate = "pl-app list" };
            doc.Clis.Add(cli);
            doc.Snips.Add(snip);

            var store = new InMemorySnipStore(doc);
            var ix = new FakeShellInteractions();
            var runner = new FakeRunCoordinator();
            var vm = new ShellViewModel(
                store,
                new FakeClipboardService(),
                new FakeClock(new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero)),
                ix,
                new FakeIconAssetStorage(),
                new FakeExternalLinkService(),
                glyphCatalogue: null,
                runCoordinator: runner);
            await vm.LoadAsync();
            return (vm, runner, store, ix, cli, snip);
        }

        private static SnipCardViewModel CardFor(ShellViewModel vm, Cli cli)
        {
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == cli.Id);
            return ((CliViewModel)vm.CurrentContent!).Snips[0];
        }

        [Fact]
        public async Task RunSnip_sets_current_content_to_the_run_view_and_bumps_usage()
        {
            var (vm, runner, store, _, cli, snip) = await BuildAsync();
            var runView = new object();
            runner.NextResult = runView;
            var card = CardFor(vm, cli);
            var savesBefore = store.SaveCount;

            await vm.RunSnipCommand.ExecuteAsync(card);

            Assert.Same(runView, vm.CurrentContent);
            Assert.Equal(snip.Id, runner.LastSnip!.Id);
            Assert.Same(cli, runner.LastCli);
            Assert.Equal(1, store.Document.Snips[0].UsageCount);
            Assert.Equal(savesBefore + 1, store.SaveCount);
        }

        [Fact]
        public async Task RunSnip_leaves_content_unchanged_when_the_coordinator_returns_null()
        {
            var (vm, runner, _, _, cli, _) = await BuildAsync();
            runner.NextResult = null;
            var card = CardFor(vm, cli);
            var contentBefore = vm.CurrentContent;

            await vm.RunSnipCommand.ExecuteAsync(card);

            Assert.Same(contentBefore, vm.CurrentContent);
            Assert.Equal(0, ((CliViewModel)vm.CurrentContent!).Snips[0].Snip.UsageCount);
        }

        [Fact]
        public async Task RunSnipById_notifies_when_the_snip_is_gone()
        {
            var (vm, runner, _, ix, _, _) = await BuildAsync();

            await vm.RunSnipByIdAsync(Guid.NewGuid());

            Assert.Equal(0, runner.CallCount);
            Assert.Equal(1, ix.NotifyCount);
        }

        [Fact]
        public async Task RunSnipById_runs_an_existing_snip()
        {
            var (vm, runner, _, _, _, snip) = await BuildAsync();
            runner.NextResult = new object();

            await vm.RunSnipByIdAsync(snip.Id);

            Assert.Equal(1, runner.CallCount);
            Assert.Equal(snip.Id, runner.LastSnip!.Id);
        }

        [Fact]
        public async Task Resolvers_return_names_and_placeholders()
        {
            var (vm, _, _, _, cli, snip) = await BuildAsync();

            Assert.Equal("List", vm.ResolveSnipTitle(snip.Id));
            Assert.Equal("pl-app", vm.ResolveCliName(cli.Id));
            Assert.Equal("(deleted snip)", vm.ResolveSnipTitle(Guid.NewGuid()));
            Assert.Equal("(deleted CLI)", vm.ResolveCliName(Guid.NewGuid()));
        }
    }
}
