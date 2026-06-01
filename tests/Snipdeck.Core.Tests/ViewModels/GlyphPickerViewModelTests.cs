using Snipdeck.Core.Models;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class GlyphPickerViewModelTests
    {
        // Glyphs built from their code points so the source stays ASCII; these
        // mirror the real Segoe Fluent Icons Home/Settings/Folder glyphs.
        private static string Glyph(int codePoint) => char.ConvertFromUtf32(codePoint);

        private static IReadOnlyList<GlyphCatalogueEntry> SampleCatalogue() =>
        [
            new(Glyph(0xE80F), "Home", ["house", "dashboard"]),
            new(Glyph(0xE713), "Settings", ["gear", "config"]),
            new(Glyph(0xE8B7), "Folder", ["directory", "files"]),
        ];

        [Fact]
        public void Entries_are_sorted_by_name_and_shown_in_full_initially()
        {
            var vm = new GlyphPickerViewModel(SampleCatalogue());

            Assert.Equal(["Folder", "Home", "Settings"], vm.Results.Select(e => e.Name));
        }

        [Fact]
        public void Search_matches_name_case_insensitively()
        {
            var vm = new GlyphPickerViewModel(SampleCatalogue())
            {
                SearchText = "fold",
            };

            Assert.Equal(["Folder"], vm.Results.Select(e => e.Name));
            Assert.False(vm.HasNoResults);
        }

        [Fact]
        public void Search_matches_keywords()
        {
            var vm = new GlyphPickerViewModel(SampleCatalogue())
            {
                SearchText = "gear",
            };

            Assert.Equal(["Settings"], vm.Results.Select(e => e.Name));
        }

        [Fact]
        public void Search_matches_code_point()
        {
            var vm = new GlyphPickerViewModel(SampleCatalogue())
            {
                SearchText = "e80f",
            };

            Assert.Equal(["Home"], vm.Results.Select(e => e.Name));
        }

        [Fact]
        public void Search_with_no_match_clears_results_and_flags_no_results()
        {
            var vm = new GlyphPickerViewModel(SampleCatalogue())
            {
                SearchText = "nonsense",
            };

            Assert.Empty(vm.Results);
            Assert.True(vm.HasNoResults);
        }

        [Fact]
        public void Current_glyph_preselects_the_matching_entry()
        {
            var vm = new GlyphPickerViewModel(SampleCatalogue(), Glyph(0xE713));

            Assert.Equal("Settings", vm.SelectedEntry?.Name);
            Assert.Equal(Glyph(0xE713), vm.SelectedGlyph);
        }

        [Fact]
        public void Current_glyph_accepts_a_typed_code_point()
        {
            // The stored value might be a code point rather than the character;
            // resolution should still preselect.
            var vm = new GlyphPickerViewModel(SampleCatalogue(), "E80F");

            Assert.Equal("Home", vm.SelectedEntry?.Name);
        }

        [Fact]
        public void Filtering_out_the_selection_drops_it()
        {
            var vm = new GlyphPickerViewModel(SampleCatalogue(), Glyph(0xE713));
            Assert.NotNull(vm.SelectedEntry);

            vm.SearchText = "folder";

            Assert.Null(vm.SelectedEntry);
            Assert.Equal(string.Empty, vm.SelectedGlyph);
        }

        [Fact]
        public void Empty_catalogue_reports_empty()
        {
            var vm = new GlyphPickerViewModel([]);

            Assert.True(vm.IsEmpty);
            Assert.False(vm.HasNoResults); // empty catalogue, not "filtered to nothing"
            Assert.Empty(vm.Results);
        }
    }
}
