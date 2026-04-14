using System.Text.Json.Serialization;

namespace EC.FocusMonitor.Models;

internal class FocusWindow
{
    [JsonIgnore]
    public nint WindowHandle { get; set; }
    public string? Title { get; set; }
    public TimeSpan FocusTime { get; set; }
}
