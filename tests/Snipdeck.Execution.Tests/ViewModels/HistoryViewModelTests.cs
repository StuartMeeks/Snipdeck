using Snipdeck.Execution.Models;
using Snipdeck.Execution.Tests.Support;
using Snipdeck.Execution.ViewModels;

namespace Snipdeck.Execution.Tests.ViewModels
{
    public class HistoryViewModelTests
    {
        private static readonly Guid _snipId = Guid.NewGuid();
        private static readonly Guid _cliId = Guid.NewGuid();

        private static HistoryViewModel Build(FakeCommandHistoryStore store, FakeShellInteractions interactions)
        {
            return new HistoryViewModel(
                store,
                interactions,
                id => id == _snipId ? "My Snip" : "(deleted snip)",
                id => id == _cliId ? "pl-app" : "(deleted CLI)");
        }

        private static CommandHistoryEntry Entry(string command = "pl-app deploy", string output = "done")
            => new()
            {
                SnipId = _snipId,
                CliId = _cliId,
                ResolvedCommand = command,
                CleanedOutput = output,
                ExitCode = 0,
            };

        [Fact]
        public async Task LoadAsync_projects_entries_and_resolves_names()
        {
            var store = new FakeCommandHistoryStore();
            store.Entries.Add(Entry());
            var vm = Build(store, new FakeShellInteractions());

            await vm.LoadAsync();

            var item = Assert.Single(vm.Items);
            Assert.Equal("My Snip", item.SnipTitle);
            Assert.Equal("pl-app", item.CliName);
            Assert.Equal("pl-app deploy", item.ResolvedCommand);
            Assert.False(vm.IsEmpty);
        }

        [Fact]
        public async Task IsEmpty_is_true_after_loading_with_no_runs()
        {
            var vm = Build(new FakeCommandHistoryStore(), new FakeShellInteractions());

            await vm.LoadAsync();

            Assert.True(vm.IsEmpty);
            Assert.Empty(vm.Items);
        }

        [Fact]
        public async Task Open_raises_OpenRequested_with_the_run_id()
        {
            var store = new FakeCommandHistoryStore();
            store.Entries.Add(Entry());
            var vm = Build(store, new FakeShellInteractions());
            await vm.LoadAsync();

            Guid? requested = null;
            vm.OpenRequested += (_, id) => requested = id;
            vm.OpenCommand.Execute(vm.Items[0]);

            Assert.Equal(vm.Items[0].Id, requested);
        }

        [Fact]
        public async Task Delete_removes_the_run_when_confirmed()
        {
            var store = new FakeCommandHistoryStore();
            store.Entries.Add(Entry());
            var interactions = new FakeShellInteractions { NextConfirmResult = true };
            var vm = Build(store, interactions);
            await vm.LoadAsync();

            await vm.DeleteCommand.ExecuteAsync(vm.Items[0]);

            Assert.Empty(store.Entries);
            Assert.True(vm.IsEmpty);
        }

        [Fact]
        public async Task Delete_keeps_the_run_when_cancelled()
        {
            var store = new FakeCommandHistoryStore();
            store.Entries.Add(Entry());
            var interactions = new FakeShellInteractions { NextConfirmResult = false };
            var vm = Build(store, interactions);
            await vm.LoadAsync();

            await vm.DeleteCommand.ExecuteAsync(vm.Items[0]);

            Assert.Single(store.Entries);
        }

        [Fact]
        public async Task ClearAll_empties_the_store_when_confirmed()
        {
            var store = new FakeCommandHistoryStore();
            store.Entries.Add(Entry("a"));
            store.Entries.Add(Entry("b"));
            var interactions = new FakeShellInteractions { NextConfirmResult = true };
            var vm = Build(store, interactions);
            await vm.LoadAsync();

            await vm.ClearAllCommand.ExecuteAsync(null);

            Assert.Empty(store.Entries);
        }

        [Fact]
        public async Task LoadAsync_passes_search_text_to_the_store()
        {
            var store = new FakeCommandHistoryStore();
            var vm = Build(store, new FakeShellInteractions());
            vm.SearchText = "kubectl";

            await vm.LoadAsync();

            Assert.Equal("kubectl", store.LastSearch);
        }
    }
}
