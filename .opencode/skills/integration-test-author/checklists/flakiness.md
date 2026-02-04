# Integration Test Flakiness Checklist

- Uses unique test data to avoid collisions.
- Avoids dependence on wall-clock timing or random data.
- Relies on database readiness from the fixture.
