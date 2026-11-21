using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.IO;
using CatGuard;
using CatGuard.Core;

internal static class Program
{
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public Keyboard Keyboard; [FieldOffset(0)] public Mouse Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct Keyboard { public ushort Vk, Scan; public uint Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct Mouse { public int X, Y; public uint Data, Flags, Time; public nuint Extra; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    private static Window window = null!;
    private static int seen;
    private static int failures;
    private static string report = "";
    private static void Log(string message) => File.AppendAllText(report, message + Environment.NewLine);
    private static void Send(ushort key, bool down)
    {
        if (GetForegroundWindow() != new WindowInteropHelper(window).Handle) throw new InvalidOperationException("Test window lost focus; no input sent.");
        var input = new Input { Type = 1, Data = new InputUnion { Keyboard = new Keyboard { Vk = key, Flags = down ? 0u : 2u, Extra = 0x43415454 } } };
        if (SendInput(1, [input], Marshal.SizeOf<Input>()) != 1) throw new InvalidOperationException("SendInput failed: " + Marshal.GetLastWin32Error());
    }
    private static void Key(ushort key) { Send(key, true); Send(key, false); }
    private static void Check(bool pass, string name) { Log((pass ? "PASS " : "FAIL ") + name); if (!pass) failures++; }
    [STAThread]
    private static int Main(string[] args)
    {
        report = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "native-check.txt");
        File.WriteAllText(report, "Native input checks started" + Environment.NewLine);
        var app = new Application();
        bool physicalTest = args.Contains("--physical");
        window = new Window { Title = physicalTest ? "WindowsCatGuard 实体键盘验收" : "CatGuard native input check", Width = 580, Height = 300, Topmost = true, WindowStartupLocation = WindowStartupLocation.CenterScreen };
        var box = new TextBox { AcceptsReturn = true, Text = "", FontSize = 18 };
        var start = new Button { Content = physicalTest ? "开始实体键盘验收" : "Start native input checks", Padding = new Thickness(15) };
        var instruction = new TextBlock { Text = physicalTest ? "点击开始后，同时按住 A、S、D、F，再按一下 G，然后全部松开。\n窗口提示恢复后按 H 完成。测试结束会自动退出。" : "Automatic native input checks", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12), FontSize = 16 };
        var panel = new StackPanel(); panel.Children.Add(instruction); panel.Children.Add(start); panel.Children.Add(box); window.Content = panel;
        var timeout = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMinutes(4) };
        timeout.Tick += (_, _) => { failures++; window.Close(); }; timeout.Start();
        box.PreviewKeyDown += (_, _) => seen++;
        start.Click += async (_, _) =>
        {
            start.IsEnabled = false; timeout.Stop();
            KeyboardHook? hook = null;
            try
            {
                window.Activate(); box.Focus(); await Task.Delay(400);
                hook = new KeyboardHook();
                Log("Initial keys: " + string.Join(",", hook.InitialKeys.Select(v => "0x" + v.ToString("X2"))));
                if (physicalTest)
                {
                    bool gotG = false, gotH = false, sawLocked = false;
                    box.PreviewKeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.G) gotG = true; if (e.Key == System.Windows.Input.Key.H) gotH = true; };
                    hook.Configure(new UserSettings { Enabled = true, Sensitivity = 10 });
                    await Task.Delay(400);
                    instruction.Text = "现在同时按住 A、S、D、F，再按 G，然后全部松开。";
                    long until = Environment.TickCount64 + 120000;
                    while (Environment.TickCount64 < until)
                    {
                        if (!window.IsActive) throw new InvalidOperationException("测试窗口失去焦点，已停止验收并解除锁定。");
                        var s = hook.Snapshot;
                        if (s.State == GuardState.AutoLocked) { sawLocked = true; instruction.Text = "已自动锁定。请按 G 验证拦截，然后松开所有按键。"; }
                        if (sawLocked && s.State == GuardState.Watching)
                        {
                            instruction.Text = "已自动恢复。现在按 H 完成验收。";
                            if (gotH)
                            {
                                Check(s.PhysicalEvents > 0, "actual non-injected hardware events received");
                                Check(s.Triggers > 0 && s.BlockedEvents > 0, "actual hardware paw triggered and suppressed input");
                                Check(!gotG, "G did not reach receiver during physical auto lock");
                                Check(gotH, "H reached receiver after physical auto release");
                                Log("Physical final: " + s);
                                return;
                            }
                        }
                        await Task.Delay(50);
                    }
                    throw new TimeoutException("实体键盘验收未完成，已自动解锁退出。");
                }
                Key(0x41); await Task.Delay(250);
                Log("Baseline: " + hook.Snapshot);
                Check(seen == 1 && box.Text.Length == 1, "unlocked input reaches real text control");
                int before = seen;
                hook.Toggle();
                Check(hook.Snapshot.State == GuardState.ManualLocked, "native hook engine is locked");
                Key(0x42); await Task.Delay(250);
                Log("Locked: " + hook.Snapshot);
                Check(seen == before && box.Text.Length == 1, "locked input does not reach real text control");
                hook.Unlock(); Key(0x43); await Task.Delay(250);
                Check(seen == before + 1, "unlock restores delivery");
                box.Clear();
                hook.Configure(new UserSettings { Enabled = true, Sensitivity = 10 });
                await Task.Delay(400);
                Send(0xA2, true); Send(0x41, true); Send(0x53, true); Send(0x44, true); Send(0x46, true);
                await Task.Delay(200);
                Check(hook.Snapshot.State == GuardState.AutoLocked, "Ctrl plus paw reaches AutoLocked via native hook");
                int blockedBefore = seen; Key(0x47); await Task.Delay(200);
                Check(seen == blockedBefore, "automatic lock suppresses subsequent native input");
                foreach (ushort key in new ushort[] { 0xA2, 0x41, 0x53, 0x44, 0x46 }) Send(key, false);
                await Task.Delay(1300);
                Check(hook.Snapshot.State == GuardState.Watching, "native auto release after quiet keyUp");
                before = seen; Key(0x48); await Task.Delay(200);
                Check(seen == before + 1, "input reaches receiver after automatic release");
                Log("Final: " + hook.Snapshot);
            }
            catch (Exception ex) { Log(ex.ToString()); failures++; }
            finally { hook?.Dispose(); window.Close(); }
        };
        window.ContentRendered += (_, _) => { if (args.Contains("--auto")) start.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); };
        app.Run(window);
        Log("Native failures: " + failures);
        return failures == 0 ? 0 : 1;
    }
}
