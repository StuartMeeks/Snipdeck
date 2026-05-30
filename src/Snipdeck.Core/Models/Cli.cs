namespace Snipdeck.Core.Models
{
    public sealed class Cli
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string? IconRef { get; set; }

        /// <summary>
        /// Parameter definitions shared by every Snip under this CLI. A Snip's
        /// <c>{token}</c> resolves to one of these by name when the Snip has no
        /// local parameter of that name. See <c>ParameterResolver</c>.
        /// </summary>
        public List<Parameter> Parameters { get; set; } = [];
    }
}
