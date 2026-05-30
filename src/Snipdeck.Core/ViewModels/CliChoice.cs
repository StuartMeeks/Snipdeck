using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// One entry in the CLI switcher (title bar). <see cref="IsAll"/> is true for
    /// the synthetic "All" entry at the top — the unscoped view across every CLI;
    /// for every other entry <see cref="Cli"/> points at a real <see cref="Cli"/>.
    /// </summary>
    public sealed class CliChoice
    {
        public Cli? Cli { get; init; }

        public string Display { get; init; } = string.Empty;

        public bool IsAll => Cli is null;

        public override string ToString()
        {
            return Display;
        }
    }
}
