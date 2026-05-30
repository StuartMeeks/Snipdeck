using System.Text.Json.Serialization;

namespace Snipdeck.Core.Models
{
    public enum ThemePreference
    {
        [JsonStringEnumMemberName("system")]
        System = 0,

        [JsonStringEnumMemberName("light")]
        Light = 1,

        [JsonStringEnumMemberName("dark")]
        Dark = 2,
    }
}
