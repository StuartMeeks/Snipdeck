using System.Text.Json.Serialization;

namespace Snipdeck.Core.Models
{
    public enum ParameterType
    {
        [JsonStringEnumMemberName("text")]
        Text = 0,

        [JsonStringEnumMemberName("choice")]
        Choice = 1,
    }
}
