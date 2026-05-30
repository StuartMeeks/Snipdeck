using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Core.Tests.Support;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class ShellViewModelNavTests
    {
        private sealed class InMemorySnipStore(SnipStoreDocument document) : ISnipStore
        {
            public string FilePath => "in-memory";
            public Task<SnipStoreDocument> LoadAsync(CancellationToken ct = default) => Task.FromResult(document);
            public Task SaveAsync(SnipStoreDocument d, CancellationToken ct = default) => Task.CompletedTask;
        }

        private static async Task<(ShellViewModel vm, FakeExternalLinkService links, Guid plId, Guid mptId)> BuildAsync()
        {
            var pl = new Cli { Name = "pl-app" };
            var mpt = new Cli { Name = "mpt-app" };
            var doc = new SnipStoreDocument
            {
                Clis = [pl, mpt],
                Snips =
                [
                    new Snip { CliId = pl.Id, Title = "Deploy", CommandTemplate = "pl deploy", Tags = ["ops"] },
                    new Snip { CliId = pl.Id, Title = "List pods", CommandTemplate = "pl pods" },
                    new Snip { CliId = mpt.Id, Title = "Deploy", CommandTemplate = "mpt deploy" },
                ],
            };
            var links = new FakeExternalLinkService();
            var vm = new ShellViewModel(
                new InMemorySnipStore(doc),
                new FakeClipboardService(),
                new FakeClock(DateTimeOffset.UtcNow),
                new FakeShellInteractions(),
                new FakeIconAssetStorage(),
                links);
            await vm.LoadAsync();
            return (vm, links, pl.Id, mpt.Id);
        }

        [Fact]
        public async Task Initial_state_is_home_with_all_scope()
        {
            var (vm, _, _, _) = await BuildAsync();

            Assert.True(vm.SelectedCliChoice!.IsAll);
            Assert.Null(vm.SelectedTagItem);
            _ = Assert.IsType<HomeViewModel>(vm.CurrentContent);
        }

        [Fact]
        public async Task Selecting_a_cli_shows_its_snips_with_the_All_tag_selected()
        {
            var (vm, _, plId, _) = await BuildAsync();

            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == plId);

            var content = Assert.IsType<CliViewModel>(vm.CurrentContent);
            Assert.Equal("pl-app", content.Name);
            Assert.True(content.HasCli);
            Assert.Equal(2, content.Snips.Count);
            Assert.True(vm.SelectedTagItem!.IsAll);
        }

        [Fact]
        public async Task ShowHome_returns_to_the_launcher()
        {
            var (vm, _, plId, _) = await BuildAsync();
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == plId);

            vm.ShowHome();

            _ = Assert.IsType<HomeViewModel>(vm.CurrentContent);
            Assert.Null(vm.SelectedTagItem);
        }

        [Fact]
        public async Task Search_suggestions_are_scoped_and_carry_the_cli_name()
        {
            var (vm, _, plId, _) = await BuildAsync();

            // All scope: both "Deploy" snips match, distinguished by CLI name.
            var all = vm.GetSearchSuggestions("deploy");
            Assert.Equal(2, all.Count);
            Assert.Contains(all, r => r.CliName == "pl-app");
            Assert.Contains(all, r => r.CliName == "mpt-app");

            // Scoped to a CLI: only that CLI's match.
            vm.SelectedCliChoice = vm.CliChoices.Single(c => c.Cli?.Id == plId);
            var scoped = vm.GetSearchSuggestions("deploy");
            Assert.Equal("pl-app", Assert.Single(scoped).CliName);
        }

        [Fact]
        public async Task Selecting_a_search_result_switches_scope_and_filters_to_it()
        {
            var (vm, _, _, mptId) = await BuildAsync();

            var result = vm.GetSearchSuggestions("deploy").Single(r => r.CliId == mptId);
            vm.SelectSearchResult(result);

            Assert.Equal(mptId, vm.SelectedCliChoice!.Cli!.Id);
            Assert.Equal("Deploy", vm.SearchText);
            var content = Assert.IsType<CliViewModel>(vm.CurrentContent);
            Assert.Equal("Deploy", Assert.Single(content.Snips).Title);
        }

        [Fact]
        public async Task ApplySearch_from_home_moves_to_the_filtered_snip_list()
        {
            var (vm, _, _, _) = await BuildAsync();
            Assert.Null(vm.SelectedTagItem); // on Home

            vm.ApplySearch("deploy");

            Assert.True(vm.SelectedTagItem!.IsAll); // moved onto the snip list
            Assert.Equal("deploy", vm.SearchText);
            var content = Assert.IsType<CliViewModel>(vm.CurrentContent);
            Assert.Equal(2, content.Snips.Count); // both "Deploy" snips across CLIs
        }

        [Fact]
        public async Task OpenDocumentation_opens_the_readme_url()
        {
            var (vm, links, _, _) = await BuildAsync();

            await vm.OpenDocumentationAsync();

            Assert.Equal(1, links.OpenCount);
            Assert.Equal(ShellViewModel.DocumentationUrl, links.LastOpenedUrl);
        }
    }
}
