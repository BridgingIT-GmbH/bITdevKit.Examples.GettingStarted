---
description: 'C# and .NET code review instructions using GitHub Copilot'
applyTo: '**'
excludeAgent: ["coding-agent"]
---

# C# and .NET Code Review Instructions

Comprehensive code review guidelines for GitHub Copilot focused on C# and .NET development. These instructions follow best practices and provide a structured approach to code quality, security, testing, and architecture review.

## Review Priorities

When performing a code review, prioritize issues in the following order:

### 🔴 CRITICAL (Block merge)
- **Security**: Vulnerabilities, exposed secrets, authentication/authorization issues
- **Correctness**: Logic errors, data corruption risks, race conditions, null reference exceptions
- **Breaking Changes**: API contract changes without versioning, public interface modifications
- **Data Loss**: Risk of data loss or corruption
- **Resource Management**: Undisposed IDisposable objects, memory leaks in long-running processes

### 🟡 IMPORTANT (Requires discussion)
- **Code Quality**: Severe violations of SOLID principles, excessive duplication
- **Test Coverage**: Missing tests for critical paths or new functionality
- **Performance**: Obvious performance bottlenecks (synchronous I/O, boxing/unboxing)
- **Architecture**: Violations of layer boundaries, cross-layer dependencies
- **Async/Await**: Blocking on async code (`.Result`, `.Wait()`), missing async propagation

### 🟢 SUGGESTION (Non-blocking improvements)
- **Readability**: Poor naming, complex expressions that could be simplified
- **Optimization**: Opportunities for Span<T>, ValueTask<T>, or collection expressions
- **Best Practices**: Minor deviations from C# conventions or .NET idioms
- **Documentation**: Missing XML documentation comments for public APIs
- **Modern C#**: Opportunities to use pattern matching, records, file-scoped namespaces

## General Review Principles

When performing a code review, follow these principles:

1. **Be specific**: Reference exact lines, files, and provide concrete examples
2. **Provide context**: Explain WHY something is an issue and the potential impact
3. **Suggest solutions**: Show corrected code when applicable, not just what's wrong
4. **Be constructive**: Focus on improving the code, not criticizing the author
5. **Recognize good practices**: Acknowledge well-written code and smart solutions
6. **Be pragmatic**: Not every suggestion needs immediate implementation
7. **Group related comments**: Avoid multiple comments about the same topic

## Code Quality Standards

When performing a code review, check for:

### Clean Code
- Descriptive and meaningful names following C# conventions (PascalCase for classes/methods/properties, camelCase for parameters/locals)
- Single Responsibility Principle: each class/method does one thing well
- DRY (Don't Repeat Yourself): no code duplication
- Methods should be small and focused (ideally < 20-30 lines)
- Avoid deeply nested code (max 3-4 levels); use guard clauses and early returns
- Avoid magic numbers and strings (use `const` or `readonly` fields)
- Use expression-bodied members for accessors and properties
- **Mandatory**: File-scoped namespaces
- **Mandatory**: Using directives inside namespace
- **Mandatory**: Use `var` for all variable declarations
- Code should be self-documenting; XML comments required for all public APIs
- No double empty lines; single empty line between methods and logical sections

### Example
```csharp
// WRONG: Block-scoped namespace (has curly braces), poor naming, magic numbers, no XML comments
namespace MyApp.Services {
    public class Calc {
        public decimal Do(decimal x, decimal y) {
            if (x > 100) return y * 0.15m;
            return y * 0.10m;
        }
    }
}

// GOOD: File-scoped namespace (ends with semicolon), clear naming, constants, XML comments, var usage, pattern matching
namespace MyApp.Services;

/// <summary>
/// Provides discount calculation services.
/// </summary>
public class DiscountCalculator
{
    private const decimal PremiumThreshold = 100m;
    private const decimal PremiumDiscountRate = 0.15m;
    private const decimal StandardDiscountRate = 0.10m;

    /// <summary>
    /// Calculates the discount amount based on order total and item price.
    /// </summary>
    /// <param name="orderTotal">The total amount of the order.</param>
    /// <param name="itemPrice">The price of the individual item.</param>
    /// <returns>The calculated discount amount.</returns>
    public decimal CalculateDiscount(decimal orderTotal, decimal itemPrice) =>
        orderTotal switch
        {
            > PremiumThreshold => itemPrice * PremiumDiscountRate,
            _ => itemPrice * StandardDiscountRate
        };
}
```

### C# Language Features and .editorconfig Compliance
- Adhere to .editorconfig rules for C# coding style
- **Mandatory**: File-scoped namespaces (ends with `;` not `{}`)
  - CORRECT: `namespace MyApp.Services;` (file-scoped)
  - INCORRECT: `namespace MyApp.Services { }` (block-scoped)
  - **Verification**: Look for semicolon after namespace declaration, NOT opening brace
- **Mandatory**: Using directives inside namespace
- **Mandatory**: Use `var` for all local variables
- Prefer pattern matching over type checks and casts
- Use collection expressions (C# 12+) where appropriate: `[1, 2, 3]`
- Leverage records for immutable data types
- Use primary constructors (C# 12+) for simple classes
- Prefer `string.IsNullOrWhiteSpace()` over `string.IsNullOrEmpty()`
- Use null-conditional (`?.`) and null-coalescing (`??`, `??=`) operators
- Prefer `is` pattern matching over equality checks for null: `if (obj is null)`
- Use `this.` qualifier for fields, methods, properties, and events
- Prefer simple using statements, declarations (C# 8+) for IDisposable objects

### Error Handling
- Use specific exception types, not generic `Exception` when possible
- Avoid catching general exceptions; catch specific exceptions only
- Validate inputs early using guard clauses
- Never catch and swallow exceptions without logging
- In async code, avoid catching and re-throwing without preserving stack trace
- Dispose resources properly using `using` statements or declarations
- Exceptions thrown in methods must not be documented as XML comments.

### Example
```csharp
// WRONG: Silent failure, catching all exceptions, no XML comments
public void ProcessData(int id)
{
    try
    {
        var data = this.Load(id);
        data.Process();
    }
    catch
    {
        // Silent failure
    }
}

// GOOD: Explicit error handling with XML comments
namespace MyApp.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Processes data operations.
/// </summary>
public class DataProcessor
{
    private readonly IDataStore store;
    private readonly ILogger<DataProcessor> logger;

    /// <summary>
    /// Processes data by identifier.
    /// </summary>
    /// <param name="id">The data identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if processing succeeded; otherwise, false.</returns>
    public async Task<bool> ProcessDataAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("ID cannot be empty", nameof(id));
        }

        var data = await this.store.LoadAsync(id, cancellationToken);
        if (data is null)
        {
            this.logger.LogWarning("Data {Id} not found", id);
            return false;
        }

        try
        {
            await data.ProcessAsync(cancellationToken);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            this.logger.LogWarning(ex, "Processing failed for data {Id}", id);
            return false;
        }
    }
}
```

## Security Review

When performing a code review, check for security issues:

- **Sensitive Data**: No passwords, API keys, connection strings, tokens, or PII in code or logs
- **Configuration**: Use configuration systems for sensitive data (not hardcoded values)
- **Input Validation**: All user inputs must be validated
- **SQL Injection**: Always use parameterized queries (never string concatenation)
- **Cryptography**: Use System.Security.Cryptography APIs, never roll your own
- **Dependency Security**: Check NuGet packages for known vulnerabilities using `dotnet list package --vulnerable`

### Example
```csharp
// WRONG: Exposed secret in code
namespace MyApp.Services;

public class ApiClient
{
    private const string ApiKey = "sk_live_abc123xyz789"; // NEVER DO THIS!
}

// GOOD: Use configuration with XML comments
namespace MyApp.Services;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Client for external API communication.
/// </summary>
public class ApiClient
{
    private readonly string apiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClient"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <exception cref="InvalidOperationException">Thrown when API key is not configured.</exception>
    public ApiClient(IConfiguration configuration)
    {
        this.apiKey = configuration["ExternalApi:ApiKey"]
            ?? throw new InvalidOperationException("API key not configured");
    }
}
```

## Testing Standards

When performing a code review, verify test quality:

- **Coverage**: Critical paths and new functionality must have tests
- **Test Names**: Descriptive names using clear naming conventions (e.g., `Should_ExpectedBehavior_When_Condition`)
- **Test Structure**: Clear Arrange-Act-Assert pattern
- **Independence**: Tests should not depend on each other or shared mutable state
- **Assertions**: Use specific assertions from xUnit or assertion libraries
- **Edge Cases**: Test boundary conditions, null values, empty collections, Guid.Empty
- **Mock Appropriately**: Mock external dependencies, not the code under test
- **Async Tests**: All async tests should use `async Task` and properly await

### Example
```csharp
// WRONG: Vague name, generic assertion
[Fact]
public void Test1()
{
    var result = Calculate(5, 10);
    Assert.True(result > 0);
}

// GOOD: Clear naming, specific assertions, AAA pattern
namespace MyApp.Tests;

using Xunit;

public class DiscountCalculatorTests
{
    [Fact]
    public void CalculateDiscount_Should_ApplyTenPercentDiscount_When_OrderTotalIsUnder100()
    {
        // Arrange
        var calculator = new DiscountCalculator();
        var orderTotal = 50m;
        var itemPrice = 20m;

        // Act
        var discount = calculator.CalculateDiscount(orderTotal, itemPrice);

        // Assert
        Assert.Equal(2.00m, discount);
    }

    [Theory]
    [InlineData(50, 20, 2.00)]
    [InlineData(101, 20, 3.00)]
    public void CalculateDiscount_Should_ApplyCorrectRate_When_GivenVariousOrderTotals(
        decimal orderTotal,
        decimal itemPrice,
        decimal expectedDiscount)
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act
        var discount = calculator.CalculateDiscount(orderTotal, itemPrice);

        // Assert
        Assert.Equal(expectedDiscount, discount);
    }
}
```

## Performance Considerations

When performing a code review, check for performance issues:

- **Async/Await**: Use async methods for I/O operations; avoid blocking with `.Result` or `.Wait()`
- **Algorithms**: Appropriate time/space complexity for the use case
- **Resource Management**: Proper disposal of IDisposable objects
- **String Operations**: Use StringBuilder for concatenation in loops
- **Collections**: Use appropriate collection types (`List<T>`, `HashSet<T>`, `Dictionary<TKey, TValue>`)
- **Boxing**: Avoid unnecessary boxing/unboxing of value types
- **Span<T>**: Use Span<T> and Memory<T> for high-performance scenarios

### Example
```csharp
// WRONG: Blocking on async code
public void ProcessOrder(Guid orderId)
{
    var order = this.repository.GetAsync(orderId).Result; // BLOCKS THREAD!
    order.Process();
}

// GOOD: Proper async/await
namespace MyApp.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service for processing orders.
/// </summary>
public class OrderService
{
    private readonly IOrderRepository repository;

    /// <summary>
    /// Processes an order asynchronously.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await this.repository.GetAsync(orderId, cancellationToken);
        await order.ProcessAsync(cancellationToken);
    }
}
```

## Architecture and Design

When performing a code review, verify architectural principles:

- **Separation of Concerns**: Clear boundaries between layers/modules
- **Dependency Direction**: Dependencies flow from high-level to low-level abstractions
- **Interface Segregation**: Prefer small, focused interfaces; avoid god interfaces
- **Loose Coupling**: Components should be independently testable via interfaces
- **High Cohesion**: Related functionality grouped together
- **Consistent Patterns**: Follow established patterns in the codebase
- **No Circular References**: Modules and layers must not have circular dependencies

## Documentation Standards

When performing a code review, check documentation:

- **Public APIs**: All public classes, methods, and properties must have XML documentation comments
- **Complex Logic**: Non-obvious algorithms should have explanatory comments
- **README Updates**: Update README.md when adding features or changing setup
- **Breaking Changes**: Document any breaking changes clearly in comments and commit messages
- **Param/Returns**: Document all parameters and return values using `<param>` and `<returns>` tags
- **Exceptions**: Document exceptions that may be thrown using `<exception>` tags
- **Summary**: Every public member should have a `<summary>` tag

### Example
```csharp
// WRONG: No documentation on public API
namespace MyApp.Services;

public class PaymentService
{
    public async Task<bool> ProcessAsync(PaymentRequest request)
    {
        // Implementation
    }
}

// GOOD: Comprehensive XML documentation
namespace MyApp.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides payment processing operations.
/// </summary>
public class PaymentService
{
    private readonly IPaymentGateway gateway;
    private readonly ILogger<PaymentService> logger;

    /// <summary>
    /// Processes a payment request asynchronously.
    /// </summary>
    /// <param name="request">The payment request containing transaction details.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>True if the payment was processed successfully; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <remarks>
    /// This method validates the payment request before submitting to the gateway.
    /// </remarks>
    public async Task<bool> ProcessAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Implementation
    }
}
```

## Comment Format Template

When performing a code review, use this format for comments:

```markdown
**[PRIORITY] Category: Brief title**

Detailed description of the issue or suggestion.

**Why this matters:**
Explanation of the impact or reason for the suggestion.

**Suggested fix:**
[code example if applicable]

**Reference:** [link to .editorconfig rule, documentation, or standard]
```

### Example Comments

#### Critical Issue
```markdown
**🔴 CRITICAL - Security: Sensitive Data Exposed**

The connection string on line 45 in `DatabaseService.cs` contains a password
hardcoded directly in the source code.

**Why this matters:**
Anyone with access to the repository can see credentials, creating a significant
security vulnerability.

**Suggested fix:**
```csharp
// Instead of:
private const string ConnectionString = "Server=db;User=sa;Password=Secret123!";

// Use configuration:
public class DatabaseService
{
    private readonly string connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        this.connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string not configured");
    }
}
```

**Reference:** [.NET Configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
```

#### Important Issue
```markdown
**🟡 IMPORTANT - .editorconfig: Missing file-scoped namespace declaration**

The namespace declaration in `PaymentService.cs` uses block-scoped syntax instead
of file-scoped, violating the .editorconfig rule.

**Why this matters:**
The .editorconfig specifies `csharp_style_namespace_declarations = file_scoped:error`,
which is a mandatory rule for consistency across the codebase.

**Suggested fix:**
```csharp
// Instead of:
namespace MyApp.Services {
    public class PaymentService { }
}

// Use file-scoped namespace:
namespace MyApp.Services;

public class PaymentService { }
```

**Reference:** .editorconfig line 125: `csharp_style_namespace_declarations = file_scoped:error`
```

#### Suggestion
```markdown
**🟢 SUGGESTION - Readability: Simplify nested conditionals**

The nested if statements on lines 30-45 in `OrderValidator.cs` make the control
flow hard to follow.

**Why this matters:**
Guard clauses with early returns improve readability and reduce cognitive complexity.

**Suggested fix:**
```csharp
// Instead of nested ifs:
public bool Validate(Order order)
{
    if (order is not null)
    {
        if (order.Items.Count > 0)
        {
            if (order.Total > 0)
            {
                return true;
            }
        }
    }
    return false;
}

// Use guard clauses:
public bool Validate(Order order)
{
    if (order is null)
        return false;

    if (order.Items.Count == 0)
        return false;

    if (order.Total <= 0)
        return false;

    return true;
}
```

**Reference:** [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
```

## Review Checklist

When performing a code review, systematically verify:

### Code Quality (C#/.NET)
- [ ] Code follows .editorconfig rules (file-scoped namespaces, var usage, using placement)
- [ ] Namespace is file-scoped (check for semicolon `;` after namespace, not opening brace `{`)
- [ ] Names follow C# naming conventions (PascalCase for types/methods, camelCase for parameters/locals)
- [ ] All public APIs have XML documentation comments (`<summary>`, `<param>`, `<returns>`)
- [ ] Methods are small and focused (< 30 lines ideally)
- [ ] No code duplication (DRY principle)
- [ ] Complex logic is broken into smaller, testable methods
- [ ] Error handling uses appropriate exception types
- [ ] No commented-out code or TODO without issue references
- [ ] Proper use of modern C# features (pattern matching, collection expressions)
- [ ] IDisposable objects are properly disposed (using statements/declarations)

### Security (.NET)
- [ ] No sensitive data (passwords, API keys, connection strings) in code or logs
- [ ] Input validation implemented on all user inputs
- [ ] No SQL injection vulnerabilities (parameterized queries only)
- [ ] Configuration systems used for sensitive data, not hardcoded values
- [ ] Dependencies are up-to-date and have no known vulnerabilities
- [ ] Cryptography uses System.Security.Cryptography APIs

### Testing
- [ ] New functionality has corresponding unit tests
- [ ] Tests follow clear naming conventions (e.g., `Should_ExpectedBehavior_When_Condition`)
- [ ] Tests use clear Arrange-Act-Assert structure
- [ ] Tests cover edge cases (null, empty, Guid.Empty, boundary conditions)
- [ ] Tests are independent and don't rely on shared mutable state
- [ ] Assertions are specific and meaningful
- [ ] External dependencies are mocked appropriately
- [ ] No tests that always pass or are commented out

### Performance (.NET)
- [ ] Async/await used for I/O operations; no blocking with `.Result` or `.Wait()`
- [ ] String operations use efficient methods (StringBuilder for loops, string.Join)
- [ ] CancellationToken passed through async call chains
- [ ] Proper disposal of resources (streams, connections)
- [ ] No obvious performance issues (excessive allocations, boxing)

### Architecture
- [ ] Follows established layer boundaries and separation of concerns
- [ ] No circular references between modules or layers
- [ ] Dependencies flow from high-level to low-level abstractions
- [ ] Consistent use of patterns established in the codebase
- [ ] Interfaces are small and focused (Interface Segregation)

### Documentation (XML Comments)
- [ ] Public APIs have complete XML documentation
- [ ] Complex algorithms have explanatory comments
- [ ] Exceptions documented with `<exception>` tags
- [ ] README files updated for new features or setup changes
- [ ] Breaking changes clearly documented
