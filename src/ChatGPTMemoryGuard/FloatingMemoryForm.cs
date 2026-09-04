using System.Drawing.Drawing2D;
using ChatGPTMemoryGuard.Core;

namespace ChatGPTMemoryGuard;

public sealed class FloatingMemoryForm : Form
{
    private const long FourGiB = 4L * 1024L * 1024L * 1024L;
    private readonly Action<Point> _positionChanged;
    private MemorySnapshot _snapshot = new(DateTimeOffset.Now, 0, 0, 0, 0);
    private AlertLevel _level = AlertLevel.Normal;
    private bool _dragging;
    private Point _dragStartCursor;
    private Point _dragStartWindow;

    public FloatingMemoryForm(ContextMenuStrip menu, Action<Point> positionChanged)
    {
        ArgumentNullException.ThrowIfNull(menu);
        _positionChanged = positionChanged ?? throw new ArgumentNullException(nameof(positionChanged));

        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(31, 33, 36);
        ClientSize = new Size(380, 72);
        ContextMenuStrip = menu;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "FloatingMemoryWindow";
        Opacity = 0.96;
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "ChatGPT Memory Guard";
        TopMost = true;
        DoubleBuffered = true;
        Cursor = Cursors.SizeAll;
        AccessibleName = "ChatGPT 内存实时悬浮窗";
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WsExToolWindow = 0x00000080;
            const int WsExNoActivate = 0x08000000;
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= WsExToolWindow | WsExNoActivate;
            return parameters;
        }
    }

    public void UpdateSnapshot(MemorySnapshot snapshot, AlertLevel level)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _level = level;
        Invalidate();
    }

    protected override void OnSizeChanged(EventArgs eventArgs)
    {
        base.OnSizeChanged(eventArgs);
        using GraphicsPath path = CreateRoundedRectangle(ClientRectangle, 12);
        Region?.Dispose();
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        Graphics graphics = eventArgs.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.FromArgb(31, 33, 36));

        Color accent = GetAccentColor();
        using var accentBrush = new SolidBrush(accent);
        graphics.FillEllipse(accentBrush, 12, 12, 8, 8);

        using var titleFont = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var primaryFont = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold, GraphicsUnit.Point);
        using var secondaryFont = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        TextRenderer.DrawText(graphics, "CHATGPT 内存", titleFont, new Point(25, 8), Color.FromArgb(185, 190, 196), Color.Transparent);

        OverlayText text = DisplayText.BuildOverlay(_snapshot);
        TextRenderer.DrawText(graphics, text.Primary, primaryFont, new Point(11, 27), Color.White, Color.Transparent);
        Size secondarySize = TextRenderer.MeasureText(text.Secondary, secondaryFont);
        TextRenderer.DrawText(
            graphics,
            text.Secondary,
            secondaryFont,
            new Point(ClientSize.Width - secondarySize.Width - 10, 33),
            Color.FromArgb(205, 209, 214),
            Color.Transparent);

        long monitoredBytes = Math.Max(_snapshot.TotalWorkingSetBytes, _snapshot.LargestPrivateBytes);
        float ratio = Math.Clamp(monitoredBytes / (float)FourGiB, 0f, 1f);
        using var trackBrush = new SolidBrush(Color.FromArgb(65, 70, 75));
        graphics.FillRectangle(trackBrush, 0, ClientSize.Height - 4, ClientSize.Width, 4);
        graphics.FillRectangle(accentBrush, 0, ClientSize.Height - 4, ClientSize.Width * ratio, 4);
    }

    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        if (eventArgs.Button == MouseButtons.Left)
        {
            _dragging = true;
            _dragStartCursor = Cursor.Position;
            _dragStartWindow = Location;
            Capture = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs eventArgs)
    {
        base.OnMouseMove(eventArgs);
        if (_dragging)
        {
            Point cursor = Cursor.Position;
            Location = new Point(
                _dragStartWindow.X + cursor.X - _dragStartCursor.X,
                _dragStartWindow.Y + cursor.Y - _dragStartCursor.Y);
        }
    }

    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        base.OnMouseUp(eventArgs);
        if (_dragging && eventArgs.Button == MouseButtons.Left)
        {
            _dragging = false;
            Capture = false;
            _positionChanged(Location);
        }
    }

    private Color GetAccentColor()
    {
        if (_snapshot.ProcessCount == 0)
        {
            return Color.FromArgb(135, 140, 145);
        }

        return _level switch
        {
            AlertLevel.Warning => Color.FromArgb(255, 179, 0),
            AlertLevel.Critical => Color.FromArgb(245, 75, 75),
            _ => Color.FromArgb(53, 199, 112),
        };
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
