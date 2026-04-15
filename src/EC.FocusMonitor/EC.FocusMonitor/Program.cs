using EC.FocusMonitor.Models;
using System.Collections.Concurrent;

internal partial class Program
{
    private static readonly CancellationTokenSource _cancellationTokenSource = new();
    private static readonly ConcurrentDictionary<string, FocusApplication> _cache = [];
    private static readonly object _cacheLock = new();

    private static NotifyIcon? _notifyIcon;
    private static string? _currentDate;

    [STAThread]
    private static async Task Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        CreateNotifyIcon();

        _currentDate = GetCurrentDate();

        LoadCache();
        StartFocusHook();

        var writerTask = WriterAsync(_cancellationTokenSource.Token);

        try
        {
            Application.Run();

            StopFocusHook();
            await _cancellationTokenSource.CancelAsync();
            await writerTask;
        }
        finally
        {
            _notifyIcon?.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}
