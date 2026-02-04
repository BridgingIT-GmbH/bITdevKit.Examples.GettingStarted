# Application Unit Tests Checklist

- Uses test base to wire IRequester, mapping, and in-memory repository.
- Sends command or query via IRequester (pipeline coverage).
- Asserts Result success or failure explicitly.
- Asserts important data in the returned model.
- Avoids over-mocking repository when in-memory is sufficient.
- Uses deterministic data and TimeProvider.
