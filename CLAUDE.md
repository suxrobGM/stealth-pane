# StealthPane

A Windows desktop app (Avalonia UI) that runs AI coding CLIs (Claude Code, Codex, Gemini CLI) in a secure, transparent, always-on-top terminal window invisible to screen capture.

## Build & Run

```bash
dotnet build src/StealthPane/StealthPane.csproj
dotnet run --project src/StealthPane/StealthPane.csproj
```

Publish AOT single executable:

```bash
dotnet publish -c Release src/StealthPane/StealthPane.csproj
```

No test projects exist yet.

## Architecture

- **Framework:** .NET 10, Avalonia 11.3, PublishAot, full trim
- **Platform:** Windows only
- **Pattern:** MVVM with CommunityToolkit.Mvvm (source generators)
- **DI:** Microsoft.Extensions.DependencyInjection — stateful services (`PtyService`, `CleanupService`, `HotkeyService`) and ViewModels are registered as singletons. Stateless services (`SettingsService`, `CliProviderRegistry`, `ScreenCaptureService`, `CaptureInjectorService`, `ContentProtectionService`, `WindowEnumerationService`) are static classes.
- **Messaging:** CommunityToolkit.Mvvm `WeakReferenceMessenger` with explicit `Register<T>` (no `RegisterAll` — AOT incompatible)
- **Terminal:** xterm.js in `NativeWebView` (WebView2), bridged via PTY

## Project Structure

```text
src/
  StealthPane/              # Main UI app
    Models/                 # Data classes (AppSettings, CaptureSettings, CliProviderConfig)
    ViewModels/             # MVVM view models (ObservableProperty, RelayCommand)
    Views/                  # XAML views + minimal code-behind
    Services/               # Win32 services, settings persistence
    Controls/               # TerminalWebView (NativeWebView wrapper), HotkeyTextBox
    Messages/               # Messenger record types
    Themes/                 # Theme.axaml (brushes), Styles.axaml (component styles)
    Utilities/              # Window opacity helpers
  StealthPane.Terminal/     # PTY library
    Assets/                 # Embedded xterm.js + terminal.html
```

## PTY Implementation

- **Windows:** winpty via `Quick.PtyNet` NuGet package (`WinPtyProvider`). Creates a hidden console and screen-scrapes it to produce VT sequences. Limited to 16 colors but provides real TTY semantics required by interactive CLIs (e.g. Claude Code).
- **ConPTY note:** The native `CreatePseudoConsole` API has a rendering pipeline stall on Windows 11 25H2 (build 26200+) where output stops after 16 bytes of initialization sequences. The `WindowsPtyProvider` (ConPTY) was removed in favor of winpty until Microsoft fixes this.

## Key Conventions

- File-scoped namespaces, braces required
- `[ObservableProperty]` for bindable properties, `[RelayCommand]` for commands
- Private fields: camelCase. Consts/statics: PascalCase
- Win32 interop uses `[LibraryImport]` with `partial` static classes
- All colors defined in `Themes/Theme.axaml`, all styles in `Themes/Styles.axaml`
- Reusable XAML classes: `Button.chrome`, `Button.chrome.pinned`, `TextBlock.section-header`, `TextBlock.field-label`, `Border.divider`, `*.form-input`
- Settings persisted to `%APPDATA%/StealthPane/settings.json` via `System.Text.Json` source gen
- Code-behind kept minimal — only platform-specific ops (Win32 opacity, terminal lifecycle, window chrome, drag)
- Content protection disabled in DEBUG builds for screenshot debugging

## Capture Modes

- **Full Screen** — Captures entire primary monitor via `BitBlt`
- **Region** — User selects a screen region via overlay; coordinates saved in settings
- **Window** — User picks a window from a list; captured via `PrintWindow` with `PW_RENDERFULLCONTENT`

## Hotkeys

- Multiple named global hotkeys via `RegisterHotKey` Win32 API
- `HotkeyService` supports registering/unregistering by name ("capture", "opacity")
- Hotkey format: `Modifier+Key` (e.g. `Ctrl+Shift+C`). Validated by `HotkeyTextBox` control.

## NativeWebView Caveat

`NativeWebView` renders in its own OS HWND — it sits above all Avalonia controls. Never overlay Avalonia controls on top of it; use side-by-side layout instead. Window opacity requires Win32 `SetLayeredWindowAttributes` (Avalonia's `Window.Opacity` won't affect it).
