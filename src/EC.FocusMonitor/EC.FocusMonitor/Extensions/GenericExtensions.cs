using EC.FocusMonitor.Options;
using System.Text.Json;

namespace EC.FocusMonitor.Extensions;

internal static class GenericExtensions
{
    internal static string? ToJson<T>(this T? obj)
    {
        if (obj is null) return default;
        if (obj is string ret) return ret;
        return JsonSerializer.Serialize(obj, JsonOptions.Default);
    }
}
