# Example: Fixture and authentication setup

Goal: configure the shared fixture once, attach output, and authenticate the HttpClient.

Reference:

- `tests/Modules/CoreModule/CoreModule.IntegrationTests/Presentation/Web/EndpointTestFixture.cs`

Pattern:

1. Attach output with `fixture.Attach(output)`.
2. Configure options once in the constructor.
3. The fixture acquires a token and attaches the bearer header.

CORRECT: Configure options before making any requests.
WRONG: Making requests before fixture options are configured.
