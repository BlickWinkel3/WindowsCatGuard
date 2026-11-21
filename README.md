# WindowsCatGuard

Windows 防猫踩键盘工具，面向 Windows 10/11 x64。C# / .NET 10 / WPF，中文界面，托盘常驻。

当前版本 0.1.1：修复启动时将 Windows 残留逻辑按键误当作实体按住状态、导致自动检测一直等待的问题；修复 Ctrl/Alt 使其他所有按键都被排除检测的问题。窗口显示收到的按键事件、当前按住键数和拦截次数。

复用 [CatSit](https://github.com/glvckoma/CatSit) 的键盘检测规则与 Windows 钩子结构，借鉴 [PawPause](https://github.com/miladsafarzadeh1/PawPause-mac) 的松键后自动恢复方式。完整来源和许可证见 [第三方声明](THIRD_PARTY_NOTICES.md)。

## 使用

1. 启动 `WindowsCatGuard.exe`。每次启动自动防护默认关闭，鼠标始终可用。
2. 点击“立即锁定键盘”提前锁键，或勾选“启用自动防护”检测邻键同时按下、短时间乱按和持续压键。
3. 按 **Ctrl+Alt+L** 切换锁定，也可点击提示窗、主窗口或托盘的“解锁”。
4. 自动触发后，全部按键释放且安静 1 秒会恢复；“保持锁定”可关闭自动恢复。手动锁定始终需要人工解除。
5. 容易误触时降低灵敏度，或先使用“仅提示”模式。关闭主窗口会收起到托盘；选择“退出并恢复输入”才能退出。

`Ctrl+Alt+L` 作为保留操作键会被程序消费。自动模式触发前可能已有少量输入进入应用，不会自动删除这些输入。程序使用用户态钩子，不是安全锁屏，也不保证覆盖管理员应用、特殊游戏输入、远程会话及安全桌面。`Ctrl+Alt+Delete` 仍由 Windows 处理。

仅提示模式下按钮和快捷键不会新建锁定。电源或会话切换、输入处理异常后会关闭防护，需要手动重新启用。自动防护关闭时仍保留键盘钩子，以支持全局手动锁定快捷键；退出程序后卸载钩子。

## 构建

需要 Windows 和 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。无第三方 NuGet 依赖。

```powershell
dotnet build src/CatGuard/CatGuard.csproj -c Release
dotnet run --project tests/CatGuard.Checks -c Release
dotnet run --project src/CatGuard -c Release
```

启用自动防护后，可同时按住 A、S、D、F 检查触发；这是同时按住，逐个输入普通字符不等同于多键踩压。

发布无需另装 .NET 的便携包：

```powershell
./build.ps1
```

输出为 `dist/WindowsCatGuard-win-x64/`。复制整个目录使用；移动目录后，如已启用开机启动，请关闭再重新开启该选项。程序尚未签名。

## 隐私与存储

程序本身不联网，不保存输入文本、按键序列或窗口标题；设置保存在 `%APPDATA%\WindowsCatGuard\settings.json`。触发计数和原因仅存在当前进程中。开机启动选项只修改当前用户 Run 注册表项，不安装服务或驱动。

## 结构与验证

- `src/CatGuard.Core`：纯检测与状态机，物理按键、已放行按键、被屏蔽按键分别跟踪。
- `src/CatGuard`：独立输入线程、WPF 主窗口与解锁提示、托盘、JSON 设置。
- `tests/CatGuard.Checks`：无需测试框架的可执行断言；失败退出非零，覆盖检测、输入配对、解锁、恢复及特殊状态。
- `docs/设计.md`：实施前设计；`docs/实施与验证.md`：当前实现范围和验证记录。

自动化状态机测试不能替代真实键盘、输入法和游戏兼容性测试。US/QWERTY 邻键表沿用上游；其他布局的相邻判断需进一步适配。

## 本机输入验收

`tests/CatGuard.NativeChecks` 使用真实 Windows 键盘钩子和独立接收窗口。先构建：

```powershell
dotnet build tests/CatGuard.NativeChecks -c Release
./tests/CatGuard.NativeChecks/bin/Release/net10.0-windows/CatGuard.NativeChecks.exe ./native-check.txt --auto
```

`--auto` 将带测试标记的 SendInput 事件经原生钩子送入检测链路，确认接收控件在锁定前有输入、锁定后无输入、恢复后重新有输入；该标记识别只编译进测试程序，不包含在发布 EXE 中。焦点不在测试窗口时立即停止，不向其他应用发送测试按键。

改用 `--physical` 可进行实体键盘验收：点击开始，同时按住 A、S、D、F，按 G 后全部释放，看到恢复提示后按 H。记录只包含断言与计数，不保存输入文本；结束、失去焦点或超时会自动解除测试钩子。

MIT License。请保留 `LICENSE`、`THIRD_PARTY_NOTICES.md` 和 `licenses/`。
