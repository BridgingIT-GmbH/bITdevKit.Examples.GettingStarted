---
name: unit-test-author
description: Guide for authoring unit tests for bITdevKit .NET applications in the Domain and Application layers. Use this when asked to write xUnit unit tests, validator tests with FluentValidation.TestHelper, Requester handler tests, or domain value object and aggregate tests.
---

# Unit Test Author

## Overview

This skill teaches how to write unit tests that match this repository's patterns for Domain and Application layers. It focuses on the Result pattern, Requester pipeline usage, FluentValidation validators, and Quartz job units.

## When to Use This Skill

- You add or change a command, query, or handler and need a unit test.
- You add or change a FluentValidation validator.
- You add or change a domain value object, aggregate method, or business rule.
- You add or change a Quartz job in the Application layer.
- You fix a bug in domain logic and need a regression unit test.

## When NOT to Use This Skill

- You are writing endpoint or HTTP tests (use an integration test skill).
- You need EF Core or WebApplicationFactory based tests.

## Prerequisites

- xUnit test project is available.
- Shouldly assertions are available.
- FluentValidation.TestHelper is available for validator tests.
- NSubstitute is available for job tests that require Quartz context.

## Instructions

### Step 1: Decide the Test Type

- Application handler test: Use IRequester and the test DI container.
- Validator test: Use FluentValidation.TestHelper.
- Domain value object test: Call Create and assert Result success and failure.
- Domain aggregate test: Call Create and change methods, assert invariants and Result failure paths.
- Job test: Call Process with a substituted IJobExecutionContext.

### Step 2: Use the Repository Test Base for Application Tests

Use the base class pattern that wires DI, mapping, Requester/Notifier, TimeProvider, and an in-memory repository.

Reference: `tests/Modules/CoreModule/CoreModule.UnitTests/CoreModuleTestsBase.cs`

### Step 3: Write the Test Using the Recipe

Use the templates in `./templates/` as a starting point.

### Step 4: Assert Behavior, Not Implementation Details

- Prefer Result success and failure signals over internal implementation details.
- Use Shouldly for clear, focused assertions.

## Templates

- `./templates/ApplicationHandlerTestsTemplate.cs`
- `./templates/ValidatorTestsTemplate.cs`
- `./templates/DomainValueObjectTestsTemplate.cs`
- `./templates/DomainAggregateTestsTemplate.cs`
- `./templates/JobTestsTemplate.cs`

## Examples

- `./examples/application-handler-via-requester.md`
- `./examples/validator-tests.md`
- `./examples/domain-value-object.md`
- `./examples/quartz-job.md`

## Checklists

- `./checklists/application-unit-tests.md`
- `./checklists/domain-unit-tests.md`
- `./checklists/quality-gates.md`

## Common Pitfalls

WRONG: Newing up a handler when you need the Requester pipeline.
CORRECT: Use IRequester from the test DI container and send the command or query.

WRONG: Tests that only assert no exception was thrown.
CORRECT: Assert Result success or failure and the relevant output data.

WRONG: Mocking repositories when an in-memory repository is already wired.
CORRECT: Use the in-memory repository for realistic, fast behavior unless you need to isolate a dependency.

WRONG: Using real time or random data that makes tests flaky.
CORRECT: Use the configured TimeProvider and deterministic data.

## References

- `tests/Modules/CoreModule/CoreModule.UnitTests/CoreModuleTestsBase.cs`
- `tests/Modules/CoreModule/CoreModule.UnitTests/Application/Commands/CustomerCreateCommandHandlerTests.cs`
- `tests/Modules/CoreModule/CoreModule.UnitTests/Application/Commands/CustomerCreateCommandValidatorTests.cs`
- `tests/Modules/CoreModule/CoreModule.UnitTests/Application/Jobs/CustomerExportJobTests.cs`
