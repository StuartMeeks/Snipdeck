using Snipdeck.Core.Tests.Support;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class TagIconsViewModelTests
    {
        // "X" is a stand-in glyph — any non-blank, non-default string exercises the logic.

        [Fact]
        public void Rows_are_distinct_sorted_and_prefilled_with_stored_glyphs()
        {
            var vm = new TagIconsViewModel(
                ["deploy", "ops", "deploy"],
                new Dictionary<string, string> { ["deploy"] = "X" });

            Assert.Equal(["deploy", "ops"], vm.Rows.Select(r => r.TagName));
            Assert.Equal("X", vm.Rows[0].Glyph); // prefilled from the stored map
            Assert.Equal(string.Empty, vm.Rows[1].Glyph); // no stored glyph -> blank (uses the default tag icon)
        }

        [Fact]
        public void PreviewGlyph_falls_back_to_default_when_blank_else_shows_the_glyph()
        {
            var row = new TagIconRowViewModel("ops", string.Empty);
            Assert.Equal(TagItemViewModel.DefaultGlyph, row.PreviewGlyph);

            row.Glyph = "X";
            Assert.Equal("X", row.PreviewGlyph);
        }

        [Fact]
        public void BuildTagIcons_keeps_only_non_default_glyphs()
        {
            var vm = new TagIconsViewModel(["a", "b", "c"], new Dictionary<string, string>());
            vm.Rows[0].Glyph = "X";  // custom -> kept
            vm.Rows[1].Glyph = TagItemViewModel.DefaultGlyph; // explicit default -> dropped
            vm.Rows[2].Glyph = "  "; // blank -> dropped

            var map = vm.BuildTagIcons();

            Assert.Equal(["a"], map.Keys);
            Assert.Equal("X", map["a"]);
        }

        [Fact]
        public async Task ChooseGlyph_stores_the_picked_glyph()
        {
            var interactions = new FakeShellInteractions { NextPickGlyphResult = "Y" };
            var row = new TagIconRowViewModel("ops", "X", interactions);

            await row.ChooseGlyphCommand.ExecuteAsync(null);

            Assert.Equal("Y", row.Glyph);
            Assert.Equal("X", interactions.LastPickGlyphCurrent); // passes the current glyph for preselection
        }

        [Fact]
        public async Task ChooseGlyph_leaves_the_glyph_unchanged_when_cancelled()
        {
            var interactions = new FakeShellInteractions { NextPickGlyphResult = null };
            var row = new TagIconRowViewModel("ops", "X", interactions);

            await row.ChooseGlyphCommand.ExecuteAsync(null);

            Assert.Equal("X", row.Glyph);
        }

        [Fact]
        public async Task ChooseGlyph_is_a_no_op_without_interactions()
        {
            var row = new TagIconRowViewModel("ops", "X");

            await row.ChooseGlyphCommand.ExecuteAsync(null);

            Assert.Equal("X", row.Glyph);
        }
    }
}
