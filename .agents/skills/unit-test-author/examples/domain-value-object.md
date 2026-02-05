# Example: Domain value object tests

Goal: verify Result success and failure paths, normalization, and equality.

Suggested assertions:

- Valid input returns Success and non-null Value.
- Invalid input returns Failure with errors.
- Normalization is applied (trim, lower-case, etc.).
- Equality holds for the same atomic values.

Reference example in README:

- `README.md` EmailAddress snippet
