# Example: Quartz job unit test

Goal: call job.Process with a substituted IJobExecutionContext and assert behavior.

Pattern:

- Use NSubstitute to create IJobExecutionContext.
- Construct the job with logger factory and scope factory from DI.
- Call Process and assert the expected behavior.

Reference pattern:

- `tests/Modules/CoreModule/CoreModule.UnitTests/Application/Jobs/CustomerExportJobTests.cs`

CORRECT: Assert observable behavior, not logging output.
WRONG: Only asserting that no exception was thrown.
