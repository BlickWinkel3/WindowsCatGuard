using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using CatGuard.Core;

namespace CatGuard;

// Adapted from CatSit InputHookManager / HookThread (MIT); see THIRD_PARTY_NOTICES.md.
internal sealed class KeyboardHook : IDisposable
{
    private delegate nint HookProc(int code, nint wParam, nint lParam);
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardData { public uint Vk, Scan, Flags, Time; public nuint Extra; }
    [DllImport("user32.dll", SetLastError = true)] private static extern nint SetWindowsHookExW(int type, HookProc proc, nint module, uint thread);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] private static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandleW(string? name);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vk);
    private readonly GuardEngine engine = new();
    private readonly Thread thread;
    private readonly ManualResetEventSlim ready = new();
    private Dispatcher dispatcher = null!;
    private DispatcherTimer timer = null!;
    private HookProc? callback;
    private nint handle;
    private Exception? startupError;
    private bool disposed;
    private bool notificationQueued;
    private long physicalEvents;
    private long injectedEvents;
    private long blockedEvents;
#if NATIVE_TEST
    public List<int> InitialKeys { get; } = [];
#endif
    public event Action<GuardSnapshot>? Changed;
    public event Action<string>? Failed;
    public GuardSnapshot Snapshot => dispatcher.Invoke(ReadSnapshot);

    public KeyboardHook()
    {
        thread = new Thread(Run) { IsBackground = true, Name = "CatGuard.Input" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
        if (startupError is not null) { thread.Join(); throw startupError; }
    }

    private void Run()
    {
        try
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            engine.Changed += QueueNotification;
            callback = Callback;
            handle = SetWindowsHookExW(13, callback, GetModuleHandleW(null), 0);
            if (handle == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to install the keyboard hook");
            // Preserve releases for keys held before launch without seeding physical state.
            for (int vk = 1; vk < 256; vk++)
                if (vk is not (1 or 2 or 4 or 5 or 6 or 0x10 or 0x11 or 0x12) && (GetAsyncKeyState(vk) & 0x8000) != 0)
                {
#if NATIVE_TEST
                    InitialKeys.Add(vk);
#endif
                    engine.SeedForwarded(vk);
                }
            timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Send,
                (_, _) => { try { engine.Poll(Environment.TickCount64); } catch { Disarm("DetectionError"); } }, dispatcher);
        }
        catch (Exception ex) { startupError = ex; }
        finally { ready.Set(); }
        try { if (startupError is null) Dispatcher.Run(); }
        finally { if (handle != 0) UnhookWindowsHookEx(handle); handle = 0; }
    }

    private nint Callback(int code, nint wParam, nint lParam)
    {
        if (code < 0) return CallNextHookEx(0, code, wParam, lParam);
        try
        {
            int msg = (int)wParam;
            if (msg is not (0x100 or 0x101 or 0x104 or 0x105)) return CallNextHookEx(0, code, wParam, lParam);
            var data = Marshal.PtrToStructure<KeyboardData>(lParam);
            bool injected = (data.Flags & 0x10) != 0;
            if (injected) injectedEvents++; else physicalEvents++;
#if NATIVE_TEST
            // Test assembly only: route marked SendInput through the physical-key path.
            // This branch is not compiled into WindowsCatGuard.exe.
            if (data.Extra == 0x43415454) injected = false;
#endif
            if (engine.Handle((int)data.Vk, msg is 0x100 or 0x104, Environment.TickCount64, injected)) { blockedEvents++; return 1; }
        }
        catch { Disarm("InputError"); }
        return CallNextHookEx(0, code, wParam, lParam);
    }

    // Coalesce status work outside the native callback; never wait for the UI here.
    private void QueueNotification()
    {
        if (notificationQueued) return;
        notificationQueued = true;
        dispatcher.BeginInvoke(() => { notificationQueued = false; Changed?.Invoke(ReadSnapshot()); });
    }
    private GuardSnapshot ReadSnapshot() => new(engine.State, engine.TriggerCount, engine.LastRule?.ToString(), handle != 0, physicalEvents, injectedEvents, blockedEvents, engine.HeldCount);
    public void Configure(UserSettings settings) => dispatcher.Invoke(() => engine.Configure(settings.Enabled, settings.Sensitivity, settings.WarnOnly, settings.StayLocked, Environment.TickCount64));
    public void Toggle() => dispatcher.Invoke(() => engine.Toggle(Environment.TickCount64));
    public void Unlock() => dispatcher.Invoke(() => engine.Unlock(Environment.TickCount64));
    public void Suspend() => dispatcher.Invoke(() => Disarm("SessionChanged"));
    private void Disarm(string message)
    {
        engine.FailOpen(Environment.TickCount64);
        dispatcher.BeginInvoke(() => Failed?.Invoke(message));
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        dispatcher.Invoke(() => { timer.Stop(); engine.FailOpen(Environment.TickCount64); dispatcher.BeginInvokeShutdown(DispatcherPriority.Send); });
        thread.Join();
        ready.Dispose();
    }
}

internal sealed record GuardSnapshot(GuardState State, long Triggers, string? Rule, bool HookInstalled, long PhysicalEvents, long InjectedEvents, long BlockedEvents, int HeldCount);
