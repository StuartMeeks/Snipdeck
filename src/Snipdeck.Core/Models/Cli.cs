namespace Snipdeck.Core.Models
{
    public sealed class Cli
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string? IconRef { get; set; }
    }
}
