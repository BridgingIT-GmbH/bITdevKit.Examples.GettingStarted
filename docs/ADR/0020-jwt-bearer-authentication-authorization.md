# ADR-0020: JWT Bearer Authentication & Authorization Strategy

## Status

Accepted

## Context

Modern web APIs require secure authentication and authorization mechanisms to:

- **Protect Resources**: Ensure only authenticated users can access protected endpoints
- **Stateless Authentication**: Support distributed systems without server-side session storage
- **Token-Based Security**: Enable Single Page Applications (SPAs), mobile apps, and API clients
- **Role-Based Access Control**: Authorize users based on roles and claims
- **OAuth2/OpenID Connect Compliance**: Support standard authentication protocols
- **Development Velocity**: Provide fast development/testing without external identity providers
- **Production Readiness**: Plan migration path to production-grade identity providers

Traditional authentication approaches face challenges:

- **Cookie-Based Sessions**: Not suitable for cross-domain SPAs and mobile apps
- **Basic Authentication**: Credentials sent with every request, no expiration
- **External Dependencies**: Requiring external identity providers slows local development
- **Complex Setup**: OAuth2 flows require significant configuration
- **Testing Friction**: Integration tests need authentication infrastructure

The application needed an authentication strategy that:

1. Uses **JWT Bearer tokens** for stateless authentication
2. Supports **OAuth2/OpenID Connect** standard flows
3. Provides **fake identity provider** for development and testing
4. Enables **role-based authorization** via claims
5. Integrates with **ASP.NET Core authentication/authorization**
6. Includes **current user accessor** for business logic
7. Plans for **production identity provider migration**

## Decision

Adopt **JWT Bearer authentication** with the **bITdevKit Fake Identity Provider** for development/testing and a clear migration path to production identity providers (e.g., Azure AD, Keycloak, IdentityServer).

### Core Authentication Components

**1. JWT Bearer Authentication Setup** (`Program.cs:63`)

```csharp
builder.Services.AddJwtBearerAuthentication(builder.Configuration);
```

Configures JWT Bearer authentication with:

- Token validation against configured authority
- Audience and issuer validation
- Automatic JWT claims mapping
- Bearer token extraction from `Authorization` header

**2. Fake Identity Provider** (`Program.cs`)

```csharp
builder.Services.AddAppIdentityProvider(
    builder.Environment.IsLocalDevelopment() || builder.Environment.IsContainerized(),
    builder.Configuration);
```

Implementation (`ProgramExtensions.cs`):

```csharp
public static IServiceCollection AddAppIdentityProvider(
    this IServiceCollection services, bool enabled, IConfiguration configuration)
{
    return services.AddFakeIdentityProvider(o => o
        .Enabled(enabled)
        .WithIssuer(configuration["Authentication:Authority"])
        .WithUsers(FakeUsers.Fantasy)
        .WithClient("test", "test-client")
        .WithClient("Scalar", "scalar",
            $"{configuration["Authentication:Authority"]}/scalar/"));
}
```

**3. Current User Accessor** (`Program.cs`)

```csharp
builder.Services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();
```

Provides access to authenticated user context in application/domain layers.

**4. Middleware Pipeline** (`Program.cs`)

```csharp
app.UseAuthentication();    // Validates JWT and populates User principal
app.UseAuthorization();     // Enforces authorization policies
app.UseCurrentUserLogging(); // Logs current user info for audit trails
```

### Configuration

**appsettings.json** (Shared)

```json
{
  "Authentication": {
    "Authority": "https://localhost:5001"
  }
}
```

### Endpoint Protection

**Minimal API Endpoints** (`CustomerEndpoints.cs`)

```csharp
var group = app
    .MapGroup("api/coremodule/customers")
    .RequireAuthorization()  // All endpoints require authentication
    .WithTags("CoreModule.Customers");
```

Individual endpoints produce 401 status:

```csharp
.Produces(StatusCodes.Status401Unauthorized)
```

### Fake Identity Provider for Development

The bITdevKit Fake Identity Provider supports:

**OAuth2 Grant Types**:

- Authorization Code Flow (SPAs, web apps)
- Password Grant Flow (development, testing)
- Client Credentials Flow (service-to-service)
- Refresh Token Flow

**Key Endpoints** (`.bdk/docs/features-identityprovider.md`):

- `GET /api/_system/identity/connect/authorize` - Start OAuth2 flow
- `POST /api/_system/identity/connect/token` - Exchange code/credentials for tokens
- `GET /api/_system/identity/connect/userinfo` - Get user information
- `GET /api/_system/identity/.well-known/openid-configuration` - Discovery document

**Preconfigured Users** (`FakeUsers.Fantasy`):

- Hardcoded test users with roles (Administrators, Users)
- No password validation (development convenience)
- Deterministic user selection UI

**Security Characteristics** (Development Only):

- Symmetric key signing (HS256)
- No client secret validation
- Debug endpoints exposed
- No token encryption requirements

### OpenAPI Integration

**Scalar UI Integration** (`ProgramExtensions.cs:154-174`)

```csharp
app.MapScalarApiReference(o =>
{
    o.AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme)
     .AddAuthorizationCodeFlow(JwtBearerDefaults.AuthenticationScheme, f =>
     {
         var idpOptions = app.Services.GetService<FakeIdentityProviderEndpointsOptions>();
         var idpClient = idpOptions?.Clients?.FirstOrDefault(
             c => string.Equals(c.Name, "Scalar", StringComparison.OrdinalIgnoreCase));
         f.ClientId = idpClient?.ClientId;
         f.AuthorizationUrl = $"{idpOptions?.Issuer}/api/_system/identity/connect/authorize";
         f.TokenUrl = $"{idpOptions?.Issuer}/api/_system/identity/connect/token";
         f.RedirectUri = idpClient?.RedirectUris?.FirstOrDefault();
     });
});
```

**OpenAPI Security Scheme** (`ProgramExtensions.cs:90`)

```csharp
.AddDocumentTransformer<BearerSecurityRequirementDocumentTransformer>()
```

### Production Migration Path

**Phase 1: Development (Current)**

- Use Fake Identity Provider
- Enabled in `IsLocalDevelopment() || IsContainerized()`
- No external dependencies

**Phase 2: Staging**

- Replace with production identity provider (Azure AD, Keycloak, Auth0)
- Update `Authentication:Authority` to external IDP
- Configure client credentials
- Test OAuth2 flows

**Phase 3: Production**

- Remove `AddFakeIdentityProvider` registration
- Keep `AddJwtBearerAuthentication` unchanged
- Configure production JWT validation parameters
- Enable advanced features (token encryption, certificate validation)

**Migration Code Pattern**:

```csharp
// Before (Development)
builder.Services.AddAppIdentityProvider(
    builder.Environment.IsLocalDevelopment() || builder.Environment.IsContainerized(),
    builder.Configuration);

// After (Production)
// Remove AddAppIdentityProvider call entirely
// JWT Bearer authentication automatically validates against external authority

// Optional: Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Administrators"));
});
```

## Rationale

### Why JWT Bearer?

1. **Stateless**: No server-side session storage, scales horizontally
2. **Cross-Platform**: Works with SPAs, mobile apps, API clients
3. **Self-Contained**: Claims embedded in token, no database lookups
4. **Standard**: OAuth2/OpenID Connect industry standard
5. **Expiration**: Built-in token lifetime management

### Why Fake Identity Provider for Development?

1. **Zero External Dependencies**: No need for Azure AD, Auth0, or Keycloak
2. **Instant Setup**: Works out-of-the-box with minimal configuration
3. **Deterministic Testing**: Predictable users and roles
4. **Fast Iteration**: No network calls to external services
5. **Full Control**: Customize users, roles, and claims

### Why bITdevKit Implementation?

1. **Framework Integration**: Designed specifically for bITdevKit applications
2. **Minimal API Support**: Works seamlessly with Minimal API endpoints
3. **Scalar Integration**: Automatic OAuth2 flow in API documentation
4. **Consistent Patterns**: Matches bITdevKit conventions and extensions
5. **Migration Ready**: Clear separation between dev and prod configuration

### Why ICurrentUserAccessor?

1. **Layer Independence**: Application/Domain layers don't depend on `HttpContext`
2. **Testability**: Easy to mock for unit tests
3. **Consistent Interface**: Standard abstraction across all modules
4. **Future-Proof**: Supports non-HTTP contexts (background jobs, console commands)

## Consequences

### Positive

- **Fast Development**: No external identity provider setup required
- **Stateless Authentication**: Enables horizontal scaling and distributed systems
- **Standard Compliance**: OAuth2/OpenID Connect compatibility
- **Framework Integration**: Uses ASP.NET Core authentication/authorization primitives
- **Testable**: Fake IDP enables integration tests without external dependencies
- **Production Ready**: Clear migration path to production identity providers
- **Role-Based Authorization**: Built-in support for role and claim-based access control
- **API Documentation**: Scalar UI includes interactive OAuth2 authentication
- **Audit Logging**: `UseCurrentUserLogging()` automatically logs authenticated user

### Negative

- **Development-Only IDP**: Fake Identity Provider must be replaced in production
- **Token Storage**: Clients responsible for secure token storage (not framework concern)
- **No Built-In Refresh**: Fake IDP provides refresh tokens but no automatic client refresh logic
- **Limited User Management**: Hardcoded users only; no self-registration or password reset
- **Symmetric Signing**: Fake IDP uses HS256 (symmetric); production should use RS256 (asymmetric)
- **Migration Effort**: Production deployment requires identity provider setup and configuration
- **No Multi-Tenancy**: Single issuer configuration; multi-tenant scenarios require additional work

### Neutral

- **Token Validation**: Automatic JWT validation adds minimal per-request overhead (~1-2ms)
- **Configuration Required**: `Authentication:Authority` must be set correctly
- **Middleware Ordering**: `UseAuthentication()` must precede `UseAuthorization()`
- **Claims Transformation**: Future claims transformation requires additional configuration
- **Token Lifetime**: Default lifetimes from Fake IDP; production IDP controls expiration
- **Environment-Specific**: Fake IDP behavior differs between development and production

## Alternatives Considered

### Alternative 1: Cookie-Based Authentication

**Description**: Use ASP.NET Core Cookie Authentication with server-side sessions.

**Rejected Because**:

- Not suitable for SPAs and mobile apps (cross-domain issues)
- Requires server-side session storage (reduces scalability)
- Doesn't support OAuth2/OpenID Connect flows
- Complicates API client integration

### Alternative 2: Always Require External Identity Provider

**Description**: Require Azure AD or Keycloak even in development.

**Rejected Because**:

- Increases development friction and setup complexity
- Requires internet connectivity for local development
- External IDP outages block development
- Costs for cloud-based identity providers (Azure AD)
- Configuration drift between dev and production

### Alternative 3: Basic Authentication

**Description**: Use HTTP Basic Authentication with username/password.

**Rejected Because**:

- Credentials sent with every request (security risk)
- No token expiration or revocation
- No support for roles or claims
- Not compatible with OAuth2 flows
- Poor user experience (browser prompts)

### Alternative 4: API Key Authentication

**Description**: Use custom API key header for authentication.

**Rejected Because**:

- No standard protocol (OAuth2/OpenID Connect)
- No role or claim support
- Difficult to implement user context
- Poor integration with SPA frameworks
- No token expiration or refresh mechanism

### Alternative 5: IdentityServer4 / Duende IdentityServer

**Description**: Self-host production-grade identity provider in the same application.

**Rejected Because**:

- Significant complexity for a getting-started example
- IdentityServer4 deprecated; Duende requires commercial license
- Adds database and UI dependencies
- Overkill for development/testing scenarios
- bITdevKit Fake IDP provides same development benefits with less complexity

### Alternative 6: Azure AD B2C Only

**Description**: Use Azure AD B2C for both development and production.

**Rejected Because**:

- Requires Azure subscription for development
- Increases cost for every developer
- Requires internet connectivity
- External dependency slows local development
- Fake IDP pattern allows offline development

## Related Decisions

- [ADR-0001](0001-clean-onion-architecture.md): Authentication middleware in Presentation layer; `ICurrentUserAccessor` injected into Application/Domain
- [ADR-0014](0014-minimal-api-endpoints-dto-exposure.md): `.RequireAuthorization()` applied to Minimal API endpoint groups
- [ADR-0016](0016-logging-observability-strategy.md): `UseCurrentUserLogging()` enriches logs with authenticated user information
- [ADR-0018](0018-dependency-injection-service-lifetimes.md): `ICurrentUserAccessor` registered as Scoped (per-request)

## References

- [bITdevKit Fake Identity Provider Documentation](.bdk/docs/features-identityprovider.md)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [OAuth 2.0 RFC 6749](https://datatracker.ietf.org/doc/html/rfc6749)
- [OpenID Connect Core Specification](https://openid.net/specs/openid-connect-core-1_0.html)
- [Scalar OpenAPI Authentication](https://github.com/scalar/scalar)

## Notes

### Implementation Files

**Authentication Setup**:

- `src/Presentation.Web.Server/Program.cs` (Registration and middleware
- `src/Presentation.Web.Server/ProgramExtensions.cs` Configuration extensions
- `src/Presentation.Web.Server/appsettings.json` Authority configuration

**Endpoint Protection**:

- `src/Modules/CoreModule/CoreModule.Presentation/Web/Endpoints/CustomerEndpoints.cs` - Authorization requirement

**API Testing**:

- `src/Presentation.Web.Server/Authentication-API.http` - HTTP requests for testing authentication flows

### Authentication Flow Example

**Resource Owner Password Flow** (Development/Testing):

1. Client sends credentials to token endpoint:

   ```http
   POST /api/_system/identity/connect/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=password
   &client_id=test-client
   &username=luke.skywalker@starwars.com
   &scope=openid profile email roles
   ```

2. Fake IDP returns JWT tokens:

   ```json
   {
     "access_token": "eyJhbGci...",
     "expires_in": 1800,
     "refresh_token": "eyJhbGci...",
     "token_type": "Bearer",
     "scope": "openid profile email roles"
   }
   ```

3. Client calls protected endpoint with Bearer token:

   ```http
   GET /api/coremodule/customers
   Authorization: Bearer eyJhbGci...
   ```

4. `JwtBearerAuthentication` middleware validates token:
   - Verifies signature using issuer's signing key
   - Validates issuer, audience, expiration
   - Populates `HttpContext.User` with claims

5. `RequireAuthorization()` checks authentication:
   - Returns `401 Unauthorized` if token missing/invalid
   - Proceeds to endpoint handler if authenticated

6. Handler accesses current user via `ICurrentUserAccessor`:

   ```csharp
   var userId = currentUserAccessor.UserId;
   var userName = currentUserAccessor.UserName;
   var roles = currentUserAccessor.Roles;
   ```

**Authorization Code Flow** (SPAs, Blazor):

1. Client redirects to authorization endpoint:

   ```
   /api/_system/identity/connect/authorize
     ?response_type=code
     &client_id=blazor-wasm
     &scope=openid profile email roles
     &redirect_uri=https://localhost:5001/authentication/login-callback
     &state=random123
   ```

2. User selects identity in Fake IDP UI

3. IDP redirects back with authorization code:

   ```
   https://localhost:5001/authentication/login-callback
     ?code=xyz789
     &state=random123
   ```

4. Client exchanges code for tokens (same as step 2 above)

5. Client stores tokens (localStorage, sessionStorage, memory)

6. Client includes token in API requests (same as step 3-6 above)

### Accessing Current User in Application Layer

**Command/Query Handler**:

```csharp
public class CustomerCreateCommandHandler : IRequester<CustomerCreateCommand, Result<CustomerModel>>
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IGenericRepository<Customer> repository;

    public CustomerCreateCommandHandler(
        ICurrentUserAccessor currentUserAccessor,
        IGenericRepository<Customer> repository)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.repository = repository;
    }

    public async Task<Result<CustomerModel>> Handle(
        CustomerCreateCommand request, CancellationToken cancellationToken)
    {
        // Access authenticated user
        var userId = this.currentUserAccessor.UserId;
        var userName = this.currentUserAccessor.UserName;

        // Use in business logic
        var customer = Customer.Create(
            request.Model.FirstName,
            request.Model.LastName,
            EmailAddress.Create(request.Model.EmailAddress).Value,
            createdBy: userName);

        // Repository behaviors automatically add audit fields
        await this.repository.InsertAsync(customer, cancellationToken);

        return Result<CustomerModel>.Success(customer.Adapt<CustomerModel>());
    }
}
```

**Domain Event Handler**:

```csharp
public class CustomerCreatedDomainEventHandler : IDomainEventHandler<CustomerCreatedDomainEvent>
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly ILogger logger;

    public async Task Handle(CustomerCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var user = this.currentUserAccessor.UserName ?? "System";
        this.logger.LogInformation(
            "Customer {CustomerId} created by user {UserName}",
            notification.Customer.Id,
            user);
    }
}
```

### Security Considerations

**Development (Fake IDP)**:

- WARNING: Never expose Fake IDP publicly or connect to production data
- Fake IDP uses symmetric signing (HS256) - acceptable for development only
- No client secret validation - any client_id accepted
- Hardcoded users - no real authentication
- Debug endpoints exposed - configuration visible

**Production (External IDP)**:

- Use asymmetric signing (RS256) with certificate rotation
- Validate client secrets for confidential clients
- Enable token encryption for sensitive claims
- Set appropriate token lifetimes (access: 15-60 min, refresh: 1-7 days)
- Implement token revocation checks
- Enable audit logging for authentication events
- Use HTTPS only for token endpoints
- Validate redirect URIs strictly
- Implement rate limiting on token endpoints

### Testing Authentication

**Unit Tests** (Application Layer):

```csharp
[Fact]
public async Task Handle_WithAuthenticatedUser_ShouldAuditCreator()
{
    // Arrange
    var mockUserAccessor = Substitute.For<ICurrentUserAccessor>();
    mockUserAccessor.UserId.Returns("user-123");
    mockUserAccessor.UserName.Returns("Luke Skywalker");

    var handler = new CustomerCreateCommandHandler(
        mockUserAccessor,
        mockRepository);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    // Verify audit fields populated with user info
}
```

**Integration Tests** (Endpoint Tests):

```csharp
[Fact]
public async Task GetCustomers_WithoutToken_ShouldReturn401()
{
    // Arrange
    var client = factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/coremodule/customers");

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task GetCustomers_WithValidToken_ShouldReturn200()
{
    // Arrange
    var token = await GetAccessTokenAsync(); // Authenticate with Fake IDP
    var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/api/coremodule/customers");

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
}
```

### Role-Based Authorization Example

**Future Enhancement** (not currently implemented):

```csharp
// Program.cs - Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Administrators"));

    options.AddPolicy("RequireCustomerAccess", policy =>
        policy.RequireClaim("scope", "customers:read"));
});

// Endpoint - Apply policy
group.MapDelete("/{id:guid}", async (/* ... */) => { /* ... */ })
    .RequireAuthorization("RequireAdmin")  // Only admins can delete
    .WithName("CoreModule.Customers.Delete");

// Alternative: Combine authentication + authorization
group.MapDelete("/{id:guid}", async (/* ... */) => { /* ... */ })
    .RequireAuthorization(policy => policy
        .RequireAuthenticatedUser()
        .RequireRole("Administrators"))
    .WithName("CoreModule.Customers.Delete");
```

### Known Limitations

1. **No Cookie Authentication**: Fake IDP supports only Bearer tokens; add `.AddCookieAuthentication()` if web app needs cookie-based auth
2. **Single Issuer**: Configuration supports one authority; multi-tenant scenarios require custom JWT validation
3. **No Claims Transformation**: Additional claims mapping requires `IClaimsTransformation` implementation
4. **No Dynamic Policies**: Authorization policies defined at startup; runtime policy changes require restart
5. **No Token Revocation**: Fake IDP doesn't support token revocation; tokens valid until expiration
6. **No Refresh Token Rotation**: Fake IDP returns same refresh token; production should rotate refresh tokens
7. **No MFA Support**: Fake IDP provides single-factor authentication only

### Comparison: Development vs Production

| Feature | Fake IDP (Development) | Production IDP (e.g., Azure AD) |
|---------|------------------------|----------------------------------|
| **Setup Complexity** | Minimal (3 lines of code) | High (cloud configuration, certs) |
| **External Dependencies** | None | Internet, external service |
| **Cost** | Free | Pay-per-user or subscription |
| **User Management** | Hardcoded in code | Database, admin UI |
| **Token Signing** | Symmetric (HS256) | Asymmetric (RS256) |
| **Security** | Development-only | Production-grade |
| **Token Lifetime** | Configurable (default: never expires) | Configurable (15-60 min typical) |
| **Refresh Tokens** | Supported | Supported with rotation |
| **Multi-Factor Auth** | No | Yes |
| **Audit Logging** | Basic | Comprehensive |
| **Uptime SLA** | N/A | 99.9%+ |
| **Integration Tests** | Fast, offline | Slower, requires network |

### Additional Resources

**bITdevKit Documentation**:

- [Identity Provider Feature](.bdk/docs/features-identityprovider.md)

**External Documentation**:

- [ASP.NET Core Security Overview](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [JWT.io - Token Debugger](https://jwt.io/)
- [OAuth2 Simplified](https://aaronparecki.com/oauth-2-simplified/)
- [OpenID Connect Primer](https://openid.net/developers/how-connect-works/)
