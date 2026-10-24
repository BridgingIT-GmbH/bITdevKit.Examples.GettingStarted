# ADR-0024: Central Package Management and Versioning

## Status

Accepted

## Context

The solution consists of several projects per module (Domain, Application, Infrastructure, Presentation, tests) plus the web host and test projects. Every project references NuGet packages, and the repository also relies on .NET local tools and a specific SDK. Without a single place that controls versions, the same package can end up at different versions in different projects, which causes restore conflicts, subtle runtime differences, and noisy dependency updates.

The solution is built on bITdevKit (ADR-0021), whose `BridgingIT.DevKit.*` packages and CLI tool are released together and must stay aligned. The repository also has to remain reproducible across developer machines and pipelines.

### Technical Requirements

- One authoritative place for package versions across all projects
- Related package families (bITdevKit, Microsoft 10.0.x, OpenTelemetry) updated together
- Transitive dependencies that matter for security or compatibility can be pinned
- Developer tools (EF CLI, Kiota, the bdk CLI, and others) available at known versions without global installs
- Reproducible SDK selection and restricted package sources
- Warnings are not allowed to accumulate unnoticed

### Business Requirements

- Small, reviewable dependency updates
- Fast detection of vulnerable or outdated packages
- Predictable builds for every team member and pipeline

### Design Challenges

- Keeping the bITdevKit packages and the bdk CLI tool on the same version
- Balancing frequent updates with the stability of a shared platform version
- Avoiding hidden version drift in tests and tooling projects

## Decision

Use **NuGet Central Package Management** with `Directory.Packages.props`, pin local tools in a **tool manifest**, pin the SDK in `global.json`, and restrict restore to **nuget.org with package source mapping**.

### How It Works

1. **Central versions**: `Directory.Packages.props` enables `ManagePackageVersionsCentrally` and `CentralPackageTransitivePinningEnabled`. Project files declare `PackageReference` entries without versions. Special asset settings for build-only packages (for example `PrivateAssets` and `IncludeAssets` for EF design-time and test runner packages) are declared at the central level.
2. **Aligned families**: all `BridgingIT.DevKit.*` packages share one version (currently 10.0.114). The Microsoft packages follow one 10.0.x patch version, and the OpenTelemetry packages share one version.
3. **Aligned tooling**: the `bridgingit.devkit.cli` tool in `.config/dotnet-tools.json` uses the same version as the `BridgingIT.DevKit.*` packages (ADR-0022).
4. **Tool manifest**: `.config/dotnet-tools.json` pins tools such as `dotnet-ef`, `kiota`, `httpgenerator`, `dotnet-script`, `dotnet-outdated-tool`, `nuget-license`, `reportgenerator`, `minver-cli`, `csharpier`, `roslynator.dotnet.cli`, and the diagnostics tools, all with `rollForward` disabled. Developers run `dotnet tool restore` once.
5. **SDK pin**: `global.json` selects SDK 10.0.400 with `rollForward` set to `latestFeature`.
6. **Package sources**: `nuget.config` clears inherited sources, declares nuget.org only, and uses package source mapping.
7. **Build hygiene**: `Directory.Build.props` sets common language settings and `TreatWarningsAsErrors`, so build warnings, including NuGet warnings, fail the build.
8. **Maintenance through bdk tasks**:
   - Packages Outdated List and Packages Outdated Update (`dotnet outdated` for the solution)
   - DevKit Packages Update (`dotnet outdated --upgrade` limited to `BridgingIT.DevKit` packages)
   - Vulnerabilities Check and Vulnerabilities Check (Deep, includes transitive packages)
   - Packages Outdated Check and Export (JSON)
   - Licenses Report Generate (`nuget-license`)

### Rules

- Add or change a package version only in `Directory.Packages.props`; project files must not contain `Version` attributes.
- Update the bITdevKit packages and the `bridgingit.devkit.cli` tool together.
- Update related families as a unit; do not mix patch versions inside the Microsoft 10.0.x family.
- Pin a transitive package centrally only for a security or compatibility reason, and add a short comment that names the reason.
- Run the vulnerability check after every package update.
- Do not install required tools globally; add them to the tool manifest.

## Rationale

1. **Single source of truth**: one file controls every version, which removes drift between projects and makes version changes a one-line diff.
2. **Safe family alignment**: keeping related packages on one version avoids runtime mismatches, especially for bITdevKit, which is released as a set.
3. **Transitive control**: transitive pinning lets the repository fix vulnerable or incompatible indirect dependencies without touching each project.
4. **Reproducible tooling**: a tool manifest and an SDK pin give every machine and pipeline the same tool and SDK versions.
5. **Supply chain safety**: a single package source with source mapping reduces the chance of dependency confusion.
6. **Early problem detection**: warnings as errors plus the vulnerability and outdated checks surface issues while they are cheap to fix.
7. **Low ceremony**: the maintenance tasks are exposed through the bdk CLI and VS Code tasks, so updating does not depend on remembered command lines.

## Consequences

### Positive

- Consistent dependency versions across all projects, tests, and the host.
- Update pull requests are small and focused on `Directory.Packages.props` and the tool manifest.
- New developers get working tooling with `dotnet tool restore`.
- Security issues in direct and transitive packages are found through one routine.

### Negative

- Updating a family requires touching several entries at once and checking that all of them exist on the feed.
- A package that needs a different version in one project cannot be handled locally without a deliberate override.
- `TreatWarningsAsErrors` can turn a new analyzer or NuGet warning from an update into a failing build until it is addressed.
- Keeping the bITdevKit packages and the CLI tool in step is a manual discipline unless the DevKit update task is used.
- Documentation that quotes package versions (for example ADR-0021) becomes outdated when versions change.

### Neutral

- The pinned SDK allows newer feature bands through `latestFeature` roll-forward, so the exact SDK may differ slightly between machines.
- Local tools are restored per repository and do not affect other repositories on the machine.
- The `minver-cli` tool is available and shown through a bdk utility task, but no MinVer package currently stamps the assemblies; assembly versioning is not driven by git tags in this repository.

## Alternatives Considered

- **Alternative 1: Versions in each project file**
  - Each project declares its own package versions.
  - Rejected because versions drift between projects and every update touches many files.

- **Alternative 2: Shared MSBuild properties for versions**
  - Define version properties in `Directory.Build.props` and reference them from project files.
  - Rejected because Central Package Management is the supported NuGet mechanism and also provides transitive pinning and tooling support.

- **Alternative 3: Floating versions or version ranges**
  - Use wildcard or range versions to receive updates automatically.
  - Rejected because builds would no longer be reproducible and a package release could break the build without any change in the repository.

- **Alternative 4: Globally installed tools**
  - Document the required tools and ask developers to install them globally.
  - Rejected because tool versions would differ per machine and onboarding would depend on manual steps.

- **Alternative 5: Paket**
  - Alternative dependency manager with its own lock file and tooling.
  - Rejected because Central Package Management covers the requirement natively with the standard .NET tooling.

## Related Decisions

- [ADR-0021](0021-bitdevkit-as-the-application-foundation.md): Defines the bITdevKit packages whose versions are kept aligned
- [ADR-0022](0022-bdk-local-mcp-server-for-development-and-introspection.md): The bdk CLI tool is pinned in the tool manifest and kept at the DevKit package version
- [ADR-0023](0023-openapi-documentation-and-client-generation.md): Kiota and httpgenerator are pinned tools used for client generation
- [ADR-0013](0013-unit-testing-high-coverage-strategy.md): Test and coverage packages are versioned centrally

## References

- [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [NuGet Package Source Mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping)
- [.NET Local Tools](https://learn.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use)
- [global.json Overview](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)
- Project Documentation: `README.md`

## Notes

### Central Versions

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="BridgingIT.DevKit.Domain" Version="10.0.114" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.13" />
    <PackageVersion Include="Scalar.AspNetCore" Version="2.18.0" />
  </ItemGroup>
</Project>
```

### Project File Usage

```xml
<ItemGroup>
  <PackageReference Include="Scalar.AspNetCore" />
</ItemGroup>
```

### Source and SDK Pins

```json
{
  "sdk": {
    "version": "10.0.400",
    "rollForward": "latestFeature",
    "allowPrerelease": true
  }
}
```

### Update Routine

1. Run Packages Outdated List and review the proposed changes.
2. Apply DevKit Packages Update, then update the `bridgingit.devkit.cli` tool to the same version.
3. Update the remaining families together (Microsoft 10.0.x, OpenTelemetry).
4. Restore, build, and run the unit and integration tests.
5. Run Vulnerabilities Check (Deep) and fix findings through central pins with a comment.
6. Generate the license report when licenses matter for the release.

### Implementation Files

- `Directory.Packages.props`
- `Directory.Build.props`
- `global.json`
- `nuget.config`
- `.config/dotnet-tools.json`
- `.bdk/cli/lib/TaskRegistry.csx`
