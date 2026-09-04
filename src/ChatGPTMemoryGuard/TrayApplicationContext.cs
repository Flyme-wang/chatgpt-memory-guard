using System.Diagnostics;
using ChatGPTMemoryGuard.Core;

namespace ChatGPTMemoryGuard;

public sealed class TrayApplicationContext : ApplicationContext
{
    private GuardSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly LogWriter _log;
    private readonly ProcessMemoryReader _memoryReader;
    private readonly AlertEvaluator _alertEvaluator;
    private readonly StartupRegistration _startupRegistration;
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _overlayItem;
    private readonly ContextMenuStrip _menu;
    private readonly FloatingMemoryForm _overlay;
    private readonly Icon _normalIcon = IconFactory.Create(Color.FromArgb(30, 160, 85));
    private readonly Icon _warningIcon = IconFactory.Create(Color.FromArgb(230, 160, 20));
    private readonly Icon _criticalIcon = IconFactory.Create(Color.FromArgb(210, 55, 55));
    private readonly Icon _inactiveIcon = IconFactory.Create(Color.FromArgb(125, 125, 125));
    private MemorySnapshot _lastSnapshot = new(DateTimeOffset.Now, 0, 0, 0, 0);
    private AlertLevel? _lastLevel;
    private bool _disposed;

    public TrayApplicationContext(GuardSettings settings, SettingsStore settingsStore, LogWriter log, StartupRegistration startupRegistration)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _startupRegistration = startupRegistration ?? throw new ArgumentNullException(nameof(startupRegistration));
        _memoryReader = new ProcessMemoryReader(_log.Write);
        _alertEvaluator = new AlertEvaluator(
            settings.WarningBytes,
            settings.CriticalBytes,
            TimeSpan.FromMinutes(settings.CooldownMinutes),
            settings.ResetMarginBytes);

        _statusItem = new ToolStripMenuItem("正在读取内存…") { Enabled = false };
        var refreshItem = new ToolStripMenuItem("立即刷新", null, (_, _) => RefreshSnapshot());
        _overlayItem = new ToolStripMenuItem("显示桌面悬浮窗", null, ToggleOverlay) { Checked = settings.ShowOverlay };
        _startupItem = new ToolStripMenuItem("随 Windows 启动", null, ToggleStartup);
        SetStartupCheckSafely();
        var openLogsItem = new ToolStripMenuItem("打开日志目录", null, OpenLogs);
        var exitItem = new ToolStripMenuItem("退出", null, ExitRequested);
        _menu = new ContextMenuStrip();
        _menu.Items.AddRange([
            _statusItem,
            new ToolStripSeparator(),
            refreshItem,
            _overlayItem,
            _startupItem,
            openLogsItem,
            new ToolStripSeparator(),
            exitItem,
        ]);

        _notifyIcon = new NotifyIcon
        {
            Icon = _inactiveIcon,
            Text = "ChatGPT Memory Guard",
            ContextMenuStrip = _menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowCurrentStatus();

        _overlay = new FloatingMemoryForm(_menu, SaveOverlayPosition);
        PositionOverlay();
        if (settings.ShowOverlay)
        {
            _overlay.Show();
        }

        _timer = new System.Windows.Forms.Timer { Interval = settings.PollIntervalSeconds * 1000 };
        _timer.Tick += (_, _) => RefreshSnapshot();
        _timer.Start();

        _log.Write($"监测已启动：普通预警 {settings.WarningGiB:0.0} GB，紧急预警 {settings.CriticalGiB:0.0} GB，间隔 {settings.PollIntervalSeconds} 秒。");
        RefreshSnapshot();
    }

    private void RefreshSnapshot()
    {
        try
        {
            _lastSnapshot = _memoryReader.Capture();
            AlertDecision decision = _alertEvaluator.Evaluate(_lastSnapshot, DateTimeOffset.Now);
            UpdateVisualState(decision.Level);
            _overlay.UpdateSnapshot(_lastSnapshot, decision.Level);

            if (decision.Level != _lastLevel)
            {
                _log.Write($"状态变为 {decision.Level}：总计 {DisplayText.FormatGiB(_lastSnapshot.TotalWorkingSetBytes)}，最大进程 {DisplayText.FormatGiB(_lastSnapshot.LargestPrivateBytes)}。");
                _lastLevel = decision.Level;
            }

            if (decision.ShouldNotify)
            {
                ShowAlert(decision.Level);
            }
        }
        catch (Exception error)
        {
            _log.Write($"刷新失败：{error.Message}");
        }
    }

    private void UpdateVisualState(AlertLevel level)
    {
        _statusItem.Text = DisplayText.BuildStatus(_lastSnapshot);
        string tooltip = DisplayText.BuildTooltip(_lastSnapshot);
        _notifyIcon.Text = tooltip.Length <= 63 ? tooltip : tooltip[..63];
        _notifyIcon.Icon = _lastSnapshot.ProcessCount == 0
            ? _inactiveIcon
            : level switch
            {
                AlertLevel.Warning => _warningIcon,
                AlertLevel.Critical => _criticalIcon,
                _ => _normalIcon,
            };
    }

    private void ShowAlert(AlertLevel level)
    {
        NotificationText notification = DisplayText.BuildNotification(level, _lastSnapshot);
        _notifyIcon.ShowBalloonTip(
            10_000,
            notification.Title,
            notification.Message,
            level == AlertLevel.Critical ? ToolTipIcon.Error : ToolTipIcon.Warning);
        _log.Write($"已显示{notification.Title}：{notification.Message}");
    }

    private void ShowCurrentStatus()
    {
        _notifyIcon.ShowBalloonTip(5_000, "ChatGPT 当前内存", DisplayText.BuildStatus(_lastSnapshot), ToolTipIcon.Info);
    }

    private void ToggleOverlay(object? sender, EventArgs args)
    {
        bool show = !_overlay.Visible;
        if (show)
        {
            PositionOverlay();
            _overlay.Show();
        }
        else
        {
            _overlay.Hide();
        }

        _overlayItem.Checked = show;
        _settings = _settings with { ShowOverlay = show };
        SaveSettingsSafely();
    }

    private void PositionOverlay()
    {
        Point requested = new(_settings.OverlayLeft ?? Cursor.Position.X, _settings.OverlayTop ?? Cursor.Position.Y);
        Screen screen = _settings.OverlayLeft is null || _settings.OverlayTop is null
            ? Screen.PrimaryScreen ?? Screen.FromPoint(Cursor.Position)
            : Screen.FromPoint(requested);
        _overlay.Location = OverlayPlacement.Clamp(
            _settings.OverlayLeft,
            _settings.OverlayTop,
            _overlay.Size,
            screen.WorkingArea);
    }

    private void SaveOverlayPosition(Point location)
    {
        _settings = _settings with { OverlayLeft = location.X, OverlayTop = location.Y };
        SaveSettingsSafely();
    }

    private void SaveSettingsSafely()
    {
        try
        {
            _settingsStore.Save(_settings);
        }
        catch (Exception error)
        {
            _log.Write($"保存悬浮窗设置失败：{error.Message}");
        }
    }

    private void ToggleStartup(object? sender, EventArgs args)
    {
        try
        {
            bool enable = !_startupRegistration.IsEnabled();
            _startupRegistration.SetEnabled(enable);
            _startupItem.Checked = enable;
            _log.Write(enable ? "已启用随 Windows 启动。" : "已关闭随 Windows 启动。");
        }
        catch (Exception error)
        {
            _log.Write($"修改开机启动失败：{error.Message}");
            MessageBox.Show($"无法修改开机启动设置：{error.Message}", "ChatGPT Memory Guard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SetStartupCheckSafely()
    {
        try
        {
            _startupItem.Checked = _startupRegistration.IsEnabled();
        }
        catch (Exception error)
        {
            _log.Write($"读取开机启动设置失败：{error.Message}");
            _startupItem.Checked = false;
        }
    }

    private void OpenLogs(object? sender, EventArgs args)
    {
        try
        {
            Directory.CreateDirectory(_log.DirectoryPath);
            Process.Start(new ProcessStartInfo(_log.DirectoryPath) { UseShellExecute = true });
        }
        catch (Exception error)
        {
            _log.Write($"打开日志目录失败：{error.Message}");
            MessageBox.Show($"无法打开日志目录：{error.Message}", "ChatGPT Memory Guard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExitRequested(object? sender, EventArgs args)
    {
        _log.Write("监测已退出。");
        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        _overlay.Hide();
        _notifyIcon.Visible = false;
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _timer.Stop();
            _timer.Dispose();
            _overlay.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _menu.Dispose();
            _normalIcon.Dispose();
            _warningIcon.Dispose();
            _criticalIcon.Dispose();
            _inactiveIcon.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
