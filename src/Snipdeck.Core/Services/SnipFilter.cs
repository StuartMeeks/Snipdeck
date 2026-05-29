using Snipdeck.Core.Models;

namespace Snipdeck.Core.Services
{
    /// <summary>
    /// Pure filter helpers used by view models when search text or tag scope
    /// changes. Implemented as a standalone static class so they can be unit
    /// tested without any view-model plumbing.
    /// </summary>
    public static class SnipFilter
    {
        public static IEnumerable<Snip> Apply(
            IEnumerable<Snip> snips,
            string? searchText,
            string? selectedTag,
            bool includeTrash = false)
        {
            ArgumentNullException.ThrowIfNull(snips);

            var search = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim();
            var tag = string.IsNullOrWhiteSpace(selectedTag) ? null : selectedTag.Trim();

            foreach (var snip in snips)
            {
                if (!includeTrash && snip.IsTrash)
                {
                    continue;
                }
                if (tag is not null && !snip.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (search is not null && !MatchesSearch(snip, search))
                {
                    continue;
                }
                yield return snip;
            }
        }

        public static IEnumerable<string> DistinctTagsFor(IEnumerable<Snip> snips)
        {
            ArgumentNullException.ThrowIfNull(snips);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var snip in snips)
            {
                if (snip.IsTrash)
                {
                    continue;
                }
                foreach (var tag in snip.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag) && seen.Add(tag))
                    {
                        yield return tag;
                    }
                }
            }
        }

        private static bool MatchesSearch(Snip snip, string search)
        {
            return ContainsOrdinalIgnoreCase(snip.Title, search)
                || ContainsOrdinalIgnoreCase(snip.CommandTemplate, search)
                || snip.Tags.Any(t => ContainsOrdinalIgnoreCase(t, search));
        }

        private static bool ContainsOrdinalIgnoreCase(string? haystack, string needle)
        {
            return haystack is not null
                && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
        }
    }
}
