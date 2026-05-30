using System.Text.Json.Serialization;

namespace Snipdeck.Core.Models
{
    public enum CloseBehaviour
    {
        [JsonStringEnumMemberName("hideToTray")]
        HideToTray = 0,

        [JsonStringEnumMemberName("exit")]
        Exit = 1,
    }
}
