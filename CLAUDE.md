# StealthPane

A cross-platform desktop app (Avalonia UI) that runs AI coding CLIs (Claude Code, Codex, Gemini CLI) in a secure, transparent, always-on-top terminal window invisible to screen capture.

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
- **Pattern:** MVVM with CommunityToolkit.Mvvm (source generators)
- **DI:** Microsoft.Extensions.DependencyInjection (all singletons)
- **Messaging:** CommunityToolkit.Mvvm `WeakReferenceMessenger` with explicit `Register<T>` (no `RegisterAll` — AOT incompatible)
- **Terminal:** xterm.js in `NativeWebView` (WebView2 on Windows), bridged via PTY

## Project Structure

```text
src/
  StealthPane/              # Main UI app
    Models/                 # Data classes (AppSettings, CliProviderConfig)
    ViewModels/             # MVVM view models (ObservableProperty, RelayCommand)
    Views/                  # XAML views + minimal code-behind
    Services/               # Platform services, settings persistence
    Controls/               # TerminalWebView (NativeWebView wrapper)
    Messages/               # Messenger record types
    Themes/                 # Theme.axaml (brushes), Styles.axaml (component styles)
  StealthPane.Terminal/     # PTY library (ConPTY on Windows, forkpty on macOS)
    Assets/                 # Embedded xterm.js + terminal.html
```

## Key Conventions

- File-scoped namespaces, braces required
- `[ObservableProperty]` for bindable properties, `[RelayCommand]` for commands
- Private fields: camelCase. Consts/statics: PascalCase
- Platform code guarded by `OperatingSystem.IsWindows()` / `OperatingSystem.IsMacOS()`
- Win32 interop uses `[LibraryImport]` with `partial` static classes
- All colors defined in `Themes/Theme.axaml`, all styles in `Themes/Styles.axaml`
- Reusable XAML classes: `Button.chrome`, `TextBlock.section-header`, `TextBlock.field-label`, `Border.divider`, `*.form-input`
- Settings persisted to `%APPDATA%/StealthPane/settings.json` via `System.Text.Json` source gen
- Code-behind kept minimal — only platform-specific ops (Win32 opacity, terminal lifecycle, window chrome, drag)
- Content protection disabled in DEBUG builds for screenshot debugging

## NativeWebView Caveat

`NativeWebView` renders in its own OS HWND — it sits above all Avalonia controls. Never overlay Avalonia controls on top of it; use side-by-side layout instead. Window opacity requires Win32 `SetLayeredWindowAttributes` (Avalonia's `Window.Opacity` won't affect it).
