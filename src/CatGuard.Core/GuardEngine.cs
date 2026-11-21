using CatGuard.Core.Detection;

namespace CatGuard.Core;

public enum GuardState { Off, Watching, AutoLocked, ManualLocked }

// All methods run on the input thread. No input text, key history, or window titles are persisted.
public sealed class GuardEngine
{
    private readonly HashSet<int> physical = [];
    private readonly HashSet<int> forwarded = [];
    private readonly HashSet<int> suppressed = [];
    private KeyboardCatDetector detector = new(SensitivityProfile.FromLevel(5));
    private long lastActivity;
    private long coolUntil;
    private bool drain;
    private bool hotkeyDown;
    public GuardState State { get; private set; } = GuardState.Off;
    public bool Locked => State is GuardState.AutoLocked or GuardState.ManualLocked;
    public bool Enabled { get; private set; }
    public bool WarnOnly { get; private set; }
    public bool StayLocked { get; private set; }
    public long TriggerCount { get; private set; }
    public CatRule? LastRule { get; private set; }
    public int HeldCount => physical.Count;
    public event Action? Changed;

    // GetAsyncKeyState is an OS logical state, which can contain stale/aliased keys.
    // Preserve keyUp pairing, but never infer physical holds or gate detection from it.
    public void SeedForwarded(int vk) => forwarded.Add(vk);

    public void Configure(bool enabled, int sensitivity, bool warnOnly, bool stayLocked, long now)
    {
        Enabled = enabled;
        WarnOnly = warnOnly;
        StayLocked = stayLocked;
        detector = new(SensitivityProfile.FromLevel(Math.Clamp(sensitivity, 1, 10)));
        if (!enabled || (warnOnly && Locked)) SetState(enabled ? GuardState.Watching : GuardState.Off, now);
        else if (!Locked) SetState(GuardState.Watching, now);
    }

    public void Toggle(long now)
    {
        if (Locked) Unlock(now);
        else if (!WarnOnly) SetState(GuardState.ManualLocked, now);
    }

    public void Unlock(long now) => SetState(Enabled ? GuardState.Watching : GuardState.Off, now);

    // True means swallow. Forwarded keyDowns always receive their matching keyUp.
    // Suppressed keyDowns keep their keyUp suppressed even after the lock is lifted.
    public bool Handle(int vk, bool down, long now, bool injected = false)
    {
        if (injected) return false;
        bool repeat = down && physical.Contains(vk);
        lastActivity = now;
        if (down) physical.Add(vk); else physical.Remove(vk);

        bool control = physical.Contains(0xA2) || physical.Contains(0xA3) || physical.Contains(0x11);
        bool alt = physical.Contains(0xA4) || physical.Contains(0xA5) || physical.Contains(0x12);
        if (vk == 0x4C && down && (hotkeyDown || (control && alt)))
        {
            if (!hotkeyDown) { hotkeyDown = true; Toggle(now); }
            suppressed.Add(vk);
            return true;
        }
        if (vk == 0x4C && !down && hotkeyDown) hotkeyDown = false;

        if (!down)
        {
            bool swallow = suppressed.Remove(vk);
            bool sent = forwarded.Remove(vk);
            if (drain && physical.Count == 0) drain = false;
            if (!drain && !Locked) detector.Observe(new(vk, false, now, false, control || alt));
            return !sent && swallow;
        }

        if (Enabled && !Locked && !drain && now >= coolUntil && !repeat)
            Trigger(detector.Observe(new(vk, true, now, false, control || alt)), now);

        if (Locked || suppressed.Contains(vk)) { suppressed.Add(vk); return true; }
        forwarded.Add(vk);
        return false;
    }

    public void Poll(long now)
    {
        if (State == GuardState.AutoLocked && !StayLocked && physical.Count == 0 && now - lastActivity >= 1000)
            Unlock(now);
        else if (State == GuardState.Watching && Enabled && !drain && now >= coolUntil)
            Trigger(detector.Poll(now), now);
    }

    private void Trigger(CatSignal? signal, long now)
    {
        if (signal is null) return;
        LastRule = signal.Rule;
        TriggerCount++;
        if (WarnOnly)
        {
            detector.Reset();
            drain = physical.Count != 0;
            coolUntil = now + 1500;
            Changed?.Invoke();
        }
        else SetState(GuardState.AutoLocked, now);
    }

    private void SetState(GuardState state, long now)
    {
        State = state;
        detector.Reset();
        drain = physical.Count != 0;
        coolUntil = now + 300;
        Changed?.Invoke();
    }

    // Fail open after an input error or desktop/session change. Disarm until the user enables again.
    public void FailOpen(long now)
    {
        Enabled = false;
        physical.Clear();
        forwarded.Clear();
        suppressed.Clear();
        drain = false;
        hotkeyDown = false;
        SetState(GuardState.Off, now);
    }
}
