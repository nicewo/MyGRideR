# Roadmap

## Phase 0: Foundation

- Initialize solution and project structure.
- Configure Clean Architecture boundaries.
- Configure MAUI AppShell and dark design system.
- Create mock dashboard UI.

## Phase 1: Domain Modeling

- Define ride, trip, route, location sample, and dashboard domain models.
- Define application interfaces for location, trip recording, storage, and clock abstractions.
- Add focused unit tests for domain rules.

## Phase 2: Device Integrations

- Add GPS/location service behind interfaces.
- Add permission handling through platform abstractions.
- Add background execution strategy research for Android.

## Phase 3: Persistence

- Add SQLite schema and migrations.
- Implement repositories in Infrastructure.
- Add integration tests for persistence behavior.

## Phase 4: Maps and Ride Tracking

- Add map provider abstraction.
- Implement map display.
- Implement ride recording workflows.
- Add trip history and statistics.

## Phase 5: Production Readiness

- Add telemetry, diagnostics, crash reporting, release signing, CI, and store deployment workflows.
