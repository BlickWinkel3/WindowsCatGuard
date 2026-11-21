# Third party notices

WindowsCatGuard is distributed under MIT. The following original notices and
licenses are included in `licenses/` and must accompany redistributed copies.

## CatSit

- Repository: https://github.com/glvckoma/CatSit
- Source commit: `e5bed77cfd380e1f11a45c438760abbbc425d2f9`
- Copyright (c) 2026 glvckoma
- License: MIT, reproduced in `licenses/CatSit-MIT.txt`.
- Copied files: `src/CatGuard.Core/Detection/*.cs` and `src/CatGuard.Core/Input/KeyNames.cs`.
  Namespaces changed from `CatSit.Core` to `CatGuard.Core`; the detection window
  is capped at 128 events. Modifier keys no longer exclude all other keys from
  detection; common Ctrl/Alt shortcuts remain exempt from low-count hold triggers.
- `src/CatGuard/KeyboardHook.cs` adapts the native hook/thread structure of
  CatSit's `InputHookManager.cs`, `NativeMethods.cs` and `HookThread.cs`.
  Keyboard pairing, recovery, detection scheduling and UI integration differ.

## PawPause mac

- Repository: https://github.com/miladsafarzadeh1/PawPause-mac
- Source commit: `9dff60f6fe81bea4c4fcf91d4b0079124757eeb9`
- Copyright (c) 2026 Milad Safarzadeh
- License: MIT, reproduced in `licenses/PawPause-MIT.txt`.
- `src/CatGuard.Core/GuardEngine.cs` adapts the automatic-release and persistent-lock
  approach from `PawPause/CatDetector.swift` to C#. WindowsCatGuard tracks modifiers,
  measures silence from both keyDown and keyUp, and separates physical and forwarded
  keys. The six-signal weighted scoring algorithm is not used.

The user interface is implemented for WindowsCatGuard. No third-party image or
audio files are bundled; the cat symbol uses the system font.
