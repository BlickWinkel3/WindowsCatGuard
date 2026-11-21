using System.Globalization;

namespace CatGuard;

internal sealed class UiText
{
    public string Language { get; private set; }
    public UiText(string language) => Language = Normalize(language);
    public static string Normalize(string? language) => language == "en" ? "en" : "zh-CN";
    public void SetLanguage(string language) => Language = Normalize(language);
    public string this[string key] => All.TryGetValue(key, out var pair) ? (Language == "en" ? pair.En : pair.Zh) : key;
    public string Format(string key, params object[] args) => string.Format(Language == "en" ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("zh-CN"), this[key], args);
    public static readonly Dictionary<string, (string Zh, string En)> All = new()
    {
        ["WindowTitle"] = ("WindowsCatGuard · 防猫踩键盘", "WindowsCatGuard · Keyboard companion"),
        ["Tagline"] = ("防猫踩键盘", "Paw-proof keyboard"),
        ["Protection"] = ("键盘防护", "KEYBOARD GUARD"),
        ["Off"] = ("防护已关闭", "Guard is off"),
        ["Watching"] = ("正在安静守护", "Quietly watching"),
        ["AutoLocked"] = ("猫咪来访，键盘已暂停", "Paws detected. Keyboard paused."),
        ["ManualLocked"] = ("键盘已手动锁定", "Keyboard is locked"),
        ["OffDetail"] = ("开启自动防护，或提前手动锁键。", "Turn on automatic protection, or lock ahead of time."),
        ["WatchingDetail"] = ("发现多键踩压后自动暂停输入。", "Pauses input when a paw-like key pattern appears."),
        ["AutoDetail"] = ("所有按键松开并安静 1 秒后恢复。", "Resumes after all keys are up and quiet for 1 second."),
        ["PersistentDetail"] = ("保持锁定，直到你主动解锁。", "Stays locked until you choose to unlock."),
        ["Lock"] = ("立即锁定键盘", "Lock keyboard"),
        ["Unlock"] = ("解锁", "Unlock"),
        ["Restore"] = ("恢复键盘输入", "Resume keyboard"),
        ["MouseAvailable"] = ("随时切换锁定，鼠标始终可用", "Toggle anytime. Your mouse stays available."),
        ["Triggers"] = ("本次触发", "Triggers"),
        ["Held"] = ("当前按住", "Keys held"),
        ["Blocked"] = ("已拦截事件", "Blocked events"),
        ["Settings"] = ("防护设置", "Protection settings"),
        ["MoreSettings"] = ("更多设置", "More settings"),
        ["AutoGuard"] = ("自动防护", "Automatic protection"),
        ["AutoGuardHint"] = ("检测邻近多键、密集乱按和持续踩压", "Detect clusters, key bursts and sustained holds"),
        ["WarnOnly"] = ("仅提示", "Notify only"),
        ["WarnOnlyHint"] = ("观察触发情况，不屏蔽键盘输入", "Review triggers without blocking your keyboard"),
        ["Sensitivity"] = ("检测灵敏度", "Sensitivity"),
        ["LessSensitive"] = ("减少误触", "Fewer false alarms"),
        ["MoreSensitive"] = ("更快响应", "Quicker response"),
        ["StayLocked"] = ("自动触发后保持锁定", "Keep automatic locks until I unlock"),
        ["Sound"] = ("锁定时播放提示音", "Play a sound when locking"),
        ["Startup"] = ("开机启动到托盘", "Start in the tray with Windows"),
        ["StartupHint"] = ("每次启动的自动防护初始关闭", "Automatic protection starts off each launch"),
        ["TestHint"] = ("测试：开启自动防护，再同时按住 A、S、D、F。", "Try it: enable protection, then hold A, S, D and F together."),
        ["LimitHint"] = ("检测触发前，少量输入可能已进入应用。", "A few keystrokes may pass before detection triggers."),
        ["Diagnostics"] = ("输入状态", "Input status"),
        ["PhysicalEvents"] = ("收到实体按键事件：{0}", "Physical key events received: {0}"),
        ["InjectedEvents"] = ("软件模拟事件：{0}（不用于自动检测）", "Simulated events: {0} (excluded from detection)"),
        ["LastReason"] = ("最近触发：{0}", "Last trigger: {0}"),
        ["PawCluster"] = ("邻近多键同时按下", "Adjacent keys pressed together"),
        ["MashBurst"] = ("短时间密集乱按", "Rapid burst of nearby keys"),
        ["SustainedHold"] = ("多键持续压住", "Several keys held down"),
        ["None"] = ("暂无", "None yet"),
        ["TrayNote"] = ("关闭窗口会收起到托盘", "Closing the window keeps the app in the tray"),
        ["Hide"] = ("收起到托盘", "Hide to tray"),
        ["Exit"] = ("退出并恢复输入", "Exit and restore input"),
        ["Open"] = ("打开主窗口", "Open window"),
        ["TrayToggle"] = ("锁定 / 解锁  Ctrl+Alt+L", "Lock / unlock  Ctrl+Alt+L"),
        ["HintTitle"] = ("键盘已暂停", "Keyboard paused"),
        ["HintShortcut"] = ("按 Ctrl+Alt+L 或点击下方恢复", "Press Ctrl+Alt+L or choose Resume"),
        ["Language"] = ("界面语言", "Interface language"),
        ["SaveFailed"] = ("设置暂未保存：{0}", "Settings could not be saved: {0}"),
        ["StartupFailed"] = ("开机启动设置失败：{0}", "Could not change startup setting: {0}"),
        ["InputError"] = ("输入处理异常，已停止防护。", "Input processing failed. Protection has been turned off."),
        ["DetectionError"] = ("检测异常，已停止防护。", "Detection failed. Protection has been turned off."),
        ["SessionChanged"] = ("会话或电源状态改变，防护已暂停，请手动重新启用。", "Session or power state changed. Turn protection on again when ready."),
        ["AlreadyRunning"] = ("WindowsCatGuard 已在运行，请查看任务栏托盘。", "WindowsCatGuard is already running. Look for it in the system tray."),
        ["LaunchFailed"] = ("无法启动防护，键盘未锁定。\n", "Protection could not start. Your keyboard is not locked.\n"),
        ["Version"] = ("版本 0.3.0", "Version 0.3.0"),
    };
}
