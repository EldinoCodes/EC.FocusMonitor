using EC.FocusMonitor.Providers;
using System.Diagnostics;
using System.Drawing.Drawing2D;

internal partial class Program
{
    private static void CreateNotifyIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "Focus Monitor",            
            ContextMenuStrip = new ContextMenuStrip()
            {
                Items = {
                    new ToolStripMenuItem("Open Data Folder", null, OnOpenDataFolder),
                    new ToolStripSeparator(),
                    new ToolStripMenuItem("Exit", null, OnExit)
                }
            },
            Visible = true,
        };
    }
    private static Icon CreateIcon()
    {
        using var bitmap = new Bitmap(128, 128);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Eye shape: two bezier curves joining at the left and right corners
        using var eyePath = new GraphicsPath();
        eyePath.AddBezier(  4f, 64f,  32f,  16f,  96f,  16f, 124f, 64f); // upper lid
        eyePath.AddBezier(124f, 64f,  96f, 112f,  32f, 112f,   4f, 64f); // lower lid
        eyePath.CloseFigure();

        // Sclera (white of the eye)
        g.FillPath(Brushes.White, eyePath);

        // Bullseye rings centered at (64, 64), clipped to the eye shape
        g.SetClip(eyePath);

        using var crimsonBrush = new SolidBrush(Color.Crimson);
        using var darkRedBrush = new SolidBrush(Color.DarkRed);

        static void FillRing(Graphics gr, Brush b, float r)
            => gr.FillEllipse(b, 64f - r, 64f - r, r * 2f, r * 2f);

        FillRing(g, crimsonBrush, 28f);     // outer red
        FillRing(g, Brushes.White, 22f);    // white ring
        FillRing(g, crimsonBrush, 16f);     // red ring
        FillRing(g, Brushes.White, 10f);    // white ring
        FillRing(g, darkRedBrush, 6f);      // bullseye centre

        g.ResetClip();

        // Specular highlight (top-left of iris)
        using var highlightBrush = new SolidBrush(Color.FromArgb(200, Color.White));
        g.FillEllipse(highlightBrush, 42f, 38f, 14f, 10f);

        // Upper eyelid shadow
        using var lidPath = new GraphicsPath();
        lidPath.AddBezier(4f, 64f, 32f, 16f, 96f, 16f, 124f, 64f);
        lidPath.AddLine(124f, 64f, 4f, 64f);
        using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
        g.FillPath(shadowBrush, lidPath);

        // Eye outline: thicker upper lid, thinner lower lid
        using var upperLidPen = new Pen(Color.FromArgb(30, 30, 30), 7.2f);
        using var lowerLidPen = new Pen(Color.FromArgb(50, 50, 50), 4f);
        g.DrawBezier(upperLidPen,   4f, 64f,  32f,  16f,  96f,  16f, 124f, 64f);
        g.DrawBezier(lowerLidPen,   4f, 64f,  32f, 112f,  96f, 112f, 124f, 64f);

        // Upper eyelashes
        using var lashPen = new Pen(Color.FromArgb(30, 30, 30), 4f);
        g.DrawLine(lashPen,  25f, 40f,  16f, 22f);
        g.DrawLine(lashPen,  44f, 30f,  40f, 14f);
        g.DrawLine(lashPen,  84f, 30f,  84f, 14f);
        g.DrawLine(lashPen, 103f, 40f, 112f, 22f);

        var hIcon = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            User32.DestroyIcon(hIcon);
        }
    }
    private static void OnOpenDataFolder(object? sender, EventArgs e)
    {
        var dataDirectory = GetDataDirectory();
        Process.Start(new ProcessStartInfo
        {
            FileName = dataDirectory,
            UseShellExecute = true,
            Verb = "open"
        });
    }
    private static void OnExit(object? sender, EventArgs e)
    {
        _notifyIcon?.Dispose();
        Application.Exit();
    }
}
