# Architecture

MYG RideR follows Clean Architecture with MVVM at the mobile presentation boundary.

## Layers

### MYG.RideR.Core

Contains enterprise-level domain primitives and future entities. It must not depend on other project layers.

### MYG.RideR.Application

Contains application use cases, service contracts, orchestration, validators, and DTOs. It references Core and Shared.

### MYG.RideR.Infrastructure

Contains implementations for external concerns such as persistence, device integrations, file storage, network clients, and platform adapters. It references Application contracts and implements them through Dependency Injection.

### MYG.RideR.Mobile

Contains .NET MAUI views, view models, UI services, controls, widgets, themes, and the composition root. It references Application and Infrastructure for startup registration only.

### MYG.RideR.Shared

Contains cross-layer constants and simple shared primitives that do not belong to a specific domain layer.

## Rules

- Core has no dependency on Application, Infrastructure, or Mobile.
- Application does not depend on Infrastructure or Mobile.
- Infrastructure implements abstractions defined by Application or Core.
- Mobile uses MVVM and constructor injection.
- Services must be asynchronous whenever I/O or platform work is involved.
- No static service locators or global mutable state.
- Every service implementation must have an interface.
