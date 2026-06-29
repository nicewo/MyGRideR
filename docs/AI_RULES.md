# AI Rules

These rules define how future AI-assisted development must proceed in this repository.

## General Standards

- Treat MYG RideR as production software, not a demo.
- Keep changes small, reviewable, and aligned with the current architecture.
- Do not add GPS, maps, persistence, or platform behavior without explicit task scope.
- Prefer boring, maintainable code over clever abstractions.

## Architecture

- Follow Clean Architecture dependency direction.
- Keep Core independent.
- Put contracts in the appropriate inner layer before adding Infrastructure implementations.
- Use MVVM for UI.
- Use Dependency Injection for object creation.
- Never introduce static services, service locators, or hidden global state.
- Every service implementation must have an interface.

## C# Guidelines

- Enable nullable-safe code.
- Prefer async APIs for I/O, device APIs, storage, network, and long-running work.
- Use records for immutable data snapshots and DTO-like values.
- Keep view models testable and free of platform-specific logic.
- Avoid code duplication; extract shared behavior only when it has a real second use.

## MAUI Guidelines

- Keep pages thin.
- Bind views to view models.
- Put reusable UI into Controls or Widgets.
- Keep visual tokens in Themes.
- Dark theme is the only supported theme until product requirements change.

## Testing

- Add unit tests for application and domain behavior as soon as real behavior is introduced.
- Mock interfaces, not concrete classes.
- Do not test generated framework behavior.

## Documentation

- Update Architecture.md when layer responsibilities change.
- Update Roadmap.md when milestone scope changes.
- Explain significant package additions and architectural decisions in PR descriptions.
