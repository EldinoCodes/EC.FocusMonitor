using EC.FocusMonitor.Options;
using System.Text.Json;

namespace EC.FocusMonitor.Extensions;

internal static class StringExtensions
{
    internal static T? FromJson<T>(this string? jsonString)
    {
        if (string.IsNullOrEmpty(jsonString)) return default;
        if (!jsonString.StartsWith('{') && !jsonString.StartsWith('[')) return default;
        return JsonSerializer.Deserialize<T>(jsonString, JsonOptions.Default);
    }
}
