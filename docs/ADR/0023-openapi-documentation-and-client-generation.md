# ADR-0023: OpenAPI Documentation, Scalar API Reference, and Generated Clients

## Status

Accepted

## Context

The application exposes a REST API through Minimal API endpoints (ADR-0014) protected by JWT bearer authentication (ADR-0020). The API contract has several consumers: developers exploring endpoints, browser and mobile clients, other services, test tooling, and AI agents. Each of them needs a machine-readable and human-readable description of the same contract.

### Technical Requirements

- A machine-readable OpenAPI document that is generated from the code, not written by hand
- Interactive API documentation that can authenticate against the application's identity provider
- Strongly typed client code for .NET and TypeScript consumers, generated from the document
- Ready-to-use `.http` request files for manual and scripted testing
- Automated quality checks on the contract
- Document content that reflects the Result and ProblemDetails error model (ADR-0002)

### Business Requirements

- API consumers can integrate without reading the source code
- The contract stays in step with the implementation without manual upkeep
- Breaking or low-quality API descriptions are detected early

### Design Challenges

- Generated documents must not drift from the running application
- Interactive documentation must not become a production attack surface
- Client generation and linting must be repeatable for every developer, without installing tools by hand
- The generation mechanism must not require a running server

## Decision

Use the **built-in ASP.NET Core OpenAPI generator** for the document, **Scalar** for interactive documentation, and a small set of **pinned tools driven through the bdk CLI** for linting and client generation.

### How It Works

1. **Document generation**: `Microsoft.AspNetCore.OpenApi` produces the document. Registration lives in `ProgramExtensions.OpenApi.cs` (`AddAppOpenApi`) and adds document and schema transformers for the API title, the security schemes (derived from `Authentication:Authority`), diagnostics, and the `Result` problem-details schema.
2. **Build-time spec file**: `Microsoft.Extensions.ApiDescription.Server` generates the document after every build into `src/Presentation.Web.Server/wwwroot/openapi.json`. The file is committed, so the contract is reviewable in pull requests and available to tools without starting the server.
3. **Interactive reference**: `Scalar.AspNetCore` serves the API reference through `MapScalar()`. It reads `/openapi.json`, uses the C# `HttpClient` snippet as default, and signs in through the OAuth2 authorization code flow with the `Scalar` client of the fake identity provider (ADR-0020).
4. **Environment gating**: `MapOpenApi()` and `MapScalar()` are only mapped when the host runs in local development or in a container.
5. **Tooling through bdk tasks** (category "API & Spec"), all reading `src/Presentation.Web.Server/wwwroot/openapi.json`:
   - **OpenAPI Lint**: Spectral (run from a Docker image) with the repository ruleset `.spectral.yaml`, which extends `spectral:oas` and keeps `info-description` and `operation-description` as warnings
   - **C# Client Generate**: Kiota, client class `ApiClient`, namespace `OpenApi.Client`
   - **TypeScript Client Generate**: Kiota, client class `ApiClient`
   - **HTTP Requests Generate**: `httpgenerator`, one `.http` file per tag, with a bearer authorization header placeholder
6. **Pinned tools**: `kiota` and `httpgenerator` are declared in `.config/dotnet-tools.json` (ADR-0024) and restored with `dotnet tool restore`. Generated clients and request files are written to the configured output directory (default `.artifacts`) and are not committed to the source tree.

### Rules

- Describe endpoints through Minimal API metadata (names, summaries, produced types, status codes) so the document is complete without manual edits (ADR-0014).
- Never hand-edit `openapi.json`; change the code or a transformer and rebuild.
- Review the diff of `openapi.json` in pull requests as the API contract change.
- Generate clients on demand from the committed document; do not commit generated client code.

## Rationale

1. **Single source of truth**: the document is generated from the endpoint code, so the contract cannot silently drift from the implementation.
2. **No extra runtime dependency**: the built-in generator ships with ASP.NET Core and follows the framework's release cycle, instead of adding a third-party generator that must track it.
3. **Server-less tooling**: because the spec is produced at build time and committed, linting and client generation work in CI and on developer machines without a running application or database.
4. **Reviewable contract**: contract changes show up as a readable diff next to the code change.
5. **Good developer experience**: Scalar gives an interactive reference with OAuth2 sign-in against the local identity provider, which makes manual testing of protected endpoints straightforward.
6. **Repeatable client generation**: Kiota and httpgenerator are pinned tools, so every developer and pipeline produces clients from the same tool versions.
7. **Early quality feedback**: Spectral with a project ruleset catches missing descriptions and structural problems before consumers see them.

## Consequences

### Positive

- API consumers get an accurate, always-current contract and interactive documentation.
- .NET and TypeScript clients and `.http` files can be produced with one task each.
- Contract changes are visible in code review.
- Error responses are documented consistently with the Result and ProblemDetails model.

### Negative

- WARNING: the committed spec lives in `wwwroot` and the host enables static files in every environment, so the file may be reachable outside local development even though Scalar and `MapOpenApi()` are not mapped there. This should be reviewed and, if undesired, excluded from non-development environments.
- Spec generation runs the application's startup path during the build, so startup problems can surface as build failures.
- The Spectral task depends on Docker and uses the `latest` image tag, so lint results can change when the image changes.
- A generated, committed file can create merge conflicts when several branches change endpoints.
- Kiota-generated clients reflect the spec as is; poor metadata in the endpoints leads to poor client APIs.

### Neutral

- Generated clients are artifacts and are expected to be regenerated, not versioned.
- Scalar replaces the Swagger UI; the OpenAPI document itself stays tool-agnostic.
- The document is OpenAPI 3.x as produced by the built-in generator.

## Alternatives Considered

- **Alternative 1: Swashbuckle with Swagger UI**
  - Established generator and UI, with a large community.
  - Rejected because the built-in generator now covers the needs without an additional dependency, and the Minimal API integration is first-class.

- **Alternative 2: NSwag for documents and client generation**
  - One tool for document, UI, and C#/TypeScript clients.
  - Rejected because it adds a heavier dependency and a second client generation approach, while Kiota already produces both client languages from the plain document.

- **Alternative 3: Runtime-only document, no committed file**
  - Generate the document only when the server runs and fetch it over HTTP for tooling.
  - Rejected because linting and client generation would need a running application and database, and contract changes would not be visible in code review.

- **Alternative 4: Contract-first, hand-written OpenAPI file**
  - Author the spec manually and generate server stubs from it.
  - Rejected because it duplicates the contract in two places and fits poorly with Minimal API endpoints that already carry the metadata.

## Related Decisions

- [ADR-0002](0002-result-pattern-error-handling.md): The schema transformer documents Result-based error responses
- [ADR-0014](0014-minimal-api-endpoints-dto-exposure.md): Endpoint metadata and DTO exposure feed the generated document
- [ADR-0020](0020-jwt-bearer-authentication-authorization.md): Security schemes and the Scalar OAuth2 client come from the authentication setup
- [ADR-0024](0024-central-package-management-and-versioning.md): The generation tools are pinned in the tool manifest

## References

- [ASP.NET Core OpenAPI Documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview)
- [Scalar for ASP.NET Core](https://github.com/scalar/scalar)
- [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview)
- [Spectral](https://github.com/stoplightio/spectral)
- [HttpGenerator](https://github.com/christianhelle/httpgenerator)
- Project Documentation: `README.md`
- Module Documentation: `src/Modules/CoreModule/CoreModule-README.md`

## Notes

### Build-Time Generation

```xml
<!-- src/Presentation.Web.Server/Presentation.Web.Server.csproj -->
<PropertyGroup>
  <OpenApiGenerateDocuments>true</OpenApiGenerateDocuments>
  <OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)/wwwroot</OpenApiDocumentsDirectory>
  <OpenApiGenerateDocumentsOptions>--file-name openapi</OpenApiGenerateDocumentsOptions>
</PropertyGroup>
<Target Name="GenerateOpenApiAfterBuild" AfterTargets="Build"
        DependsOnTargets="GenerateOpenApiDocuments"
        Condition="'$(OpenApiGenerateDocuments)' == 'true'" />
```

### Registration

```csharp
// ProgramExtensions.OpenApi.cs
services.AddOpenApi(options => options
    .AddDocumentTransformer<DiagnosticDocumentTransformer>()
    .AddDocumentTransformer(new DocumentInfoTransformer(new DocumentInfoOptions
    {
        Title = "BridgingIT.DevKit.Examples.GettingStarted API"
    }))
    .AddSchemaTransformer<DiagnosticSchemaTransformer>()
    .AddSchemaTransformer<ResultProblemDetailsSchemaTransformer>()
    .AddDocumentTransformer<OpenApiSecurityDocumentTransformer>());
```

### Common Tasks

| Goal | bdk task | Underlying tool |
| ---- | -------- | --------------- |
| Lint the contract | `openapi-lint` | Spectral (Docker) |
| Generate C# client | `openapi-client-dotnet` | Kiota |
| Generate TypeScript client | `openapi-client-typescript` | Kiota |
| Generate `.http` files | `openapi-http` | httpgenerator |

### Open Question

- Decide whether `wwwroot/openapi.json` should be served outside local development and containers. If not, move it out of `wwwroot` or exclude it from static file serving in other environments.

### Implementation Files

- `src/Presentation.Web.Server/ProgramExtensions.OpenApi.cs`
- `src/Presentation.Web.Server/Presentation.Web.Server.csproj`
- `src/Presentation.Web.Server/wwwroot/openapi.json`
- `.spectral.yaml`
- `.bdk/cli/lib/OpenApi.csx`
- `.config/dotnet-tools.json`
