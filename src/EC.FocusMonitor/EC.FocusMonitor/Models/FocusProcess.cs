
namespace EC.FocusMonitor.Models;

internal class FocusProcess
{
    public int ProcessId { get; set; }
    public List<FocusWindow> Windows { get; set; } = [];
    public TimeSpan FocusTime => TimeSpan.FromSeconds(Windows.Sum(w => w.FocusTime.TotalSeconds));
}
