# CLAUDE.md

Guidance for AI coding agents working in this repository.

## What this is

Pomodoro Timer: a single-project Windows tray application that tracks
task / break / long break / meeting / lunch time, shows a small always-on-top
timer window (or a full screen timer during breaks), keeps daily statistics and
updates the Microsoft Teams status automatically.

- C# / WinForms, .NET Framework **4.8**, `LangVersion` 7.3
- Solution: `PomodoroTimer.sln`, single project `PomodoroTimer.csproj`
- Root namespace and **assembly name are `DEMAgentProcess`** (the executable is
  `DEMAgentProcess.exe` and the main window title is `DEMAgentProcess.exe`, on
  purpose, so the app is discreet in the process list)
- Dependencies come from `packages.config` + the checked in `packages\` folder
  (EPPlus for the XLSX report). There is no NuGet restore step in a clean build
  as long as `packages\` is present.

## Build

```powershell
# MSBuild path (Visual Studio 2026 Enterprise on this machine)
& "C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe" `
  PomodoroTimer.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" /v:minimal /nologo
```

- Output: `bin\Release\DEMAgentProcess.exe` (Debug goes to `bin\Debug\`)
- There are no unit tests. Verification = build clean + manual run.
- Locate MSBuild on another machine with
  `vswhere.exe -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"`.

## Versioning

The version lives only in `Properties\AssemblyInfo.cs`
(`AssemblyVersion` + `AssemblyFileVersion`, keep both in sync). It is shown in
the tray menu Exit item as `v{Major}.{Minor}`. Bump the minor for fixes and the
major for behaviour changes, and always rebuild Release after bumping.

## Files

| File | Responsibility |
| --- | --- |
| `Program.cs` | Entry point, `Application.Run(new MainForm())` |
| `MainForm.cs` | Everything: countdown, tray icon and menu, statuses, statistics, hotkey, keep awake, Teams automation, full screen orchestration |
| `ToolTipForm.cs` | The timer window: small draggable timer **and** full screen break window |
| `ConfigurationForm.cs` | Task/break duration, full screen break, keep awake |
| `MetricsReportManager.cs` | Daily metrics CSV in `%AppData%\PomodoroTimer` + XLSX report on the Desktop |
| `Properties\Settings.settings` | User settings (durations, timer position, flags) |

`MainForm` has no `.Designer.cs`; its `InitializeComponent` is hand written at
the bottom of `MainForm.cs` inside a `#region Design`. Same for
`ConfigurationForm`. Only `ToolTipForm` has a real designer file.

## How it works

**Statuses** (`TimerStatus`): `Task`, `Meeting`, `Break`, `LongBreak`, `Lunch`.
A `System.Windows.Forms.Timer` ticks every second: it counts *down* for every
status except `Meeting`, which counts *up*; `Lunch` and `LongBreak` also start
at 0 and count up (they are endless until the user exits them). Reaching 0
switches Task -> Break -> Task. A break duration of 0 disables breaks entirely.

**Tray menu** shortcuts: `0` pause/continue, `1` task, `2` meeting, `3` break,
`4` long break, `5` lunch, `6` keep awake. `ALT+F12` opens the menu globally
(`RegisterHotKey` + `WM_HOTKEY` in `WndProc`). Left clicking the tray icon
toggles between the small timer window and the tray tooltip text.

**Full screen rules** (`MainForm.UpdateFullScreenStatus`) - these are the rules
to preserve, they were the source of several bugs:

- Full screen is used for `Lunch` and `LongBreak` always, and for `Break` when
  the `BreakFullScreen` setting is on.
- **Every connected monitor must show the break.** `_toolTipForm1` covers the
  screen holding the small timer (`ToolTipForm.HomeScreen`) and
  `_extraFullScreenForms` gets one extra window per remaining screen. Never
  assume two monitors and never match screens by the `Primary` flag.
- The target screen is always passed explicitly to
  `ToolTipForm.SetFullScreen` and applied with `Bounds = screen.Bounds` plus
  `FormBorderStyle.None`, so the break also covers the taskbar. Do not use
  `WindowState.Maximized` (Windows then picks the monitor) and do not use
  `WorkingArea` (it leaves the taskbar uncovered). The border style is restored
  to `FixedToolWindow` when going back to the small timer.
- Screens are always recomputed from the current small timer position, never
  cached in a field: a cached `Screen` goes stale when a monitor is
  added/removed or the resolution changes.
- A full screen window must stay visible even when the user hid the small timer
  (`_hideToolTipTimer`). Visibility goes through `MainForm.ApplyTimerVisibility`.
- Full screen windows are not draggable and never save their position; only the
  small timer position is persisted (`ToolTipFormLocation`).
- `WM_DISPLAYCHANGE` re-applies the full screen windows after ~1.5s (the screen
  list is not reliable immediately after a display change).

**Teams status** (`SetTeamStatus`) drives the tray icon of `ms-teams` through
UI Automation plus synthetic mouse clicks at hardcoded pixel offsets. It is
fragile by nature: it silently gives up when the process or the tray icon is
not found. Icon names change between Teams versions, hence the list of
fallback names in `GetTeamsTrayIcon`.

**Metrics**: `%AppData%\PomodoroTimer\pomodoro_timer_report.csv` is the source
of truth (one row per day, both formatted and raw second columns); the Desktop
`pomodoro_timer_report.xlsx` is regenerated from it with EPPlus
(`LicenseContext.NonCommercial`). Metrics are loaded at startup for today and
saved when Exit is clicked, so killing the process loses the session.

## Conventions

- Keep the existing file header comment block when creating a new file.
- Code is organised in `#region` blocks; keep new members in the matching region.
- Explanatory comments above non obvious blocks are the norm here - keep them,
  and keep them explaining *why*.
- Private fields are `_camelCase`, `this.` is used in designer style code.
- No async/await for UI flow, everything is `System.Windows.Forms.Timer` based;
  `Task.Run` is only used for the beep melody.
- The app is DPI aware (`app.manifest`), so screen coordinates are real pixels
  in the system DPI. Center controls using `ClientSize`, not screen bounds.

## Gotchas

- **`App.config` user settings are dead.** The generated settings class is
  `DEMAgentProcess.Properties.Settings`, but the `userSettings` section in
  `App.config` is named `PomodoroTimer.Properties.Settings` (from
  `GeneratedClassNamespace` in `Settings.settings`). The runtime therefore
  falls back to the `DefaultSettingValue` attributes in
  `Properties\Settings.Designer.cs` (50min task / 10min break), and real values
  come from the per user `user.config`. Change defaults in `Settings.settings`
  and regenerate, not in `App.config`.
- Do not set `ToolTipForm.Size` directly from outside; use the `NormalSize`
  property, which is ignored while the window is full screen (otherwise the
  per second timer text resizing shrinks the full screen window).
- `Application.OpenForms["MainForm"]` is used by `ToolTipForm` to call back
  into `MainForm`; keep the form `Name` as `MainForm`.
- Several `ToolTipForm` instances exist at runtime and all of them read the same
  saved location at construction. Only `_toolTipForm1` is the real timer.
- The tray icons are embedded resources loaded by name
  (`DEMAgentProcess.Resources.greenball.ico` / `redball.ico`).

## Manual verification checklist for timer/full screen changes

1. Break starts full screen on **all** monitors, covering the taskbar, with the
   small timer hidden or visible.
2. Small timer moved to the secondary monitor, then a break: still all monitors.
3. `Exit` and `+5 Minutes` buttons appear once per screen and work.
4. `ESC` exits the full screen break.
5. Lunch -> long break -> task transitions leave no leftover window or button.
6. Unplug/replug a monitor during a break: the new screen gets covered.
7. Statistics in the menu keep counting and the daily XLSX is still written.
