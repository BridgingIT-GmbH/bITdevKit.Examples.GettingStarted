# Domain Unit Tests Checklist

- Tests both success and failure paths of Create or change methods.
- Asserts Result errors/messages on failure.
- Verifies normalization (if applicable).
- Verifies equality semantics for value objects.
- Avoids infrastructure or persistence concerns.
