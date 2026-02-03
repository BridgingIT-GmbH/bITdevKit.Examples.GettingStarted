# Example: Validation problem details

Goal: assert 400 responses include validation markers and field names.

Pattern:

- Check status code is 400.
- Use `MatchInContent` for `FluentValidationError`.
- Assert key field names in the response.

Reference:

- `tests/Modules/CoreModule/CoreModule.IntegrationTests/Presentation/Web/CustomerEndpointTests.cs`
