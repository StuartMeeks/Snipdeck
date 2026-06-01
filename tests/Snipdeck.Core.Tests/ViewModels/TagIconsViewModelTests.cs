using Snipdeck.Core.Models;
using Snipdeck.Core.Tests.Support;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class TagIconsViewModelTests
    {
        // "X" is a stand-in glyph — any non-blank, non-default string exercises the logic.

        // A literal Segoe Fluent Icons glyph character, built from its code point so
        // the source stays ASCII (mirrors the real "Tag" glyph).
        private static string GlyphChar(int codePoint) => char.ConvertFromUtf32(codePoint);

        private static GlyphNameLookup CatalogueWith(params (int Code, string Name)[] entries) =>
            new(entries.Select(e => new GlyphCatalogueEntry(GlyphChar(e.Code), e.Name, [])));

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
        public void GlyphText_shows_the_catalogue_name_for_a_known_glyph()
        {
            // A picked/known glyph is shown by its friendly name, not a tofu box or code.
            var names = CatalogueWith((0xE8EC, "Tag"), (0xE713, "Settings"));
            var row = new TagIconRowViewModel("ops", GlyphChar(0xE713), names: names);

            Assert.Equal("Settings", row.GlyphText);
        }

        [Fact]
        public void GlyphText_setter_resolves_a_known_name_to_its_glyph()
        {
            var names = CatalogueWith((0xE713, "Settings"));
            var row = new TagIconRowViewModel("ops", string.Empty, names: names)
            {
                GlyphText = "settings", // case-insensitive
            };

            Assert.Equal(GlyphChar(0xE713), row.Glyph);
            Assert.Equal("Settings", row.GlyphText); // round-trips back to the name
        }

        [Fact]
        public void GlyphText_falls_back_to_hex_when_the_glyph_is_not_in_the_catalogue()
        {
            // No lookup -> a literal glyph character is shown as its code point
            // rather than an unreadable tofu box.
            var row = new TagIconRowViewModel("ops", GlyphChar(0xE8EC));

            Assert.Equal("E8EC", row.GlyphText);
        }

        [Fact]
        public void GlyphText_passes_typed_codes_and_text_through_unchanged()
        {
            var row = new TagIconRowViewModel("ops", "E8EC");
            Assert.Equal("E8EC", row.GlyphText); // typed code stays as typed

            row.Glyph = "U+E8EC";
            Assert.Equal("U+E8EC", row.GlyphText);

            row.Glyph = string.Empty;
            Assert.Equal(string.Empty, row.GlyphText);
        }

        [Fact]
        public void GlyphText_setter_keeps_a_raw_code_when_it_is_not_a_known_name()
        {
            var row = new TagIconRowViewModel("ops", string.Empty)
            {
                GlyphText = "E713",
            };

            Assert.Equal("E713", row.Glyph);
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
