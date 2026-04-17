using EC.FocusMonitor.Extensions;
using EC.FocusMonitor.Models;
using EC.FocusMonitor.Providers;
using System.Diagnostics;
using System.Text;

internal partial class Program
{
    // Kept alive to prevent the delegate from being garbage-collected while the hook is active.
    private static User32.WinEventDelegate? _winEventDelegate;
    private static nint _hookHandle;

    private static DateTime _lastFocusTime;
    private static int _lastFocusProcessId;
    private static string? _lastFocusKey;
    private static string? _lastFocusTitle;
    private static string? _lastFocusPath;

    private static void StartFocusHook()
    {
        _winEventDelegate = new User32.WinEventDelegate(WinEventProc);
        _hookHandle = User32.SetWinEventHook(
            User32.EVENT_SYSTEM_FOREGROUND, User32.EVENT_SYSTEM_FOREGROUND,
            nint.Zero, _winEventDelegate,
            0, 0,
            User32.WINEVENT_OUTOFCONTEXT | User32.WINEVENT_SKIPOWNPROCESS);
    }

    private static void StopFocusHook()
    {
        if (_hookHandle != nint.Zero)
        {
            User32.UnhookWinEvent(_hookHandle);
            _hookHandle = nint.Zero;
        }
        FlushCurrentFocus();
    }

    private static void FlushCurrentFocus()
    {
        var now = DateTime.UtcNow;
        lock (_cacheLock)
        {
            if (_lastFocusKey is not null)
            {
                var elapsed = now - _lastFocusTime;
                if (elapsed > TimeSpan.Zero)
                    RecordFocusTimeCore(_lastFocusKey, _lastFocusProcessId, _lastFocusTitle, _lastFocusPath, elapsed);
                _lastFocusKey = null;
            }
        }
    }

    // Runs on the STA message-loop thread via WINEVENT_OUTOFCONTEXT.
    private static void WinEventProc(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        var now = DateTime.UtcNow;
        var process = GetWindowProcess(hwnd);
        var title   = GetWindowTitle(hwnd);
        var key       = process?.ProcessName ?? "Idle";
        var processId = process?.Id ?? 0;
        var path      = GetProcessPath(process);

        lock (_cacheLock)
        {
            if (_lastFocusKey is not null)
            {
                var elapsed = now - _lastFocusTime;
                if (elapsed > TimeSpan.Zero)
                    RecordFocusTimeCore(_lastFocusKey, _lastFocusProcessId, _lastFocusTitle, _lastFocusPath, elapsed);
            }

            _lastFocusTime      = now;
            _lastFocusKey       = key;
            _lastFocusProcessId = processId;
            _lastFocusTitle     = title;
            _lastFocusPath      = path;
        }
    }

    // Caller must hold _cacheLock.
    private static void RecordFocusTimeCore(string key, int processId, string? title, string? path, TimeSpan elapsed)
    {
        if (!_cache.TryGetValue(key, out var focusApplication))
        {
            focusApplication = new FocusApplication { Name = key };
            _cache[key] = focusApplication;
        }

        focusApplication.Path ??= path;

        var focusProcess = focusApplication.Processes.FirstOrDefault(p => p.ProcessId == processId);
        if (focusProcess is null)
        {
            focusProcess = new FocusProcess { ProcessId = processId };
            focusApplication.Processes.Add(focusProcess);
        }

        var focusWindow = focusProcess.Windows.FirstOrDefault(w => w.Title == title);
        if (focusWindow is null)
        {
            focusWindow = new FocusWindow { Title = title ?? "Unknown" };
            focusProcess.Windows.Add(focusWindow);
        }

        focusWindow.FocusTime += elapsed;
    }

    private static async Task WriterAsync(CancellationToken cancellationToken = default)
    {
        var timeSpan = TimeSpan.FromSeconds(30);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await WriteSnapshotAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WriterAsync] {ex}");
            }

            try
            {
                await Task.Delay(timeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        try
        {
            await WriteSnapshotAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WriterAsync] {ex}");
        }
    }

    private static async Task WriteSnapshotAsync()
    {
        var currentDate = GetCurrentDate();
        if (_currentDate != currentDate)
        {
            lock (_cacheLock)
            {
                _cache.Clear();
                _lastFocusKey = null;
            }
            _currentDate = currentDate;
            return;
        }

        var filePath = Path.Combine(GetDataDirectory(), $"{currentDate}.json");

        string? json;
        lock (_cacheLock)
        {
            var snapshot = _cache.Values.ToList();
            json = snapshot.Count > 0 ? snapshot.ToJson() : null;
        }

        if (json is not null)
            await File.WriteAllTextAsync(filePath, json);
    }

    private static void LoadCache()
    {
        var filePath = Path.Combine(GetDataDirectory(), $"{GetCurrentDate()}.json");
        if (!File.Exists(filePath)) return;

        try
        {
            var json = File.ReadAllText(filePath);
            foreach (var focusApplication in json.FromJson<List<FocusApplication>>() ?? [])
            {
                if (string.IsNullOrEmpty(focusApplication?.Name)) continue;
                _cache.TryAdd(focusApplication.Name, focusApplication);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoadCache] {ex}");
        }
    }

    private static Process? GetWindowProcess(nint handle)
    {
        _ = User32.GetWindowThreadProcessId(handle, out var processId);
        try
        {
            return Process.GetProcessById((int)processId);
        }
        catch (ArgumentException)
        {
            return default;
        }
    }

    private static string? GetProcessPath(Process? process)
    {
        try
        {
            return process?.MainModule?.FileName;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? GetWindowTitle(nint handle)
    {
        var sb = new StringBuilder(256);
        return User32.GetWindowText(handle, sb, sb.Capacity) > 0 ? sb.ToString() : null;
    }

    private static string GetCurrentDate() => DateTime.UtcNow.ToString("yyyy-MM-dd");

    private static string GetDataDirectory()
    {
        var dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusMonitor", "data");
        if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
        return dataFolder;
    }
}
