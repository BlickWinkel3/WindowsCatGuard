using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace CatGuard;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        using var single = new Mutex(true, @"Local\WindowsCatGuard", out bool first);
        if (!first) { System.Windows.MessageBox.Show("WindowsCatGuard 已在运行，请查看任务栏托盘。", "防猫踩键盘"); return 0; }
        try
        {
            var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            using var hook = new KeyboardHook();
            var window = new GuardWindow(hook);
            app.MainWindow = window;
            if (!args.Contains("--tray")) window.Show();
            if (args.Contains("--smoke-test"))
            {
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    Console.WriteLine("Hook installed: " + hook.Snapshot.HookInstalled);
                    if (args.Contains("--screenshot"))
                    {
                        int shotIndex = Array.IndexOf(args, "--screenshot") + 1;
                        if (shotIndex < args.Length)
                        {
                            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                            bitmap.Render(window);
                            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                            using var stream = File.Create(args[shotIndex]);
                            encoder.Save(stream);
                        }
                    }
                    if (args.Contains("--report"))
                    {
                        int index = Array.IndexOf(args, "--report") + 1;
                        if (index < args.Length) File.WriteAllText(args[index], $"HookInstalled={hook.Snapshot.HookInstalled}; State={hook.Snapshot.State}; UI=Loaded");
                    }
                    window.ExitApp();
                };
                timer.Start();
            }
            app.Run();
            return 0;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("无法启动防护，键盘未锁定。\n" + ex.Message, "WindowsCatGuard");
            return 1;
        }
    }
}
