namespace Snipdeck.Core.Models
{
    public sealed class Parameter
    {
        public string Name { get; set; } = string.Empty;

        public ParameterType Type { get; set; } = ParameterType.Text;

        public List<string> Options { get; set; } = [];

        public string? Default { get; set; }
    }
}
