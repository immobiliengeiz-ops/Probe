# Aether Player (Native WPF Desktop Player)

Aether Player is a native Windows desktop music player built with **C# / .NET / WPF** and MVVM.

## Core capabilities
- Native WPF UI with dark premium style.
- Local library scanning from one or more selected music folders.
- Drag & drop import (audio files or folders) directly into the app window.
- Metadata extraction from local audio files.
- Multi-select library interaction with bulk actions.
- Track context menus for core media-player operations.
- Proper **Add to Playlist** / **Add to Collection** dialogs with choose-or-create flow.
- Real queue with visible queue panel, remove/reorder controls, and persisted queue state.
- Shuffle and Repeat controls integrated in player bar.
- Library tools: re-scan folders, refresh metadata, and validate missing files.
- Missing-file robustness with clear status and manual re-link command.

## Build and run on Windows
### Prerequisites
- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 + Desktop development with .NET workload

### Visual Studio
1. Open `AetherPlayer.sln`
2. Set startup project to `AetherPlayer`
3. Restore NuGet packages
4. Press `F5`

### CLI
```bash
cd AetherPlayer
 dotnet restore
 dotnet build
 dotnet run
```

## Persistence location
`%LOCALAPPDATA%\AetherPlayer\appstate.json`


## Binary demo asset policy
Demo binary audio/cover files were intentionally removed from the repository to keep PRs text-only and Codex-safe.
See `AetherPlayer/Assets/README.md` for the exact removed file list and replacement guidance.
