# MYG RideR

[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-9.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/maui/)  
[![C#](https://img.shields.io/badge/C%23-MVVM-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)  
[![Platform](https://img.shields.io/badge/platform-Android%20%7C%20iOS%20%7C%20Windows-blue)](#)

**MYG RideR** is a dark-themed motorcycle dashboard and ride-tracking app foundation built with .NET MAUI and C#. The solution is organized with Clean Architecture and MVVM to keep UI, application logic, and infrastructure concerns separate.

> **Project status:** Early development. The dashboard currently uses mock data; real GPS tracking, map integration, trip persistence, and complete ride-recording workflows are not yet implemented.

## Current scope

- Dashboard, Trips, Statistics, and Settings pages
- Reusable speed gauge, map panel, status bar, statistic cards, and bottom toolbar
- Dark-first design tokens for colors, typography, and spacing
- MVVM view models and dependency injection setup
- Clock service and mock dashboard data provider
- Separate Core, Application, Infrastructure, Shared, and Mobile projects

## Technology

- C# / .NET 9
- .NET MAUI (MauiVersion 9.0.120)
- CommunityToolkit.Maui
- CommunityToolkit.Mvvm
- Dependency Injection
- Clean Architecture and MVVM

## Solution structure

```text
MYG.RideR.sln
src/
  MYG.RideR.Core/           # Domain primitives and shared core logic
  MYG.RideR.Application/    # Application services and use cases
  MYG.RideR.Infrastructure/ # Infrastructure integrations
  MYG.RideR.Shared/         # Shared constants and contracts
  MYG.RideR.Mobile/         # MAUI UI, views, controls, and view models
docs/
  Architecture.md
  Roadmap.md
  AI_RULES.md
```

## Requirements

- .NET 9 SDK
- .NET MAUI workload
- Android SDK/emulator or a configured Android device for Android development
- On Windows, the appropriate Windows SDK for Windows-targeted builds

## Build

Run from the repository root:

```powershell
dotnet workload restore
dotnet restore MYG.RideR.sln
dotnet build MYG.RideR.sln --no-restore
```

To build specifically for Android:

```powershell
dotnet build src/MYG.RideR.Mobile/MYG.RideR.Mobile.csproj -f net9.0-android
```

Platform builds may require platform-specific workloads, SDKs, signing configuration, and—in the case of iOS/macOS—Apple development tooling.

## Roadmap

The next major steps are real GPS acquisition, map rendering, trip recording and persistence, statistics derived from actual trips, and export/share workflows. See [docs/Roadmap.md](docs/Roadmap.md) for the project roadmap.

## Privacy note

Location history is sensitive personal data. Before enabling real tracking or cloud sync, define clear permissions, local retention, export/delete behavior, and secure handling of any synchronized data.
