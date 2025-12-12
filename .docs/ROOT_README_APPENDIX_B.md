# ROOT README.md - Appendix B (OpenAPI & Kiota) Draft

This file contains the updated Appendix B section with Kiota client generation replacing nswag.

---

## Appendix B: OpenAPI Specification and API Clients

The project uses **build-time OpenAPI** document generation, ensuring consistent API specifications across environments. This appendix explains how to generate and use the OpenAPI specification to create strongly-typed API clients.

### OpenAPI Document Generation

#### Build-Time Generation

The OpenAPI specification is generated automatically during compilation:

- **On build**: OpenAPI spec generated to `wwwroot/openapi.json`
- **At runtime**: Served as static file at `/openapi.json`
- **UI**: Scalar UI available at `/scalar` (Development/Container only)

This approach ensures the specification is:
- **Consistent**: Same spec in development, staging, and production
- **Versioned**: Committed to source control with code
- **Build artifact**: Available for deployment and client generation

#### Project Configuration

**Presentation.Web.Server.csproj**:
```xml
<PropertyGroup>
  <OpenApiGenerateDocuments>true</OpenApiGenerateDocuments>
  <OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)/wwwroot</OpenApiDocumentsDirectory>
  <OpenApiGenerateDocumentsOptions>--file-name openapi</OpenApiGenerateDocumentsOptions>
</PropertyGroup>

<Target Name="GenerateOpenApiAfterBuild" 
        AfterTargets="Build" 
        DependsOnTargets="GenerateOpenApiDocuments" />
```

#### Required NuGet Packages

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" />
<PackageReference Include="Microsoft.Extensions.ApiDescription.Server" />
<PackageReference Include="Scalar.AspNetCore" />
```

#### Program.cs Configuration

```csharp
// Service registration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Application pipeline (Development/Container only)
if (app.Environment.IsLocalDevelopment() || app.Environment.IsContainerized())
{
    app.MapOpenApi();    // Serves /openapi.json
    app.MapScalar();     // Serves /scalar UI
}

app.UseStaticFiles();    // Required to serve wwwroot/openapi.json
```

### Accessing the OpenAPI Specification

**Generated file location**:
```
src/Presentation.Web.Server/wwwroot/openapi.json
```

**Runtime endpoints**:
- OpenAPI JSON: `https://localhost:5001/openapi.json`
- Scalar UI: `https://localhost:5001/scalar`

**Example curl**:
```bash
curl https://localhost:5001/openapi.json -o openapi.json
```

### Generating API Clients with Kiota

[Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview) is Microsoft's OpenAPI-based API client generator that produces idiomatic, strongly-typed clients for multiple languages.

#### Why Kiota?

**Advantages over nswag**:
- **Microsoft-supported**: Active development and first-party support
- **Modern patterns**: Leverages latest language features (C# 12+, .NET 8+)
- **Lightweight**: Generated code is minimal and readable
- **Fluent API**: Request builders provide IntelliSense-friendly API
- **Multiple languages**: C#, TypeScript, Python, Java, Go, PHP, Ruby
- **OpenAPI 3.0**: Full support for modern OpenAPI specifications

#### Installing Kiota

**Global tool installation**:
```bash
dotnet tool install --global Microsoft.OpenApi.Kiota
```

**Verify installation**:
```bash
kiota --version
```

**Update to latest**:
```bash
dotnet tool update --global Microsoft.OpenApi.Kiota
```

### Generating C# Client

#### Basic Generation

```bash
kiota generate \
  --openapi src/Presentation.Web.Server/wwwroot/openapi.json \
  --language CSharp \
  --class-name GettingStartedApiClient \
  --namespace BridgingIT.DevKit.Examples.GettingStarted.Client \
  --output ./generated/csharp
```

#### Advanced Generation with Options

```bash
kiota generate \
  --openapi https://localhost:5001/openapi.json \
  --language CSharp \
  --class-name GettingStartedApiClient \
  --namespace BridgingIT.DevKit.Examples.GettingStarted.Client \
  --output ./generated/csharp \
  --include-path "/api/coremodule/**" \
  --exclude-backward-compatible \
  --serializer Json \
  --deserializer Json \
  --clean-output
```

**Options explained**:
- `--openapi`: Path or URL to OpenAPI specification
- `--language`: Target language (CSharp, TypeScript, Java, Python, etc.)
- `--class-name`: Name of the root client class
- `--namespace`: Namespace for generated types
- `--output`: Output directory for generated code
- `--include-path`: Include only specific paths (glob pattern)
- `--exclude-backward-compatible`: Exclude deprecated operations
- `--serializer`/`--deserializer`: Serialization format (Json, Form, Text, Multipart)
- `--clean-output`: Clean output directory before generation

#### Generated Client Structure

```
generated/csharp/
??? GettingStartedApiClient.cs          # Root client class
??? Api/
?   ??? Coremodule/
?       ??? Customers/
?           ??? CustomersRequestBuilder.cs
?           ??? Item/
?           ?   ??? WithIdItemRequestBuilder.cs
?           ??? Models/
?               ??? CustomerModel.cs
?               ??? CustomerStatus.cs
??? Models/
    ??? (shared models)
```

#### Using the Generated Client

**Install required packages**:
```bash
dotnet add package Microsoft.Kiota.Abstractions
dotnet add package Microsoft.Kiota.Http.HttpClientLibrary
dotnet add package Microsoft.Kiota.Serialization.Json
```

**Basic usage**:
```csharp
using BridgingIT.DevKit.Examples.GettingStarted.Client;
using Microsoft.Kiota.Http.HttpClientLibrary;

// Create HTTP client with authentication
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add(
    "Authorization", 
    "Bearer YOUR_JWT_TOKEN");

// Create Kiota request adapter
var requestAdapter = new HttpClientRequestAdapter(
    new AnonymousAuthenticationProvider(), 
    httpClient: httpClient);

// Create API client
var client = new GettingStartedApiClient(requestAdapter);

// Get all customers
var customers = await client.Api.Coremodule.Customers.GetAsync();

foreach (var customer in customers)
{
    Console.WriteLine($"{customer.FirstName} {customer.LastName}");
}

// Get customer by ID
var customerId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000");
var customer = await client.Api.Coremodule.Customers[customerId].GetAsync();

// Create new customer
var newCustomer = new CustomerModel
{
    FirstName = "Jane",
    LastName = "Doe",
    Email = "jane.doe@example.com"
};

var created = await client.Api.Coremodule.Customers.PostAsync(newCustomer);
Console.WriteLine($"Created customer: {created.Id}");

// Update customer
customer.FirstName = "Janet";
var updated = await client.Api.Coremodule.Customers[customerId].PutAsync(customer);

// Delete customer
await client.Api.Coremodule.Customers[customerId].DeleteAsync();
```

**Advanced usage with request configuration**:
```csharp
// Get with query parameters and headers
var customers = await client.Api.Coremodule.Customers.GetAsync(requestConfig =>
{
    requestConfig.QueryParameters.Filter = "status eq 'Active'";
    requestConfig.Headers.Add("X-Custom-Header", "value");
});

// Post with retry policy
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

var created = await client.Api.Coremodule.Customers.PostAsync(
    newCustomer,
    requestConfig =>
    {
        requestConfig.Options.Add(
            new RetryHandlerOption
            {
                MaxRetry = 3,
                Delay = 2
            });
    },
    cts.Token);
```

### Generating TypeScript Client

```bash
kiota generate \
  --openapi src/Presentation.Web.Server/wwwroot/openapi.json \
  --language TypeScript \
  --class-name GettingStartedApiClient \
  --output ./generated/typescript
```

**Install dependencies**:
```bash
npm install @microsoft/kiota-abstractions
npm install @microsoft/kiota-http-fetchlibrary
npm install @microsoft/kiota-serialization-json
```

**Usage in TypeScript**:
```typescript
import { GettingStartedApiClient } from './generated/typescript';
import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';

// Create client
const adapter = new FetchRequestAdapter();
adapter.baseUrl = 'https://localhost:5001';

const client = new GettingStartedApiClient(adapter);

// Get all customers
const customers = await client.api.coremodule.customers.get();

customers?.forEach(customer => {
  console.log(`${customer.firstName} ${customer.lastName}`);
});

// Create customer
const newCustomer = {
  firstName: 'Jane',
  lastName: 'Doe',
  email: 'jane.doe@example.com'
};

const created = await client.api.coremodule.customers.post(newCustomer);
console.log(`Created: ${created?.id}`);
```

### Generating Python Client

```bash
kiota generate \
  --openapi src/Presentation.Web.Server/wwwroot/openapi.json \
  --language Python \
  --class-name GettingStartedApiClient \
  --output ./generated/python
```

**Install dependencies**:
```bash
pip install microsoft-kiota-abstractions
pip install microsoft-kiota-http
pip install microsoft-kiota-serialization-json
```

**Usage in Python**:
```python
from generated.python import GettingStartedApiClient
from kiota_http.httpx_request_adapter import HttpxRequestAdapter

# Create client
adapter = HttpxRequestAdapter()
adapter.base_url = "https://localhost:5001"

client = GettingStartedApiClient(adapter)

# Get all customers
customers = await client.api.coremodule.customers.get()

for customer in customers:
    print(f"{customer.first_name} {customer.last_name}")

# Create customer
new_customer = {
    "first_name": "Jane",
    "last_name": "Doe",
    "email": "jane.doe@example.com"
}

created = await client.api.coremodule.customers.post(new_customer)
print(f"Created: {created.id}")
```

### CI/CD Integration

#### Regenerate on Build

**Add to .csproj** (after OpenAPI generation):
```xml
<Target Name="GenerateKiotaClient" AfterTargets="GenerateOpenApiAfterBuild">
  <Exec Command="kiota generate --openapi $(MSBuildProjectDirectory)/wwwroot/openapi.json --language CSharp --output $(MSBuildProjectDirectory)/../clients/csharp" />
</Target>
```

#### GitHub Actions Workflow

```yaml
name: Generate API Clients

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  generate-clients:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Build project
        run: dotnet build src/Presentation.Web.Server/Presentation.Web.Server.csproj
      
      - name: Install Kiota
        run: dotnet tool install --global Microsoft.OpenApi.Kiota
      
      - name: Generate C# client
        run: |
          kiota generate \
            --openapi src/Presentation.Web.Server/wwwroot/openapi.json \
            --language CSharp \
            --class-name GettingStartedApiClient \
            --namespace BridgingIT.DevKit.Examples.GettingStarted.Client \
            --output ./clients/csharp
      
      - name: Generate TypeScript client
        run: |
          kiota generate \
            --openapi src/Presentation.Web.Server/wwwroot/openapi.json \
            --language TypeScript \
            --class-name GettingStartedApiClient \
            --output ./clients/typescript
      
      - name: Upload clients
        uses: actions/upload-artifact@v4
        with:
          name: api-clients
          path: clients/
```

### Best Practices

#### Version Your OpenAPI Spec

```bash
# Tag releases with API version
git tag -a v1.0.0-api -m "API version 1.0.0"

# Generate client for specific version
kiota generate \
  --openapi https://github.com/org/repo/raw/v1.0.0-api/openapi.json \
  --language CSharp \
  --output ./clients/v1.0.0
```

#### Handle Breaking Changes

When making breaking API changes:

1. **Version your API**: Use URL versioning `/api/v2/coremodule/customers`
2. **Maintain old clients**: Keep generated clients for previous versions
3. **Document migrations**: Provide upgrade guides for client consumers
4. **Deprecation warnings**: Mark old operations as deprecated in OpenAPI spec

#### Client Libraries

Consider publishing generated clients as NuGet packages:

```xml
<!-- GettingStartedApiClient.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <Version>1.0.0</Version>
    <Authors>BridgingIT</Authors>
    <Description>API client for GettingStarted application</Description>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Kiota.Abstractions" />
    <PackageReference Include="Microsoft.Kiota.Http.HttpClientLibrary" />
    <PackageReference Include="Microsoft.Kiota.Serialization.Json" />
  </ItemGroup>
</Project>
```

```bash
# Pack and publish
dotnet pack clients/csharp/GettingStartedApiClient.csproj
dotnet nuget push bin/Release/GettingStartedApiClient.1.0.0.nupkg \
  --source https://api.nuget.org/v3/index.json \
  --api-key YOUR_API_KEY
```

### Resources

- [Kiota Documentation](https://learn.microsoft.com/en-us/openapi/kiota/overview)
- [Kiota GitHub Repository](https://github.com/microsoft/kiota)
- [OpenAPI Specification](https://spec.openapis.org/oas/v3.1.0)
- [ASP.NET Core OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/)

---

## Summary

The combination of build-time OpenAPI generation and Kiota client generation provides:
- **Type safety**: Strongly-typed clients catch errors at compile time
- **Maintainability**: Clients automatically sync with API changes
- **Developer experience**: IntelliSense and auto-completion in IDEs
- **Multi-language support**: Generate clients for any supported language
- **CI/CD integration**: Automated client generation in build pipelines

For additional API testing tools, see [Appendix C: Testing Strategy](#appendix-c-testing-strategy).
