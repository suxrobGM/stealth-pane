# StealthPane

A Windows desktop app that runs AI coding CLIs in a secure, transparent, always-on-top terminal window **invisible to screen capture**.

Built with .NET 10 and Avalonia UI. Windows only.

## Features

- **Screen capture protection** — Window is excluded from screenshots and screen recording using `SetWindowDisplayAffinity`
- **Always-on-top overlay** — Pin the terminal above other windows with adjustable opacity, ideal for referencing code while working
- **Multiple CLI providers** — Switch between Claude Code, Codex, and Gemini CLI from the title bar
- **Built-in terminal** — Full xterm.js terminal powered by native PTY (winpty via Quick.PtyNet)
- **Screenshot capture & injection** — Capture full screen, regions, or specific windows and inject them into the active CLI session
- **Global hotkeys** — Configurable hotkeys for screen capture and opacity cycling
- **Lightweight** — Ships as a single AOT-compiled native executable

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows 10+
- One or more supported CLI tools installed:
  - [Claude Code](https://docs.anthropic.com/en/docs/claude-code)
  - [Codex](https://github.com/openai/codex)
  - [Gemini CLI](https://github.com/google-gemini/gemini-cli)

## Quick Start

```bash
# Clone the repository
git clone https://github.com/anthropics/stealth-pane.git
cd stealth-pane

# Build and run
dotnet run --project src/StealthPane/StealthPane.csproj
```

## Publish

Build a single native executable (no .NET runtime required):

```bash
dotnet publish -c Release src/StealthPane/StealthPane.csproj
```

## Usage

| Action | Control |
|--------|---------|
| Move window | Drag the title bar |
| Switch provider | Title bar dropdown |
| Adjust opacity | Settings > General > Opacity slider |
| Toggle always-on-top | Pin button in title bar |
| Capture screenshot | `Ctrl+Shift+C` (configurable in settings) |
| Cycle opacity | `Ctrl+Shift+O` (configurable in settings) |

## Architecture

```
src/
  StealthPane/              Main UI app (Avalonia, MVVM)
  StealthPane.Terminal/     PTY library (winpty)
```

- **MVVM** with CommunityToolkit.Mvvm source generators
- **Dependency injection** via Microsoft.Extensions.DependencyInjection
- **Messaging** with `WeakReferenceMessenger` for decoupled communication
- **Native interop** via `LibraryImport` for Win32 APIs

## License

[MIT](LICENSE) - Sukhrob Ilyosbekov
