using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CatGuard.Core;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace CatGuard;

internal sealed class GuardWindow : Window
{
    private readonly KeyboardHook hook;
    private readonly UserSettings settings = UserSettings.Load();
    private readonly Forms.NotifyIcon tray;
    private readonly CheckBox enabled = new() { Content = "启用自动防护" };
    private readonly CheckBox warn = new() { Content = "仅提示，不屏蔽输入" };
    private readonly CheckBox stay = new() { Content = "自动触发后保持锁定，直到我解锁" };
    private readonly CheckBox sound = new() { Content = "锁定时播放提示音" };
    private readonly CheckBox startup = new() { Content = "开机启动到托盘（自动防护初始关闭）" };
    private readonly Slider sensitivity = new() { Minimum = 1, Maximum = 10, TickFrequency = 1, IsSnapToTickEnabled = true, Margin = new Thickness(0, 8, 0, 8) };
    private readonly TextBlock level = new();
    private readonly TextBlock status = new() { FontSize = 26, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 8) };
    private readonly TextBlock detail = new() { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.SlateGray };
    private readonly TextBlock error = new() { Foreground = Brushes.Firebrick, TextWrapping = TextWrapping.Wrap };
    private readonly Button toggle = new() { Content = "立即锁定键盘", Padding = new Thickness(18, 10, 18, 10), Margin = new Thickness(0, 14, 8, 0) };
    private readonly Window hint;
    private GuardState previous = GuardState.Off;
    private bool initializing = true;
    private bool exiting;

    public GuardWindow(KeyboardHook hook)
    {
        this.hook = hook;
        Title = "WindowsCatGuard · 防猫踩键盘";
        Width = 540; Height = 670; MinWidth = 500; MinHeight = 640;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(247, 248, 251));
        FontFamily = new FontFamily("Microsoft YaHei UI"); FontSize = 14;
        var panel = new StackPanel { Margin = new Thickness(28, 22, 28, 22) };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = "🐾  WindowsCatGuard", FontSize = 23, FontWeight = FontWeights.Bold });
        panel.Children.Add(new TextBlock { Text = "让猫陪着你，也让键盘安静下来。", Foreground = Brushes.SlateGray, Margin = new Thickness(0, 5, 0, 10) });
        panel.Children.Add(status); panel.Children.Add(detail);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        toggle.Click += (_, _) => hook.Toggle(); buttons.Children.Add(toggle);
        var unlock = new Button { Content = "解锁", Padding = new Thickness(18, 10, 18, 10), Margin = new Thickness(0, 14, 0, 0) };
        unlock.Click += (_, _) => hook.Unlock(); buttons.Children.Add(unlock); panel.Children.Add(buttons);
        panel.Children.Add(new TextBlock { Text = "Ctrl + Alt + L  随时切换锁定 · 鼠标始终可用", Foreground = Brushes.SlateGray, Margin = new Thickness(0, 12, 0, 20) });
        AddOption(panel, enabled); AddOption(panel, warn);
        panel.Children.Add(new TextBlock { Text = "检测灵敏度", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(sensitivity); panel.Children.Add(level);
        AddOption(panel, stay); AddOption(panel, sound); AddOption(panel, startup);
        panel.Children.Add(new TextBlock { Text = "自动模式：所有按键松开且安静 1 秒后恢复。\n手动锁定：保持到你主动解锁。\n自动检测触发前，少量输入可能已经进入应用。", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.SlateGray, FontSize = 12, Margin = new Thickness(0, 18, 0, 12) });
        panel.Children.Add(error);
        var bottom = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
        var minimize = new Button { Content = "收起到托盘", Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(0, 0, 8, 0) };
        minimize.Click += (_, _) => Hide(); bottom.Children.Add(minimize);
        var exit = new Button { Content = "退出并恢复输入", Padding = new Thickness(12, 5, 12, 5) };
        exit.Click += (_, _) => ExitApp(); bottom.Children.Add(exit); panel.Children.Add(bottom);

        tray = new Forms.NotifyIcon { Icon = System.Drawing.SystemIcons.Information, Text = "WindowsCatGuard · 防护关闭", Visible = true };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("打开", null, (_, _) => Open());
        menu.Items.Add("锁定 / 解锁  Ctrl+Alt+L", null, (_, _) => hook.Toggle());
        menu.Items.Add("解锁", null, (_, _) => hook.Unlock());
        menu.Items.Add("退出并恢复输入", null, (_, _) => ExitApp());
        tray.ContextMenuStrip = menu; tray.DoubleClick += (_, _) => Open();

        hint = MakeHint();
        enabled.IsChecked = settings.Enabled; warn.IsChecked = settings.WarnOnly;
        stay.IsChecked = settings.StayLocked; sound.IsChecked = settings.Sound;
        startup.IsChecked = settings.StartWithWindows; sensitivity.Value = settings.Sensitivity;
        foreach (var box in new[] { enabled, warn, stay, sound }) box.Click += (_, _) => Apply();
        startup.Click += (_, _) =>
        {
            try { UserSettings.SetStartup(startup.IsChecked == true); Apply(); }
            catch (Exception ex) { startup.IsChecked = settings.StartWithWindows; error.Text = "开机启动设置失败：" + ex.Message; }
        };
        sensitivity.ValueChanged += (_, _) => Apply();
        hook.Changed += OnHookChanged; hook.Failed += OnFailure;
        SystemEvents.SessionSwitch += OnSession;
        SystemEvents.PowerModeChanged += OnPower;
        Closing += (_, e) => { if (!exiting) { e.Cancel = true; Hide(); } };
        initializing = false;
        Apply(); Refresh(hook.Snapshot);
    }

    private static void AddOption(StackPanel panel, CheckBox option) { option.Margin = new Thickness(0, 10, 0, 0); panel.Children.Add(option); }
    private void Open() { Show(); WindowState = WindowState.Normal; Activate(); }
    private void OnHookChanged(GuardSnapshot snapshot) => Dispatcher.BeginInvoke(() => { if (!exiting) Refresh(snapshot); });
    private void OnFailure(string text) => Dispatcher.BeginInvoke(() =>
    {
        if (exiting) return;
        settings.Enabled = false; enabled.IsChecked = false; error.Text = text;
        Refresh(hook.Snapshot);
    });
    private void OnSession(object sender, SessionSwitchEventArgs e) { if (!exiting) hook.Suspend(); }
    private void OnPower(object sender, PowerModeChangedEventArgs e) { if (!exiting && e.Mode is PowerModes.Suspend or PowerModes.Resume) hook.Suspend(); }

    private void Apply()
    {
        if (initializing || exiting) return;
        settings.Enabled = enabled.IsChecked == true; settings.WarnOnly = warn.IsChecked == true;
        settings.StayLocked = stay.IsChecked == true; settings.Sound = sound.IsChecked == true;
        settings.StartWithWindows = startup.IsChecked == true; settings.Sensitivity = (int)sensitivity.Value;
        level.Text = $"{settings.Sensitivity} / 10    数值越大越敏感；容易误触时调低。";
        error.Text = "";
        hook.Configure(settings);
        try { settings.Save(); } catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException) { error.Text = "设置暂未保存：" + ex.Message; }
    }

    private void Refresh(GuardSnapshot s)
    {
        bool locked = s.State is GuardState.AutoLocked or GuardState.ManualLocked;
        string label = s.State switch { GuardState.Watching => "正在监测", GuardState.AutoLocked => "猫咪来访 · 键盘已暂停", GuardState.ManualLocked => "键盘已手动锁定", _ => "防护已关闭" };
        status.Text = label; status.Foreground = locked ? Brushes.DarkOrange : s.State == GuardState.Watching ? Brushes.SeaGreen : Brushes.SlateGray;
        string reason = s.Rule switch { "PawCluster" => "邻近多键同时按下", "MashBurst" => "短时间密集乱按", "SustainedHold" => "多键持续压住", _ => "暂无" };
        detail.Text = $"本次启动触发 {s.Triggers} 次 · 最近原因：{reason}";
        toggle.Content = locked ? "恢复键盘输入" : "立即锁定键盘";
        toggle.IsEnabled = !settings.WarnOnly || locked;
        tray.Text = "WindowsCatGuard · " + label;
        if (locked)
        {
            if (!hint.IsVisible)
            {
                var area = SystemParameters.WorkArea;
                hint.Left = area.Right - hint.Width - 20; hint.Top = area.Bottom - hint.Height - 20; hint.Show();
            }
            if (previous is not (GuardState.AutoLocked or GuardState.ManualLocked) && settings.Sound) System.Media.SystemSounds.Asterisk.Play();
        }
        else hint.Hide();
        previous = s.State;
    }

    private Window MakeHint()
    {
        var content = new StackPanel { Margin = new Thickness(20) };
        content.Children.Add(new TextBlock { Text = "🐈  键盘已暂停", FontSize = 22 });
        content.Children.Add(new TextBlock { Text = "Ctrl+Alt+L 或点击下方恢复", Margin = new Thickness(0, 8, 0, 12) });
        var button = new Button { Content = "解锁键盘", Padding = new Thickness(8) };
        button.Click += (_, _) => hook.Unlock(); content.Children.Add(button);
        var window = new Window { Title = "键盘已暂停", Width = 310, Height = 180, Content = content, Topmost = true, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.ToolWindow, ResizeMode = ResizeMode.NoResize, FontFamily = FontFamily };
        window.Closing += (_, e) => { if (!exiting) { e.Cancel = true; hook.Unlock(); } };
        return window;
    }

    public void ExitApp()
    {
        if (exiting) return;
        exiting = true;
        hook.Changed -= OnHookChanged; hook.Failed -= OnFailure;
        SystemEvents.SessionSwitch -= OnSession; SystemEvents.PowerModeChanged -= OnPower;
        hook.Dispose(); hint.Close(); tray.Visible = false; tray.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}
