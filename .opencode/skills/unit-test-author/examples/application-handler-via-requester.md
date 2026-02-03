# Example: Application handler via IRequester

Goal: test a command handler using the Requester pipeline and the in-memory repository wiring.

Pattern:

1. Use the test base that registers mapping, requester, notifier, time provider, and in-memory repository.
2. Resolve IRequester from the test ServiceProvider.
3. Send the command and assert the Result.

Reference patterns:

- `tests/Modules/CoreModule/CoreModule.UnitTests/CoreModuleTestsBase.cs`
- `tests/Modules/CoreModule/CoreModule.UnitTests/Application/Commands/CustomerCreateCommandHandlerTests.cs`

CORRECT: Assert Result success and returned model fields.
WRONG: Asserting only that no exception was thrown.
