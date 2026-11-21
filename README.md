# WindowsCatGuard

**English** · [中文说明](#中文说明)

A compact Windows 10/11 x64 keyboard guard that pauses input when a cat steps on adjacent keys. Built with C#, .NET 10 and WPF. Version 0.3.0 has a switchable English/Chinese interface, a small single-column window and a paw icon. The app runs locally and stays in the system tray.

Inspired by [CatSit](https://github.com/glvckoma/CatSit)'s detection rules and Windows hook structure, and [PawPause](https://github.com/miladsafarzadeh1/PawPause-mac)'s release-after-quiet behavior. See [third-party notices](THIRD_PARTY_NOTICES.md) and the included MIT licenses.

## Use

1. Launch `WindowsCatGuard.exe`. Automatic protection starts **off** each time. The mouse remains available.
2. Select **Lock keyboard** for a manual lock, or enable **Automatic protection** to detect adjacent key clusters, bursts and sustained holds.
3. Press **Ctrl+Alt+L** to toggle the lock globally. You can also unlock from the main window, floating hint or tray menu.
4. An automatic lock ends after every key is released and 1 second of quiet, unless **Keep automatic locks until I unlock** is on. A manual lock always requires an explicit unlock.
5. Lower sensitivity if ordinary typing causes false alarms, or use **Notify only** first. Closing the main window hides it in the tray; **Exit and restore input** quits.

Choose **中文** or **EN** at the top right. The choice is saved without restarting protection. Less-used options are under **More settings**. The [visual design guide](docs/视觉规范.md) records the typography, layout, color and outline rules from the supplied reference.

To test automatic detection with a physical keyboard, enable it and hold A, S, D and F together. Typing those letters one after another is a different pattern.

`Ctrl+Alt+L` is reserved and consumed by the app. A few keystrokes may reach the focused app before detection. WindowsCatGuard does not undo them. This user-level hook is not a security lock screen and cannot guarantee coverage of elevated apps, games, remote sessions or the secure desktop. Windows still handles `Ctrl+Alt+Delete`.

In notify-only mode, the button and hotkey do not start new locks. A session or power transition, or an input failure, turns protection off until you enable it again. The keyboard hook remains installed while the app runs so the global manual hotkey works; exiting removes it.

## Build and checks

Requires Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). There are no third-party NuGet packages.

```powershell
dotnet run --project tests/CatGuard.Checks -c Release
dotnet build tests/CatGuard.NativeChecks -c Release
dotnet build tests/CatGuard.UiChecks -c Release
dotnet run --project src/CatGuard -c Release
```

Run `./build.ps1` to publish a self-contained portable folder at `dist/WindowsCatGuard-win-x64/`. Copy the whole folder. If you move it after enabling Windows startup, turn that setting off and back on. The executable is unsigned.

`tests/CatGuard.NativeChecks` uses a real Windows low-level hook and a separate receiving window. `--auto` sends specially marked test input through the native hook to check delivery before, during and after a lock. That marker handling is compiled only into the test executable. The test stops if its window loses focus. `--physical` runs the [physical-key acceptance procedure](docs/实机验收.md). `tests/CatGuard.UiChecks` checks the compact window, paw icon, both languages and state preservation during a language switch.

## Privacy and implementation

The app itself makes no network requests and does not save typed text, key sequences or window titles. Settings live in `%APPDATA%\WindowsCatGuard\settings.json`; trigger counts and reasons exist only in memory. Windows startup modifies only the current user's Run registry entry. No service or driver is installed.

`src/CatGuard.Core` contains detection and the input state machine. `src/CatGuard` contains the input thread, WPF UI, tray and settings. The [implementation notes](docs/实施与验证.md) and [0.1.1 fix record](docs/0.1.1修复记录.md) describe validation of the automatic trigger. State-machine checks do not replace compatibility testing for every physical keyboard, IME, game or layout. The adjacency map follows the upstream US/QWERTY layout.

MIT License. Keep `LICENSE`, `THIRD_PARTY_NOTICES.md` and `licenses/` with redistributed copies.

---

## 中文说明

WindowsCatGuard 是面向 Windows 10/11 x64 的防猫踩键盘工具，使用 C#、.NET 10 与 WPF 开发，常驻托盘。0.3.0 改为精简的单栏小窗口，保留中英切换，统一使用猫爪图标。程序本地运行。

检测规则与 Windows 钩子结构参考 [CatSit](https://github.com/glvckoma/CatSit)，松键后自动恢复的思路参考 [PawPause](https://github.com/miladsafarzadeh1/PawPause-mac)。来源和 MIT 许可证见[第三方声明](THIRD_PARTY_NOTICES.md)。

### 使用

1. 启动 `WindowsCatGuard.exe`。每次启动自动防护默认关闭，鼠标始终可用。
2. 点击“立即锁定键盘”手动锁键，或开启“自动防护”，检测邻近多键、密集乱按和持续压键。
3. 按 **Ctrl+Alt+L** 全局切换锁定；也可以从主窗口、悬浮提示或托盘解锁。
4. 自动触发后，全部按键松开并安静 1 秒会恢复；开启“自动触发后保持锁定”则需主动解锁。手动锁定始终要主动解除。
5. 误触较多时降低灵敏度，或先用“仅提示”。关闭主窗口会收起到托盘；选择“退出并恢复输入”才会退出。

窗口右上角可切换 **中文 / EN**，选择会保存，切换不重置防护状态。较少使用的选项位于“更多设置”。[视觉规范](docs/视觉规范.md)记录了参考图的字体、排版、颜色和描边规则。

实体键盘测试可在开启自动防护后同时按住 A、S、D、F。依次输入普通字符不等同于多键踩压。

`Ctrl+Alt+L` 是程序消费的保留操作键。自动检测触发前，少量输入可能已进入应用，程序不会撤销它们。用户态钩子不是安全锁屏，不保证覆盖管理员应用、游戏、远程会话或安全桌面；`Ctrl+Alt+Delete` 仍由 Windows 处理。

“仅提示”模式下按钮和快捷键不会新建锁定。会话、电源状态切换或输入异常后自动防护会关闭，需要手动重新启用。程序运行时保留键盘钩子以支持全局手动快捷键，退出后卸载。

### 构建与验证

需要 Windows 和 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)，无第三方 NuGet 包。

```powershell
dotnet run --project tests/CatGuard.Checks -c Release
dotnet build tests/CatGuard.NativeChecks -c Release
dotnet build tests/CatGuard.UiChecks -c Release
dotnet run --project src/CatGuard -c Release
```

运行 `./build.ps1` 会构建无需另装 .NET 的便携目录 `dist/WindowsCatGuard-win-x64/`。复制整个目录使用。移动目录后，若已开启开机启动，请关闭再重新开启。当前程序未签名。

`tests/CatGuard.NativeChecks` 使用真实 Windows 底层钩子和独立输入窗口。`--auto` 把带测试标记的模拟事件送入原生钩子，检查锁定前、锁定中和恢复后的输入投递；标记识别仅编译进测试程序，焦点离开测试窗就停止。`--physical` 用于[实体键盘验收](docs/实机验收.md)。`tests/CatGuard.UiChecks` 检查紧凑窗口、猫爪图标、中英文界面以及切换语言时防护状态不变。

### 隐私与实现

程序本身不联网，不保存输入文本、按键序列或窗口标题。设置位于 `%APPDATA%\WindowsCatGuard\settings.json`；触发计数与原因只在内存中。开机启动只修改当前用户的 Run 注册表项，不安装服务或驱动。

`src/CatGuard.Core` 实现检测和输入状态机；`src/CatGuard` 实现输入线程、WPF 窗口、托盘和设置。详见[实施与验证](docs/实施与验证.md)和[0.1.1 修复记录](docs/0.1.1修复记录.md)。自动化状态机测试不能代替所有实体键盘、输入法、游戏和布局的兼容性测试；邻键表沿用上游 US/QWERTY 布局。

MIT License。再分发时保留 `LICENSE`、`THIRD_PARTY_NOTICES.md` 和 `licenses/`。
