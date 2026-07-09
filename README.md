# WinMove

Tiny Windows utility to move any window to any monitor — and close or kill windows.

Some apps and games open on the wrong monitor (or a virtual / off-screen one) and refuse to move with `Win`+`Shift`+`Arrow`. WinMove lists every open window and puts it where you want in one click. No install, no keyboard shortcuts.

## Use

Download `winmove.exe` and run it. Requires Windows 10/11 (.NET Framework 4, preinstalled).

- Select a window, click a **Display** button to send it there.
- **Maximize after move** fills the target display.
- **Close** sends `WM_CLOSE`; **Kill** force-terminates the process (with a confirm).
- **Refresh** re-scans. Windows sitting on an off-screen / virtual display are shown in orange.

## Build

```
csc /target:winexe /out:winmove.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll winmove.cs
```

`csc.exe` ships with the .NET Framework (`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\`), so no SDK is needed. Or just run `build.bat`.

## License

MIT
