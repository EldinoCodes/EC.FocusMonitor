
namespace EC.FocusMonitor.Models;

internal class FocusApplication
{
    public string? Name { get; set; }
    public string? Path { get; set; }
    public List<FocusProcess> Processes { get; set; } = [];
    public TimeSpan FocusTime => TimeSpan.FromSeconds(Processes.Sum(p => p.FocusTime.TotalSeconds));
}
