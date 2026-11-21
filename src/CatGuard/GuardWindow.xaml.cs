using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CatGuard.Core;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace CatGuard;

internal partial class GuardWindow : Window
{
    private readonly KeyboardHook hook;
    private readonly UserSettings settings;
    private readonly UiText ui;
    private readonly bool persist;
    private readonly Forms.NotifyIcon tray;
    private readonly Forms.ContextMenuStrip menu = new();
    private readonly Window hint;
    private readonly TextBlock hintTitle = new() { FontSize = 21, FontWeight = FontWeights.Bold };
    private readonly TextBlock hintShortcut = new() { FontSize = 12, Margin = new Thickness(0, 7, 0, 14), TextWrapping = TextWrapping.Wrap };
    private readonly Button hintButton = new() { Padding = new Thickness(14, 8, 14, 8), FontWeight = FontWeights.Bold };
    private readonly DispatcherTimer statusTimer;
    private GuardSnapshot snapshot;
    private GuardState previous = GuardState.Off;
    private bool initializing = true;
    private bool exiting;

    public GuardWindow(KeyboardHook hook, UserSettings? options = null, bool persist = true)
    {
        InitializeComponent();
        this.hook = hook;
        this.persist = persist;
        settings = options ?? UserSettings.Load();
        ui = new UiText(settings.Language);
        snapshot = hook.Snapshot;
        Height = Math.Min(Height, Math.Max(MinHeight, SystemParameters.WorkArea.Height - 42));
        EnabledOption.IsChecked = settings.Enabled;
        WarnOption.IsChecked = settings.WarnOnly;
        StayOption.IsChecked = settings.StayLocked;
        SoundOption.IsChecked = settings.Sound;
        StartupOption.IsChecked = settings.StartWithWindows;
        SensitivitySlider.Value = settings.Sensitivity;

        using var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/WindowsCatGuard;component/assets/paw.ico")).Stream;
        using var icon = new System.Drawing.Icon(iconStream);
        tray = new Forms.NotifyIcon { Icon = (System.Drawing.Icon)icon.Clone(), Visible = true };
        menu.Items.Add("", null, (_, _) => Open());
        menu.Items.Add("", null, (_, _) => hook.Toggle());
        menu.Items.Add("", null, (_, _) => hook.Unlock());
        menu.Items.Add("", null, (_, _) => ExitApp());
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => Open();
        hint = MakeHint();
        hook.Changed += OnHookChanged;
        hook.Failed += OnFailure;
        SystemEvents.SessionSwitch += OnSession;
        SystemEvents.PowerModeChanged += OnPower;
        Closing += (_, e) => { if (!exiting) { e.Cancel = true; Hide(); } };
        ApplyLanguage();
        initializing = false;
        ApplyOptions();
        statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        statusTimer.Tick += (_, _) => { if (!exiting && IsVisible) RefreshCounters(hook.Snapshot); };
        statusTimer.Start();
    }

    internal string CurrentLanguage => ui.Language;
    internal string VisibleStatus => StatusText.Text;
    internal string VisibleSettingsTitle => SettingsText.Text;
    internal (GuardState State, bool Enabled) ViewState => (snapshot.State, settings.Enabled);
    internal double WindowWidth => Width;

    internal void SwitchLanguage(string language, bool save = true)
    {
        ui.SetLanguage(language);
        settings.Language = ui.Language;
        ApplyLanguage();
        if (save) SaveSettings();
    }

    private void ApplyLanguage()
    {
        Title = ui["WindowTitle"];
        TaglineText.Text = ui["Tagline"];
        VersionText.Text = ui["Version"];
        ProtectionText.Text = ui["Protection"];
        MouseText.Text = ui["MouseAvailable"];
        TriggersLabel.Text = ui["Triggers"];
        HeldLabel.Text = ui["Held"];
        BlockedLabel.Text = ui["Blocked"];
        SettingsText.Text = ui["Settings"];
        AdvancedBox.Header = ui["MoreSettings"];
        EnabledOption.Content = ui["AutoGuard"];
        EnabledHintText.Text = ui["AutoGuardHint"];
        WarnOption.Content = ui["WarnOnly"];
        WarnHintText.Text = ui["WarnOnlyHint"];
        SensitivityText.Text = ui["Sensitivity"];
        LessText.Text = ui["LessSensitive"];
        MoreText.Text = ui["MoreSensitive"];
        StayOption.Content = ui["StayLocked"];
        SoundOption.Content = ui["Sound"];
        StartupOption.Content = ui["Startup"];
        StartupHintText.Text = ui["StartupHint"];
        TestText.Text = ui["TestHint"];
        LimitText.Text = ui["LimitHint"];
        DiagnosticsBox.Header = ui["Diagnostics"];
        HideButton.Content = ui["Hide"];
        ExitButton.Content = ui["Exit"];
        UnlockButton.Content = ui["Unlock"];
        hint.Title = ui["HintTitle"];
        hintTitle.Text = ui["HintTitle"];
        hintShortcut.Text = ui["HintShortcut"];
        hintButton.Content = ui["Restore"];
        menu.Items[0].Text = ui["Open"];
        menu.Items[1].Text = ui["TrayToggle"];
        menu.Items[2].Text = ui["Unlock"];
        menu.Items[3].Text = ui["Exit"];
        ChineseButton.IsChecked = ui.Language != "en";
        EnglishButton.IsChecked = ui.Language == "en";
        SensitivityValue.Text = $"{settings.Sensitivity} / 10";
        Refresh(snapshot);
    }

    private void OnLanguageClicked(object sender, RoutedEventArgs e)
    {
        if (initializing || sender is not System.Windows.Controls.RadioButton item || item.Tag is not string language || language == ui.Language) return;
        SwitchLanguage(language);
    }

    private void OnOptionsChanged(object sender, RoutedEventArgs e) => ApplyOptions();
    private void OnOptionsChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ApplyOptions();

    private void OnStartupChanged(object sender, RoutedEventArgs e)
    {
        if (initializing) return;
        try { if (persist) UserSettings.SetStartup(StartupOption.IsChecked == true); ApplyOptions(); }
        catch (Exception ex)
        {
            StartupOption.IsChecked = settings.StartWithWindows;
            ErrorText.Text = ui.Format("StartupFailed", ex.Message);
        }
    }

    private void ApplyOptions()
    {
        if (initializing || exiting) return;
        settings.Enabled = EnabledOption.IsChecked == true;
        settings.WarnOnly = WarnOption.IsChecked == true;
        settings.StayLocked = StayOption.IsChecked == true;
        settings.Sound = SoundOption.IsChecked == true;
        settings.StartWithWindows = StartupOption.IsChecked == true;
        settings.Sensitivity = (int)SensitivitySlider.Value;
        SensitivityValue.Text = $"{settings.Sensitivity} / 10";
        ErrorText.Text = "";
        hook.Configure(settings);
        SaveSettings();
    }

    private void SaveSettings()
    {
        if (!persist) return;
        try { settings.Save(); }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        { ErrorText.Text = ui.Format("SaveFailed", ex.Message); }
    }

    private void Open() { Show(); WindowState = WindowState.Normal; Activate(); }
    private void OnToggle(object sender, RoutedEventArgs e) => hook.Toggle();
    private void OnUnlock(object sender, RoutedEventArgs e) => hook.Unlock();
    private void OnHide(object sender, RoutedEventArgs e) => Hide();
    private void OnExit(object sender, RoutedEventArgs e) => ExitApp();
    private void OnHookChanged(GuardSnapshot s) => Dispatcher.BeginInvoke(() => { if (!exiting) Refresh(s); });
    private void OnFailure(string message) => Dispatcher.BeginInvoke(() =>
    {
        if (exiting) return;
        settings.Enabled = false;
        EnabledOption.IsChecked = false;
        ErrorText.Text = ui[message];
        Refresh(hook.Snapshot);
    });
    private void OnSession(object sender, SessionSwitchEventArgs e) { if (!exiting) hook.Suspend(); }
    private void OnPower(object sender, PowerModeChangedEventArgs e) { if (!exiting && e.Mode is PowerModes.Suspend or PowerModes.Resume) hook.Suspend(); }

    private void Refresh(GuardSnapshot s)
    {
        snapshot = s;
        bool locked = s.State is GuardState.AutoLocked or GuardState.ManualLocked;
        string stateKey = s.State.ToString();
        StatusText.Text = ui[stateKey];
        StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(locked ? "#554772" : s.State == GuardState.Watching ? "#275B55" : "#242735"));
        StatusDetailText.Text = ui[s.State switch
        {
            GuardState.Watching => "WatchingDetail",
            GuardState.AutoLocked when !settings.StayLocked => "AutoDetail",
            GuardState.AutoLocked or GuardState.ManualLocked => "PersistentDetail",
            _ => "OffDetail"
        }];
        ToggleButton.Content = ui[locked ? "Restore" : "Lock"];
        ToggleButton.IsEnabled = !settings.WarnOnly || locked;
        tray.Text = ("WindowsCatGuard · " + StatusText.Text)[..Math.Min(63, ("WindowsCatGuard · " + StatusText.Text).Length)];
        RefreshCounters(s);
        if (locked)
        {
            if (!hint.IsVisible)
            {
                var area = SystemParameters.WorkArea;
                hint.Left = area.Right - hint.Width - 20;
                hint.Top = area.Bottom - hint.Height - 20;
                hint.Show();
            }
            if (previous is not (GuardState.AutoLocked or GuardState.ManualLocked) && settings.Sound) System.Media.SystemSounds.Asterisk.Play();
        }
        else hint.Hide();
        previous = s.State;
    }

    private void RefreshCounters(GuardSnapshot s)
    {
        TriggersNumber.Text = s.Triggers.ToString();
        HeldNumber.Text = s.HeldCount.ToString();
        BlockedNumber.Text = s.BlockedEvents.ToString();
        PhysicalText.Text = ui.Format("PhysicalEvents", s.PhysicalEvents);
        InjectedText.Text = ui.Format("InjectedEvents", s.InjectedEvents);
        ReasonText.Text = ui.Format("LastReason", ui[s.Rule ?? "None"]);
    }

    private Window MakeHint()
    {
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(hintTitle);
        panel.Children.Add(hintShortcut);
        hintButton.Background = new SolidColorBrush(Color.FromRgb(232, 223, 243));
        hintButton.BorderThickness = new Thickness(0);
        hintButton.Click += (_, _) => hook.Unlock();
        panel.Children.Add(hintButton);
        var window = new Window
        {
            Width = 330, Height = 181, Content = panel, Background = new SolidColorBrush(Color.FromRgb(248, 247, 252)), Icon = Icon,
            Topmost = true, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.ToolWindow,
            ResizeMode = ResizeMode.NoResize, FontFamily = FontFamily, Foreground = Foreground
        };
        window.Closing += (_, e) => { if (!exiting) { e.Cancel = true; hook.Unlock(); } };
        return window;
    }

    public void ExitApp()
    {
        if (exiting) return;
        exiting = true;
        statusTimer.Stop();
        hook.Changed -= OnHookChanged;
        hook.Failed -= OnFailure;
        SystemEvents.SessionSwitch -= OnSession;
        SystemEvents.PowerModeChanged -= OnPower;
        hook.Dispose();
        hint.Close();
        tray.Visible = false;
        tray.Icon?.Dispose();
        tray.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}
