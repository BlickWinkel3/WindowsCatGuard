using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CatGuard;
using CatGuard.Core;

namespace CatGuard.UiChecks;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 1) return 2;
        Directory.CreateDirectory(args[0]);
        var report = Path.Combine(args[0], "ui-check.txt");
        try
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            using var hook = new KeyboardHook();
            var options = new UserSettings { Enabled = false, Sensitivity = 5, Language = "zh-CN", AnimateMascot = true };
            var window = new GuardWindow(hook, options, persist: false);
            app.MainWindow = window;
            window.Loaded += async (_, _) =>
            {
                try
                {
                    await Task.Delay(500);
                    Check(window.CurrentLanguage == "zh-CN" && window.VisibleStatus == "防护已关闭", "Chinese status");
                    Check(window.VisibleSettingsTitle == "防护设置", "Chinese settings");
                    Capture(window, Path.Combine(args[0], "WindowsCatGuard-中文.png"));
                    var first = window.Motion;
                    await Task.Delay(700);
                    var second = window.Motion;
                    Check(Math.Abs(first.Tail - second.Tail) > 0.05 || Math.Abs(first.Breath - second.Breath) > 0.001, "vector animation advances");
                    window.SwitchLanguage("en", false);
                    await Task.Delay(200);
                    Check(window.CurrentLanguage == "en" && window.VisibleStatus == "Guard is off", "English status");
                    Check(window.VisibleSettingsTitle == "Protection settings", "English settings");
                    Check(window.ViewState == (GuardState.Off, false, true), "language switch preserves protection");
                    Capture(window, Path.Combine(args[0], "WindowsCatGuard-English.png"));
                    window.SetAnimation(false, false);
                    var stopped = window.Motion;
                    await Task.Delay(300);
                    Check(window.Motion == stopped, "animation switch stops motion");
                    Check(window.ViewState == (GuardState.Off, false, false), "animation switch preserves protection");
                    File.WriteAllText(report, $"PASS; Chinese and English text; state preserved; animation advanced and stopped; screenshots captured; HookInstalled={hook.Snapshot.HookInstalled}");
                }
                catch (Exception ex) { File.WriteAllText(report, "FAIL: " + ex); }
                finally { window.ExitApp(); }
            };
            window.Show();
            app.Run();
            return File.ReadAllText(report).StartsWith("PASS") ? 0 : 1;
        }
        catch (Exception ex) { File.WriteAllText(report, "FAIL: " + ex); return 1; }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
    }
    private static void Capture(Window window, string path)
    {
        window.UpdateLayout();
        var visual = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
