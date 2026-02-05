# Example: Database readiness

Goal: avoid flaky tests by waiting for database readiness before requests.

Reference:

- `tests/Modules/CoreModule/CoreModule.IntegrationTests/Presentation/Web/EndpointTestFixture.cs`

The fixture calls `IDatabaseReadyService.WaitForReadyAsync()` during initialization.
Rely on the fixture for readiness rather than adding sleeps in tests.
