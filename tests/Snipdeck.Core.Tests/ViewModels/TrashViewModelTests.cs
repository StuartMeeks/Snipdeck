using Snipdeck.Core.Models;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class TrashViewModelTests
    {
        private static Snip Trashed(string title)
        {
            return new Snip { CliId = Guid.NewGuid(), Title = title, CommandTemplate = "x", IsTrash = true };
        }

        [Fact]
        public void Constructor_orders_snips_by_title_case_insensitively()
        {
            var vm = new TrashViewModel([Trashed("beta"), Trashed("Alpha"), Trashed("gamma")]);

            Assert.Equal(["Alpha", "beta", "gamma"], vm.Snips.Select(s => s.Title));
        }

        [Fact]
        public void Empty_trash_reports_the_empty_state()
        {
            var vm = new TrashViewModel([]);

            Assert.True(vm.IsEmpty);
            Assert.False(vm.HasSnips);
        }

        [Fact]
        public void Load_repopulates_the_same_collection_instance()
        {
            // The shell relies on this: the bound list only updates if the collection
            // instance survives, because the content area resolves its bindings once.
            var vm = new TrashViewModel([Trashed("first")]);
            var collection = vm.Snips;

            vm.Load([Trashed("second"), Trashed("third")]);

            Assert.Same(collection, vm.Snips);
            Assert.Equal(["second", "third"], vm.Snips.Select(s => s.Title));
        }

        [Fact]
        public void Load_raises_change_notifications_for_the_computed_state()
        {
            var vm = new TrashViewModel([Trashed("only")]);
            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

            vm.Load([]);

            Assert.True(vm.IsEmpty);
            Assert.False(vm.HasSnips);
            Assert.Contains(nameof(TrashViewModel.IsEmpty), changed);
            Assert.Contains(nameof(TrashViewModel.HasSnips), changed);
        }

        [Fact]
        public void Load_rejects_a_null_sequence()
        {
            var vm = new TrashViewModel([]);

            _ = Assert.Throws<ArgumentNullException>(() => vm.Load(null!));
        }
    }
}
