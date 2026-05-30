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
            Assert.Equal(string.Empty, vm.Rows[1].Glyph); // no stored glyph -> blank (defaults to #)
        }

        [Fact]
        public void PreviewGlyph_falls_back_to_default_when_blank_else_shows_the_glyph()
        {
            var row = new TagIconRowViewModel("ops", string.Empty);
            Assert.Equal("#", row.PreviewGlyph);

            row.Glyph = "X";
            Assert.Equal("X", row.PreviewGlyph);
        }

        [Fact]
        public void BuildTagIcons_keeps_only_non_default_glyphs()
        {
            var vm = new TagIconsViewModel(["a", "b", "c"], new Dictionary<string, string>());
            vm.Rows[0].Glyph = "X";  // custom -> kept
            vm.Rows[1].Glyph = "#";  // explicit default -> dropped
            vm.Rows[2].Glyph = "  "; // blank -> dropped

            var map = vm.BuildTagIcons();

            Assert.Equal(["a"], map.Keys);
            Assert.Equal("X", map["a"]);
        }
    }
}
