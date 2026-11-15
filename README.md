<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/smouj/voryda-down/main/assets/logo-dark.png?v=4f13fad2ad34f1086d4275045e30e6ec832cca22d27448ff84ec0b835fb0a65f">
    <source media="(prefers-color-scheme: light)" srcset="https://raw.githubusercontent.com/smouj/voryda-down/main/assets/logo.png?v=4f13fad2ad34f1086d4275045e30e6ec832cca22d27448ff84ec0b835fb0a65f">
    <img src="https://raw.githubusercontent.com/smouj/voryda-down/main/assets/logo.png?v=4f13fad2ad34f1086d4275045e30e6ec832cca22d27448ff84ec0b835fb0a65f" alt="Voryda Down app icon" width="180">
  </picture>
</p>

<h1 align="center">Voryda Down</h1>

<p align="center">
  <strong>Preview a media link, choose a format and download with confidence.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows%2010%202004%2B%20%7C%2011-0078D4?style=flat-square" alt="Windows 10 version 2004 or later and Windows 11">
  <img src="https://img.shields.io/badge/.NET-8-512BD4?style=flat-square" alt=".NET 8">
  <img src="https://img.shields.io/badge/UI-WinForms-5C2D91?style=flat-square" alt="Windows Forms">
  <img src="https://img.shields.io/badge/engine-yt--dlp%20%2B%20FFmpeg-555?style=flat-square" alt="yt-dlp and FFmpeg">
  <img src="https://img.shields.io/badge/license-MIT-2EA44F?style=flat-square" alt="MIT License">
</p>

<p align="center">
  <a href="#overview">Overview</a> ·
  <a href="#features">Features</a> ·
  <a href="#project-status">Status</a> ·
  <a href="#build-from-source">Build</a> ·
  <a href="#privacy">Privacy</a>
</p>

## Overview

**Voryda Down** is a native Windows front end for [yt-dlp](https://github.com/yt-dlp/yt-dlp). The C# and .NET 8 Windows Forms app provides a focused workflow for HTTP or HTTPS media URLs, quality selection, optional time ranges and local downloads. It launches yt-dlp as a child process and reports progress when yt-dlp provides it.

**yt-dlp is required and is not included in this repository.** FFmpeg is optional for workflows that need stream merging or media processing. Both are separate tools with their own licenses.

## Features

- Preview title, channel, duration, upload date, thumbnail, approximate size and available resolutions before downloading.
- Play the source page inside the app when the provider allows embedded playback.
- Mark a clip by dragging the start and end handles on the timeline; timestamp fields stay synchronized for precise edits.
- Download a video using Best, 1080p, 720p or 480p quality options.
- Save the best available audio stream in M4A format.
- Set optional start and end timestamps using seconds, <code>MM:SS</code> or <code>HH:MM:SS</code>.
- View progress, speed and estimated time when yt-dlp reports those values.
- Embed media metadata and thumbnails, and optionally save available subtitles.
- Customize the download folder and output filename template.
- Check yt-dlp and FFmpeg availability in the app.
- Cancel an active download.
- Keep playlists disabled; each operation targets a single item.
- Minimize the app to the system tray and restore it later.

## Project status

The project metadata declares version **1.0.0**. No public GitHub Release or release download is available yet.

## Screenshot

A refreshed screenshot is pending visual verification of the rebuilt Windows app.

## Runtime dependencies

The app looks for its command-line tools in this order:

| Tool | Lookup locations |
| --- | --- |
| <code>yt-dlp.exe</code> | Application folder, <code>extraResources</code> folder, then system <code>PATH</code> |
| <code>ffmpeg.exe</code> | Application folder, <code>extraResources</code> folder, then system <code>PATH</code> |

Place the executables in one of those locations before using the corresponding workflow. yt-dlp is required for downloads. FFmpeg is needed when the selected streams or processing path require it. Embedded playback uses the Microsoft Edge WebView2 Runtime; if playback is blocked or the runtime is unavailable, the app falls back to the video thumbnail.

The available streams and extraction behavior depend on the source service and the installed yt-dlp version.

## Usage

1. Enter an HTTP or HTTPS media URL and select **Ver video** to load its details.
2. Review the embedded player or thumbnail, title, channel, duration and formats reported by yt-dlp.
3. Select a video quality or **Audio only (M4A)**.
4. Drag the timeline handles to mark the wanted segment, or enter its start and end times. Set the output filename and metadata/subtitle options as needed.
5. Choose the destination folder and start the download. Use **Cancelar** to stop it.

Timestamp examples:

    83
    1:23
    0:01:23

## Local data

| Item | Location or default |
| --- | --- |
| Settings | <code>%APPDATA%\AreaDown\settings.json</code> |
| Default download folder | <code>%USERPROFILE%\Downloads</code> |
| Remembered values | Last URL, quality, destination folder, filename template and metadata/subtitle options |

The <code>AreaDown</code> settings path and internal project names are retained for compatibility while the public product name is Voryda Down.

## Requirements

- Windows 10 version 2004 (build 19041) or later, or Windows 11.
- .NET 8 Desktop Runtime to run a framework-dependent build. The documented self-contained build bundles .NET.
- Microsoft Edge WebView2 Runtime for in-app playback; metadata and thumbnail preview remain available without it.
- .NET 8 SDK to build from source.
- yt-dlp for all downloads; FFmpeg for workflows that require merging or processing.

## Build from source

    git clone https://github.com/smouj/voryda-down
    cd voryda-down
    dotnet build -c Release
    dotnet run --project src\AreaDown.App\AreaDown.App.csproj

Create a self-contained Windows x64 build:

    dotnet publish src\AreaDown.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\win-x64

This build includes the .NET runtime. `yt-dlp.exe` and `ffmpeg.exe` remain separate tools; place them next to `AreaDown.exe` (or install them on `PATH`). The `publish/win-x64` output is local build output and is not tracked in Git.

This repository keeps its product and developer guidance in this README.

## Privacy and responsible use

Voryda Down stores settings locally and has no account system or application telemetry in the current code. Downloading is a network operation: yt-dlp connects to the URL and may contact services required by the source site.

Use the app only for content you are authorized to download. yt-dlp and FFmpeg are independent projects and are distributed under their own licenses.

## License

Voryda Down is distributed under the [MIT License](LICENSE).

---

<p align="center">
  <strong>Voryda</strong><br>
  Focused Windows utilities.
</p>

<p align="center">
  <a href="https://github.com/smouj/voryda-snap">Snap</a> ·
  <a href="https://github.com/smouj/voryda-rec">Rec</a> ·
  <a href="https://github.com/smouj/voryda-down">Down</a> ·
  <a href="https://github.com/smouj/voryda-cut">Cut</a>
</p>
