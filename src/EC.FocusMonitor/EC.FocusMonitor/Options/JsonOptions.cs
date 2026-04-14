using System.Text.Json;
using System.Text.Json.Serialization;

namespace EC.FocusMonitor.Options;

internal static class JsonOptions
{
    internal static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
