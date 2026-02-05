---
name: integration-test-author
description: Guide for authoring integration tests for minimal API endpoints using WebApplicationFactory, authenticated HttpClient, and repository-backed persistence. Use this when asked to write endpoint tests, HttpClient tests, or fixture-based integration tests.
---

# Integration Test Author

## Overview

This skill teaches how to write endpoint integration tests that follow this repository's patterns. It focuses on WebApplicationFactory, shared fixtures, authenticated HttpClient, and persistence readiness.

## When to Use This Skill

- You add or change a minimal API endpoint.
- You change endpoint request or response contracts.
- You change authorization or authentication for endpoints.
- You want to verify validation responses and problem details.

## When NOT to Use This Skill

- You are writing domain or application unit tests.
- You are testing handlers or validators without HTTP.

## Prerequisites

- Integration test project exists.
- WebApplicationFactory is available.
- Endpoint test fixture is available.

## Instructions

### Step 1: Use the Fixture Pattern

Use the shared collection fixture that manages WebApplicationFactory, HttpClient, and authentication.

Reference: `tests/Modules/CoreModule/CoreModule.IntegrationTests/Presentation/Web/EndpointTestFixture.cs`

### Step 2: Configure Auth Once

Configure auth options in the test constructor and attach output logging.

Reference: `tests/Modules/CoreModule/CoreModule.IntegrationTests/Presentation/Web/CustomerEndpointTests.cs`

### Step 3: Seed Data via HTTP

Prefer seeding via HTTP POST so the test remains black-box and exercises full pipeline.

### Step 4: Make Requests and Assert

- Assert HTTP status codes.
- Assert key fields in the response body.
- Deserialize to the expected model and assert properties.

### Step 5: Cover Negative Paths

- 404 for missing resources.
- 400 for validation errors.
- 401 or 403 for auth failures if applicable.

## Templates

- `./templates/EndpointTestFixtureUsageTemplate.cs`
- `./templates/EndpointCrudTestsTemplate.cs`
- `./templates/AuthZTestsTemplate.cs`
- `./templates/SeedEntityHelperTemplate.cs`

## Examples

- `./examples/fixture-and-auth.md`
- `./examples/customer-crud-endpoints.md`
- `./examples/validation-problem-details.md`
- `./examples/db-readiness.md`

## Checklists

- `./checklists/test-structure.md`
- `./checklists/assertions.md`
- `./checklists/flakiness.md`
- `./checklists/auth.md`

## Common Pitfalls

WRONG: Making requests before the fixture is initialized and authenticated.
CORRECT: Attach output and configure options in the constructor before making requests.

WRONG: Asserting only on raw JSON strings.
CORRECT: Assert status codes and deserialize into the model for property assertions.

WRONG: Seeding through internal services without a good reason.
CORRECT: Seed via HTTP POST to keep the test black-box.

WRONG: Ignoring database readiness and causing flaky tests.
CORRECT: Rely on the fixture to wait for readiness before running requests.

## References

- `tests/Modules/CoreModule/CoreModule.IntegrationTests/Presentation/Web/EndpointTestFixture.cs`
- `tests/Modules/CoreModule/CoreModule.IntegrationTests/Presentation/Web/CustomerEndpointTests.cs`
