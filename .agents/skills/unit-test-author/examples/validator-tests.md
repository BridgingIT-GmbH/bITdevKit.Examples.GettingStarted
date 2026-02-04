# Example: Validator tests with FluentValidation.TestHelper

Goal: assert validators report errors for missing or invalid data and accept valid inputs.

Pattern:

- Create the validator instance.
- Use TestValidate on the command.
- Assert with ShouldHaveValidationErrorFor and ShouldNotHaveAnyValidationErrors.

Reference pattern:

- `tests/Modules/CoreModule/CoreModule.UnitTests/Application/Commands/CustomerCreateCommandValidatorTests.cs`

CORRECT: Test the specific property with ShouldHaveValidationErrorFor.
WRONG: Catching exceptions for expected validation errors.
