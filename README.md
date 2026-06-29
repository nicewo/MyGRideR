# MYG RideR

MYG RideR is a production-grade motorcycle GPS dashboard and ride tracking application built with .NET MAUI and C#.

This repository is initialized as a clean, maintainable foundation only. GPS, maps, trip recording, persistence workflows, and domain business logic are intentionally not implemented yet.

## Technology

- .NET MAUI on .NET 9
- Android first, cross-platform ready
- C#
- Clean Architecture
- MVVM
- Dependency Injection
- CommunityToolkit.Maui
- CommunityToolkit.MVVM
- SQLite package preparation through the Infrastructure layer

## Solution Layout

```text
MYG.RideR.sln
src/
  MYG.RideR.Mobile
  MYG.RideR.Application
  MYG.RideR.Core
  MYG.RideR.Infrastructure
  MYG.RideR.Shared
tests/
  MYG.RideR.UnitTests
docs/
assets/
```

## Current Scope

- AppShell navigation
- Dark-only design system
- Dashboard, Trips, Statistics, and Settings placeholder pages
- Mock dashboard widgets
- Dependency Injection setup
- Unit test project scaffold

## Build

```powershell
dotnet restore MYG.RideR.sln
dotnet build MYG.RideR.sln --no-restore
```
