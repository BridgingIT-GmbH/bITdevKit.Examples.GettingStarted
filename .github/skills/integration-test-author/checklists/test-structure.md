# Integration Test Structure Checklist

- Uses `[Collection(nameof(EndpointCollection))]`.
- Attaches output and configures fixture options in constructor.
- Uses fixture HttpClient for all requests.
- Uses shared seed helper or per-test seed function.
