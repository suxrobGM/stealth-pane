# Stealth Code

A Windows desktop app that runs AI coding CLIs in a secure, transparent, always-on-top terminal window **invisible to screen capture**.

Built with .NET 10 and Avalonia UI. Windows only.

## Features

- **Screen capture protection** — Window is excluded from screenshots and screen recording using `SetWindowDisplayAffinity`
- **Always-on-top overlay** — Pin the terminal above other windows with adjustable opacity, ideal for referencing code while working
- **Multiple CLI providers** — Switch between Claude Code, Codex, and Gemini CLI from the title bar
- **Built-in terminal** — Full xterm.js terminal powered by native PTY (winpty via Quick.PtyNet)
- **Screenshot capture & injection** — Capture full screen, regions, or specific windows and inject them into the active CLI session
- **Meeting audio capture** — Record system audio (WASAPI loopback), transcribe with Whisper, and send to the CLI for analysis
- **Global hotkeys** — Configurable hotkeys for screen capture, audio recording, and opacity cycling
- **Lightweight** — Ships as a single self-extracting AOT executable via a launcher that embeds the entire app

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
git clone https://github.com/suxrobgm/stealth-code.git
cd stealth-code

# Build and run
dotnet run --project src/StealthCode/StealthCode.csproj
```

## Publish

Build a single self-extracting executable (no .NET runtime required):

```powershell
cd scripts
.\publish.ps1
```

This publishes the main app, GZip-compresses all files, embeds them into a lightweight AOT launcher, and outputs a single `stealthcode.exe` at `publish/win-x64/`. On first run, the launcher extracts the app to a local `stealthcode_bin/` directory and launches it.

## Usage

| Action | Control |
|--------|---------|
| Move window | Drag the title bar |
| Switch provider | Title bar dropdown |
| Adjust opacity | Settings > General > Opacity slider |
| Toggle always-on-top | Pin button in title bar |
| Capture screenshot | `Shift+C` (configurable in settings) |
| Record meeting audio | `Shift+A` (press again to stop, transcribe, and send) |
| Cycle opacity | `Shift+O` (configurable in settings) |

## Architecture

```
src/
  StealthCode/                Main UI app (Avalonia, MVVM)
  StealthCode.Terminal/       PTY library (winpty)
  StealthCode.ScreenCapture/  Screenshot capture module (Win32 GDI)
  StealthCode.Audio/          Audio capture (NAudio WASAPI) + transcription (Whisper.net)
  StealthCode.Launcher/       Self-extracting launcher (AOT single exe)
scripts/
  publish.ps1                 Build + package pipeline
```

- **Modular** — Terminal, ScreenCapture, and Audio are independent modules with zero inter-module dependencies
- **MVVM** with CommunityToolkit.Mvvm source generators
- **Dependency injection** via `services.AddTerminal()`, `services.AddScreenCapture()`, `services.AddAudioCapture()`
- **Messaging** with `WeakReferenceMessenger` for decoupled communication
- **Native interop** via `LibraryImport` for Win32 APIs

## License

[MIT](LICENSE) - Sukhrob Ilyosbekov
