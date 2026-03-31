# StealthPane — Project Plan

## Overview
A cross-platform (Windows + macOS) desktop app that runs AI coding CLI tools (Claude Code, Codex, Gemini CLI, etc.) inside a terminal that is **invisible to screen sharing and screen capture**, but fully visible to the user. The window supports **adjustable opacity** and **always-on-top** mode, letting you overlay it on top of your IDE or browser and see through to the content beneath.

## Tech Stack
- **.NET 10 AOT** — runtime, cross-platform support, native performance, single executable application, no dependencies required on user machines
- **Avalonia UI 11** — cross-platform UI framework
- **NativeWebView** — embedded browser for terminal rendering https://docs.avaloniaui.net/controls/web/nativewebview
- **xterm.js** — terminal emulator running inside the WebView
- **Pty.Net / ConPTY / forkpty** — pseudo-terminal for full TUI support

## Core Behavior
- App launches the **configured CLI provider** on startup (default: Claude Code)
- Content protection is **always on** — window is invisible to all screen capture APIs
- Window supports **adjustable opacity** (see through to apps behind)
- Window supports **always-on-top** toggle via title bar button
- **Extensible provider system** — swap between Claude Code, Codex, Gemini CLI, etc.

---

## Architecture

### Data Flow
```
Keystroke → xterm.js → postMessage → C# WebView handler
  → PtyService.Write() → CLI process stdin
  → CLI process stdout → PtyService.OutputReceived
  → C# calls WebView.ExecuteScriptAsync("term.write(...)")
  → xterm.js renders output
```

### Content Protection (per-platform)
| Platform | API | Effect |
|----------|-----|--------|
| Windows  | `SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE)` | Window fully hidden from capture |
| macOS    | `NSWindow.SharingType = NSWindowSharingNone` | Window excluded from capture/recording |

### Window Opacity
| Platform | API |
|----------|-----|
| Windows  | `Window.Opacity` (Avalonia) or `SetLayeredWindowAttributes` for fine control |
| macOS    | `NSWindow.AlphaValue` via Avalonia's `Window.Opacity` |

Range: **10% – 100%**, controlled via slider in Settings. Persisted across sessions.

### Always-on-Top
| Platform | API |
|----------|-----|
| Both     | `Window.Topmost = true/false` (Avalonia built-in) |

Toggled via a **pin button [📌] in the title bar**. Visual indicator shows current state.

---

## CLI Provider Abstraction

The app doesn't hardcode any specific CLI tool. Providers are defined as configuration:

```csharp
public interface ICliProvider
{
    string Name { get; }                // "Claude Code", "Codex", "Gemini CLI"
    string Command { get; }             // "claude", "codex", "gemini"
    string[] Args { get; }              // optional launch arguments
    bool SupportsImageInput { get; }    // can it accept screenshot file refs?
    ImageInputMode ImageMode { get; }   // FilePath, Base64, StdinPipe
    string DefaultSystemPrompt { get; } // default prompt for screenshot injection
}

public enum ImageInputMode { FilePath, Base64, StdinPipe }
```

### Built-in Providers (shipped with app)
| Provider | Command | Image Support | Notes |
|----------|---------|---------------|-------|
| Claude Code | `claude` | Yes (file path) | Default provider |
| Codex | `codex` | TBD | Added when Codex CLI stabilizes |
| Gemini CLI | `gemini` | TBD | Added when available |
| Custom | user-defined | configurable | For any CLI tool |

### Provider Settings
- **Active provider** dropdown in Settings
- **Custom provider** form: name, command, args, image support toggle
- Provider is passed to `PtyService` which spawns the configured command
- Switching providers restarts the terminal session

---

## Feature: Screenshot Capture → CLI

### Summary
Press a hotkey → capture a screenshot → save to temp file → inject a prompt with the file reference into the running CLI session, along with a configurable system prompt.

### Flow
```
User presses hotkey (e.g. Ctrl+Shift+C)
  → ScreenCaptureService captures based on configured mode
  → Screenshot saved to temp file: /tmp/stealthpane/capture_1711800000.png
  → Prompt assembled:
      "[system prompt]\n\nSee the screenshot: /tmp/stealthpane/capture_1711800000.png"
  → Prompt pasted into PTY stdin (as if user typed it)
  → CLI processes the image + prompt
  → Response renders in xterm.js as usual
```

### Capture Modes (configurable in Settings)
| Mode | Description | Implementation |
|------|-------------|----------------|
| **Full screen** | Captures entire primary monitor | `Graphics.CopyFromScreen` (Win) / `CGWindowListCreateImage` (macOS) |
| **Region** | User pre-defines a screen rectangle in settings | Crop from full screen capture |
| **Specific window** | User picks a window from a list in settings | `PrintWindow` (Win) / `CGWindowListCreateImage` with windowID (macOS) |
| **Interactive region** | Crosshair selector appears on each capture | Overlay window with drag-to-select |

Default mode: **Full screen**

### System Prompt
- A **default prompt** ships with each provider, e.g.:
  ```
  Analyze the following screenshot. Describe what you see and suggest
  actions or code changes based on the content.
  ```
- User can **edit the prompt** in the Settings panel
- The prompt is prepended to every capture injection
- Each provider can have its own default prompt
- Stored in app settings (persisted to disk)

---

## Feature: Window Opacity

### Behavior
- Opacity range: **10% to 100%** (100% = fully opaque)
- Controlled via a **slider in Settings**
- Value persisted across sessions
- The terminal text, title bar, and all UI elements become translucent together
- Allows user to read content behind StealthPane (IDE, browser, docs)

### Use Case
```
┌─ StealthPane (60% opacity, always-on-top) ──┐
│                                              │
│  claude> Here's how to fix that bug...       │
│  ┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄   │
│  ░░░ IDE code visible behind ░░░░░░░░░░░░   │
│  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   │
│                                              │
└──────────────────────────────────────────────┘
```

---

## Feature: Always-on-Top

### Behavior
- Toggled via a **pin button [📌]** in the title bar
- When active: window stays above all other windows
- When inactive: normal window stacking
- Button visually indicates state (highlighted when pinned)
- State persisted across sessions

---

## Settings Panel

A flyout panel (triggered by ⚙ button in title bar) with sections:

### General
- **CLI Provider** dropdown: Claude Code / Codex / Gemini CLI / Custom
- **Custom provider config**: command, args (shown when "Custom" selected)
- **Window opacity** slider: 10% – 100%

### Screenshot Capture
- **Capture mode** dropdown: Full screen / Region / Window / Interactive
- **Region coordinates** (if Region mode): X, Y, Width, Height + "Select region" button
- **Window picker** (if Window mode): dropdown of open windows
- **Capture hotkey**: configurable, default `Ctrl+Shift+C`
- **System prompt**: multiline text editor with reset-to-default button
- **Temp directory**: where screenshots are saved (default: OS temp)
- **Auto-cleanup**: delete screenshots older than N minutes (default: 30)

---

## UI Layout

```
┌───────────────────────────────────────────────────────┐
│ [●] STEALTHPANE  [▼ Claude Code]  ● HIDDEN  [📌] [⚙] │  ← title bar
├───────────────────────────────────────────────────────┤
│                                                       │
│  xterm.js terminal (full CLI output)                  │
│                                                       │
│                                                       │
│                                                       │
├───────────────────────────────────────────────────────┤
│ Capture: Ctrl+Shift+C  │  Opacity: 100%              │  ← info bar
└───────────────────────────────────────────────────────┘

Title bar elements:
  [●]              — app icon
  STEALTHPANE      — app name
  [▼ Claude Code]  — active provider (clickable to switch)
  ● HIDDEN         — content protection status (always green)
  [📌]             — always-on-top toggle
  [⚙]              — settings flyout

Settings flyout (when ⚙ clicked):
┌─────────────────────────────┐
│ GENERAL                     │
│ Provider      [▼ Claude Code]
│ Opacity       [━━━━━●━] 80% │
│ ─────────────────────────── │
│ CAPTURE                     │
│ Mode          [▼ Full Screen]
│ Hotkey        [Ctrl+Shift+C] │
│ ─────────────────────────── │
│ SYSTEM PROMPT               │
│ ┌─────────────────────────┐ │
│ │ Analyze the following…  │ │
│ │                         │ │
│ └─────────────────────────┘ │
│ [Reset to Default]          │
│ ─────────────────────────── │
│ Auto-cleanup     [30] min   │
└─────────────────────────────┘
```

---

## Project Structure

```
StealthPane/
├── StealthPane.csproj
├── Program.cs                          # Entry point
├── App.axaml / App.axaml.cs            # Avalonia app setup (dark theme)
├── MainWindow.axaml / .axaml.cs        # Main window — hosts WebView + title bar
│
├── Models/
│   ├── AppSettings.cs                  # All persisted settings
│   ├── CaptureSettings.cs             # Capture mode, region, hotkey
│   ├── CliProviderConfig.cs           # Provider definition (name, command, args, image support)
│   └── ImageInputMode.cs             # Enum: FilePath, Base64, StdinPipe
│
├── Services/
│   ├── ContentProtectionService.cs    # P/Invoke (Win) + ObjC interop (macOS)
│   ├── PtyService.cs                  # Spawns CLI process, manages PTY I/O
│   ├── PlatformHelper.cs             # OS detection utilities
│   ├── ScreenCaptureService.cs       # Platform-specific screenshot logic
│   ├── CaptureInjectorService.cs     # Assembles prompt + pastes into PTY
│   ├── SettingsService.cs            # Persists settings to JSON file
│   └── CliProviderRegistry.cs        # Manages built-in + custom providers
│
├── Controls/
│   └── TerminalWebView.cs            # WebView wrapper, bridges JS ↔ C#
│
├── Views/
│   └── SettingsView.axaml / .cs      # Settings flyout panel
│
├── Assets/
│   ├── terminal.html                 # Self-contained xterm.js page
│   ├── xterm.min.js                  # Bundled xterm.js
│   ├── xterm.css                     # Bundled xterm styles
│   └── xterm-addon-fit.min.js        # Auto-fit addon
│
└── app.manifest                       # Windows DPI awareness
```

---

## NuGet Dependencies
- `Avalonia` / `Avalonia.Desktop` / `Avalonia.Themes.Fluent` — UI framework

---

## Key Considerations
1. **Provider extensibility**: Adding a new CLI tool = adding a config entry. No code changes needed.
2. **Content protection + opacity**: Both work simultaneously — the window is see-through
   AND invisible to screen capture. `SetWindowDisplayAffinity` works with layered windows on Windows.
3. **Screenshot captures OTHER windows**: Our own window is protected from capture,
   but `ScreenCaptureService` captures the rest of the screen — this is intentional.
4. **Temp file cleanup**: A background timer cleans up old screenshots to avoid disk bloat.
5. **Provider-specific prompts**: Each provider can have its own default system prompt,
   since different CLIs may interpret image inputs differently.
6. **Opacity + readability**: At very low opacity the terminal text may be hard to read.
   The 10% floor prevents the window from becoming fully invisible to the user.

---

## Features (v1 — confirmed)
- [x] Always-on content protection (invisible to screen capture)
- [x] Extensible CLI provider system (Claude Code, Codex, Gemini, custom)
- [x] Full terminal emulation (xterm.js + PTY)
- [x] Cross-platform (Windows + macOS)
- [x] Dark minimal UI
- [x] Adjustable window opacity (10%–100%, slider in settings)
- [x] Always-on-top toggle (pin button in title bar)
- [x] Screenshot capture via hotkey → inject into CLI
- [x] Configurable capture mode (full screen / region / window / interactive)
- [x] Editable system prompt with per-provider defaults
- [x] Settings panel with persistence
- [x] Auto-cleanup of temp screenshots

---

## Future Ideas (not in v1)
- [ ] Multiple tabs / terminal sessions
- [ ] Split pane (two CLIs side by side)
- [ ] System tray / minimize to tray
- [ ] Session logging / history export
- [ ] Custom themes / font picker
- [ ] Clipboard protection (auto-clear after paste)
- [ ] Plugin system for additional capture post-processing
