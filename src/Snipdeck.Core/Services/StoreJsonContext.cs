using System.Text.Json.Serialization;

using Snipdeck.Core.Models;

namespace Snipdeck.Core.Services
{
    /// <summary>
    /// System.Text.Json source-generated metadata for the persisted documents.
    /// Using generated <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/>
    /// instead of the reflection-based serializer keeps the JSON stores trim-safe
    /// (no IL2026) so the app can be published with PublishTrimmed.
    ///
    /// The options here must reproduce the previous reflection serializer's wire
    /// format exactly (camelCase properties, camelCase string enums) so existing
    /// store/settings files keep loading — see StoreJsonCompatibilityTests.
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        UseStringEnumConverter = true)]
    [JsonSerializable(typeof(SnipStoreDocument))]
    [JsonSerializable(typeof(AppConfig))]
    public sealed partial class StoreJsonContext : JsonSerializerContext
    {
    }
}
