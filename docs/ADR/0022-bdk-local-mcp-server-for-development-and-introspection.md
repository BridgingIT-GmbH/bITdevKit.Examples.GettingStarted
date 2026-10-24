# ADR-0022: BDK Local MCP Server for DevKit Development and Runtime Introspection

## Status

Accepted

## Context

This repository is built on bITdevKit (see ADR-0021). The toolkit is large and evolves quickly: requester and notifier pipelines, repositories and behaviors, durable jobs, the outbox, logging, profiling, health checks, and module infrastructure. Two recurring problems slow down work on DevKit features:

1. **Knowledge drift**: developers and coding agents write DevKit code from memory or from stale examples. Wrong overloads, outdated registration calls, and invented behavior are common, and they are only found at build time or at runtime.
2. **Blind spots at runtime**: after a change, the only ways to see what the running application does are the browser dashboard, scrolling console output, and manual SQL queries. Problems such as a stale database schema, a wrong hosting environment, a duplicate server instance holding the port, or a job that never runs are slow to diagnose from the outside.

### Technical Requirements

- Authoritative DevKit guidance, documentation, and API signatures available while editing code
- Structured, bounded access to runtime state: health, errors, logs, metrics, profiling, jobs, modules
- Works with MCP-capable coding agents and editors without custom integration code per client
- Runtime access limited to local development; no new network surface in production
- Controlled runtime actions (for example triggering or pausing a job) separated from read-only inspection

### Business Requirements

- Faster feedback when implementing and verifying DevKit features
- Fewer defects caused by guessed APIs
- Reproducible diagnostics that can be shared as evidence instead of screenshots

### Design Challenges

- Introspection must not weaken the production posture of the application
- Runtime tools can change state (jobs, purge operations), so they need explicit scoping
- The tool must not depend on how the application was started (task, debugger, container)
- The repository must stay buildable and runnable without the MCP server

## Decision

Adopt the **BDK local MCP server** (`bdk mcp`, from the `BridgingIT.DevKit.Cli` tool) as the standard way for agents and developers to get DevKit guidance and to inspect the running application during local development.

### How It Works

1. The CLI is pinned as a local dotnet tool (`bridgingit.devkit.cli`, command `bdk`) in `.config/dotnet-tools.json` and restored with `dotnet tool restore`.
2. The MCP client starts `dotnet tool run bdk mcp` over standard input and output. The checked-in `.mcp.json` registers it as the `bdk` server with the `diagnostics`, `operations`, and `admin` toolsets.
3. The web host registers the runtime side with `AddMcp()` in `Program.cs`. In the Development environment the host starts a local named-pipe endpoint and writes a runtime descriptor under the user profile. Outside Development the descriptor and the endpoint are not created.
4. The MCP server selects a running host from the descriptors, so it finds the application regardless of whether it was started by a task, the debugger, or `dotnet run`.
5. Documentation, guidance, and API reference tools work without a running application. Runtime tools require the application to be started separately; the MCP server never starts it.
6. Runtime tools are grouped by toolset:
   - `diagnostics`: read-only inspection (project summary, health snapshot, recent errors and log queries, metrics, profiling, job lists, job runs)
   - `operations`: controlled actions such as triggering, pausing, resuming, or interrupting a job
   - `admin`: destructive maintenance such as purging logs or job runs, which requires explicit confirmation arguments

### Usage Rules

- Load BDK guidance and confirm API symbols through the MCP before writing DevKit code.
- After a DevKit change, start the application, run the MCP self-test, and verify the affected capability through the runtime tools.
- Use `diagnostics` tools by default. Use `operations` tools only when the task needs a runtime action, and `admin` tools only for an explicit maintenance request.
- Treat MCP output as evidence for debugging and review, but never as a replacement for automated tests (ADR-0013, ADR-0017).

## Rationale

1. **Authoritative knowledge**: guidance, documentation search, and API lookup come from the same version of the toolkit the repository consumes, which removes guessing about overloads and registration calls.
2. **Structured runtime evidence**: health, errors, metrics, profiling, and job state are returned as bounded, structured results that an agent or a developer can reason about, instead of free-form console text.
3. **Faster diagnosis**: typical local problems (wrong environment, missing schema objects, failed background services, jobs not running) are visible in one or two calls rather than a chain of manual checks.
4. **Standard protocol**: MCP is supported by multiple editors and agents, so there is no custom integration per tool and no lock-in to one assistant.
5. **No production exposure**: the runtime endpoint is a local named pipe that exists only in Development, so no new HTTP surface or credentials are added to deployed environments.
6. **Graduated safety**: separating `diagnostics`, `operations`, and `admin` toolsets makes read-only inspection the default and makes state-changing tools an explicit opt-in.
7. **Zero cost to the application design**: the integration is one registration call in the host and one pinned tool, with no changes to modules, handlers, or domain code (ADR-0001, ADR-0003).

## Consequences

### Positive

- DevKit work starts from verified guidance and confirmed API signatures.
- Running-application behavior can be verified directly after a change, including jobs, health, and error logs.
- Debugging sessions produce shareable, structured evidence.
- The same setup works for any MCP-capable client used by the team.

### Negative

- The runtime features depend on the host running in the Development environment. A host started in another environment (for example by a launch configuration that ignores the environment file) reports no MCP capabilities, which can look like a broken tool.
- The checked-in `.mcp.json` enables all three toolsets. Agents therefore have access to state-changing and destructive tools unless the toolset list is narrowed or the client requires confirmation.
- An additional tool version (`bridgingit.devkit.cli`) must be kept in step with the `BridgingIT.DevKit.*` package versions.
- Developers and agents must learn the toolset model and the guidance-first workflow.

### Neutral

- The MCP server only inspects and operates a host that is already running; it does not replace the dashboard, logs, or the test suite.
- Documentation and API tools work offline from the running application; runtime tools do not.
- Runtime descriptors are stored per user profile and are cleaned up with the process.

## Alternatives Considered

- **Alternative 1: Manual documentation and dashboard only**
  - Read the published docs and the local `.bdk/docs` files, and inspect the running app through the dashboard and console output.
  - Rejected because it does not give agents structured access, relies on memory for API details, and keeps diagnosis slow and manual.

- **Alternative 2: Custom diagnostic HTTP endpoints for agents**
  - Add repository-specific endpoints that expose logs, health, and job state, then call them from agent scripts.
  - Rejected because it adds an HTTP surface that needs authentication and hardening, duplicates what the DevKit already provides, and needs custom client glue per agent.

- **Alternative 3: Generic log and database access tools**
  - Give agents direct SQL access and log file reading through general-purpose MCP servers.
  - Rejected because the results are unstructured, bypass DevKit abstractions (for example job scheduling rules), and make it easy to damage local state without confirmation.

- **Alternative 4: Rely on the agent's built-in knowledge**
  - Use coding assistants without DevKit-specific tooling.
  - Rejected because knowledge of a fast-moving toolkit is frequently outdated, which leads to invented or obsolete API usage.

## Related Decisions

- [ADR-0021](0021-bitdevkit-as-the-application-foundation.md): The MCP server exposes guidance and runtime state for the toolkit adopted as the foundation
- [ADR-0015](0015-devkit-native-durable-jobs.md): Job list, run, and trigger tools operate on the DevKit Jobs subsystem
- [ADR-0016](0016-logging-observability-strategy.md): Log, error, metrics, and profiling tools read the observability data produced by the logging strategy
- [ADR-0013](0013-unit-testing-high-coverage-strategy.md): MCP verification complements, and does not replace, automated unit tests
- [ADR-0017](0017-integration-testing-strategy.md): MCP verification complements, and does not replace, integration tests

## References

- [bITdevKit Documentation Index](../../.bdk/docs/INDEX.md)
- [bITdevKit AI Agent Support](https://bridgingit-gmbh.github.io/bITdevKit/agent-support/)
- Project Documentation: `README.md` (Accelerate Agent Development with BDK MCP)
- Tool manifest: `.config/dotnet-tools.json`
- MCP client configuration: `.mcp.json`
- Host registration: `src/Presentation.Web.Server/Program.cs`

## Notes

### Host Registration

```csharp
var builder = DevKitWebApplication.CreateBuilder(args)
    .AddConfiguration()
    .AddLogging()
    .AddModules(modules => modules
        .WithModule(new CoreModuleModule()))
    .AddMcp();
```

### Client Command

```text
dotnet tool restore
dotnet tool run bdk mcp --toolset diagnostics
```

Add `operations` or `admin` to the `--toolset` list only for tasks that need them.

### Typical Verification Loop

1. Load guidance for the change and confirm the API symbols.
2. Implement the change and run the focused tests.
3. Start the application in the Development environment.
4. Run the MCP self-test, then check the project summary, health snapshot, and recent errors.
5. Exercise the affected feature (for example trigger a job) and check the result through the matching tool.

### Troubleshooting

- No runtime capabilities reported: confirm the host runs with `ASPNETCORE_ENVIRONMENT=Development`. The startup diagnostics log line shows the detected environment.
- Wrong or no runtime selected: confirm only one instance of the application is running and that it holds the expected ports.

### Implementation Files

- `.config/dotnet-tools.json`
- `.mcp.json`
- `src/Presentation.Web.Server/Program.cs`
- `README.md`
