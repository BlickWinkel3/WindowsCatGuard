using CatGuard.Core;

int checks = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); checks++; Console.WriteLine("PASS: " + name); }
GuardEngine Armed(bool warn = false, bool stay = false) { var e = new GuardEngine(); e.Configure(true, 5, warn, stay, 0); return e; }
void Paw(GuardEngine e, long time = 500) { e.Handle(0x41, true, time); e.Handle(0x53, true, time + 10); e.Handle(0x44, true, time + 20); e.Handle(0x46, true, time + 30); }
void ReleasePaw(GuardEngine e, long time) { foreach (int vk in new[] { 0x41, 0x53, 0x44, 0x46 }) e.Handle(vk, false, time++); }

var engine = Armed();
foreach (char ch in "HELLO WORLD") { Check(!engine.Handle(ch, true, 500), "normal typing down"); Check(!engine.Handle(ch, false, 520), "normal typing up"); }
Check(engine.State == GuardState.Watching, "normal typing stays unlocked");
Paw(engine); Check(engine.State == GuardState.AutoLocked, "adjacent paw cluster locks");
Check(!engine.Handle(0x41, false, 600), "pre-lock forwarded key receives keyUp");
Check(engine.Handle(0x46, false, 601), "triggering blocked key has blocked keyUp");
engine.Poll(10000); Check(engine.Locked, "held keys prevent auto release");
engine.Handle(0x53, false, 10001); engine.Handle(0x44, false, 10002);
engine.Poll(11001); Check(engine.Locked, "release silence counts keyUp");
engine.Poll(11002); Check(!engine.Locked, "quiet complete release auto unlocks");

engine = Armed(); Paw(engine); engine.Unlock(600);
Check(engine.Handle(0x46, true, 610), "blocked key repeat remains blocked after unlock");
Check(engine.Handle(0x46, false, 620), "blocked keyUp remains paired after unlock");
engine.Handle(0x51, true, 1000); Check(!engine.Locked, "old held keys prevent re-trigger after manual unlock");
ReleasePaw(engine, 1100); engine.Handle(0x51, false, 1105);
Paw(engine, 1500); Check(engine.Locked, "new paw is detected after complete drain");

engine = Armed(); engine.Toggle(500); Check(engine.State == GuardState.ManualLocked, "manual lock");
engine.Poll(100000); Check(engine.Locked, "manual lock never auto releases");
engine.Handle(0xA2, true, 100010); engine.Handle(0xA4, true, 100011);
Check(engine.Handle(0x4C, true, 100012) && !engine.Locked, "hotkey works while all keys blocked");
engine.Handle(0x4C, true, 100013); Check(!engine.Locked, "hotkey auto repeat does not re-lock");
Check(engine.Handle(0x4C, false, 100014), "hotkey keyUp consumed");
Check(engine.Handle(0xA2, false, 100015) && engine.Handle(0xA4, false, 100016), "blocked modifiers stay paired after unlock");

engine = Armed(true); Paw(engine); Check(!engine.Locked && engine.TriggerCount == 1, "warn-only records without blocking");
Check(!engine.Handle(0x47, true, 540), "warn-only passes events");
engine = Armed(false, true); Paw(engine); ReleasePaw(engine, 600); engine.Poll(10000); Check(engine.Locked, "stay-locked disables auto release");
engine.Configure(false, 5, false, false, 11000); Check(!engine.Locked && engine.State == GuardState.Off, "disable unlocks");
engine = Armed(); foreach (int vk in new[] { 0x41, 0x53, 0x44, 0x46 }) engine.Handle(vk, true, 500, true);
Check(!engine.Locked && engine.HeldCount == 0, "injected input is not scored");
engine.Handle(0x41, true, 500); for (int i = 0; i < 40; i++) engine.Handle(0x41, true, 501 + i);
engine.Poll(8000); Check(!engine.Locked, "single key long hold is not a paw");
engine = Armed(); engine.Handle(0x41, true, 500); engine.Handle(0x53, true, 510); engine.Poll(5000);
Check(engine.Locked, "sustained two-key hold is detected");
engine.FailOpen(6000); Check(engine.State == GuardState.Off && !engine.Enabled, "error recovery disarms");
Check(!engine.Handle(0x41, false, 6001), "error recovery passes subsequent keyUp");
engine = Armed(); Paw(engine); engine.Handle(0xA0, true, 540); ReleasePaw(engine, 600);
engine.Poll(5000); Check(engine.Locked, "held Shift prevents auto release");
engine.Handle(0xA0, false, 5010); engine.Poll(6009); Check(engine.Locked, "modifier release starts silence interval");
engine.Poll(6010); Check(!engine.Locked, "modifier release completes auto release");
engine = Armed(); engine.Handle(0xA2, true, 500); engine.Handle(0xA4, true, 510); engine.Handle(0x4C, true, 520);
Check(engine.State == GuardState.ManualLocked, "hotkey can lock from watching");
Check(!engine.Handle(0xA2, false, 530) && !engine.Handle(0xA4, false, 540), "pre-lock modifier keyUps remain forwarded");
engine = Armed(true); engine.Toggle(500); Check(!engine.Locked, "warn-only prevents manual lock too");
engine = Armed(); engine.SeedHeld(0x41, 0); engine.Handle(0x53, true, 500); engine.Handle(0x44, true, 501); engine.Handle(0x46, true, 502);
Check(!engine.Locked, "startup held keys drain before detection");
Console.WriteLine($"ALL {checks} CHECKS PASSED");
