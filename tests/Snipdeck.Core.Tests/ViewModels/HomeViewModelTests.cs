using Snipdeck.Core.Models;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class HomeViewModelTests
    {
        private static SnipStoreDocument Document(Action<SnipStoreDocument> configure)
        {
            var doc = new SnipStoreDocument();
            configure(doc);
            return doc;
        }

        [Fact]
        public void CliCards_are_sorted_alphabetically_and_carry_their_snip_counts()
        {
            var pl = new Cli { Name = "pl-app" };
            var inv = new Cli { Name = "inv-app" };
            var doc = Document(d =>
            {
                d.Clis.Add(pl);
                d.Clis.Add(inv);
                d.Snips.Add(new Snip { CliId = pl.Id });
                d.Snips.Add(new Snip { CliId = pl.Id });
                d.Snips.Add(new Snip { CliId = inv.Id });
            });

            var vm = new HomeViewModel(doc, searchText: null);

            Assert.Collection(vm.CliCards,
                c => { Assert.Equal("inv-app", c.Name); Assert.Equal(1, c.SnipCount); },
                c => { Assert.Equal("pl-app", c.Name); Assert.Equal(2, c.SnipCount); });
        }

        [Fact]
        public void Search_text_filters_cli_cards_by_name()
        {
            var doc = Document(d =>
            {
                d.Clis.Add(new Cli { Name = "pl-app" });
                d.Clis.Add(new Cli { Name = "mpt-app" });
            });

            var vm = new HomeViewModel(doc, searchText: "pl");

            var single = Assert.Single(vm.CliCards);
            Assert.Equal("pl-app", single.Name);
        }

        [Fact]
        public void MostUsedSnips_sorts_by_usage_count_then_last_used_desc()
        {
            var cli = new Cli { Name = "a" };
            var now = DateTimeOffset.UtcNow;
            var doc = Document(d =>
            {
                d.Clis.Add(cli);
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "x", UsageCount = 5, LastUsedAt = now.AddMinutes(-10) });
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "y", UsageCount = 10, LastUsedAt = now.AddMinutes(-1) });
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "z", UsageCount = 5, LastUsedAt = now });
            });

            var vm = new HomeViewModel(doc, searchText: null);

            Assert.Collection(vm.MostUsedSnips,
                s => Assert.Equal("y", s.Title),
                s => Assert.Equal("z", s.Title),
                s => Assert.Equal("x", s.Title));
        }

        [Fact]
        public void MostUsedSnips_skips_unused_and_trashed_entries()
        {
            var cli = new Cli { Name = "a" };
            var doc = Document(d =>
            {
                d.Clis.Add(cli);
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "never used", UsageCount = 0 });
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "trashed", UsageCount = 99, IsTrash = true });
                d.Snips.Add(new Snip { CliId = cli.Id, Title = "kept", UsageCount = 3 });
            });

            var vm = new HomeViewModel(doc, searchText: null);

            var single = Assert.Single(vm.MostUsedSnips);
            Assert.Equal("kept", single.Title);
        }

        [Fact]
        public void Empty_document_produces_empty_collections_with_false_predicates()
        {
            var vm = new HomeViewModel(new SnipStoreDocument(), searchText: null);

            Assert.Empty(vm.CliCards);
            Assert.Empty(vm.MostUsedSnips);
            Assert.False(vm.HasCliCards);
            Assert.False(vm.HasMostUsedSnips);
        }
    }
}
