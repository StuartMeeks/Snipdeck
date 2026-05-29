using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// One entry in the CLI switcher dropdown. <see cref="IsHome"/> is true for
    /// the synthetic "All / Home" entry that sits at the top of the list; for
    /// every other entry <see cref="Cli"/> points at a real <see cref="Cli"/>.
    /// </summary>
    public sealed class CliChoice
    {
        public Cli? Cli { get; init; }

        public string Display { get; init; } = string.Empty;

        public bool IsHome => Cli is null;

        public override string ToString()
        {
            return Display;
        }
    }
}
