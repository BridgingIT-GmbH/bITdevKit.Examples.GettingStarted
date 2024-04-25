![bITDevKit](https://raw.githubusercontent.com/bridgingIT/bITdevKit.Examples.GettingStarted/main/bITDevKit_Logo.png)
=====================================

# Architecture overview

> An application built using .NET 7 and following a Domain-Driven Design approach by using the BridgingIT DevKit. 

## Features
- Domain Model
- Commands/ Queries
- Repositories

## Frameworks
- .NET 8.0
- Entity Framework Core
- ASP.NET Core

## Getting Started

### Domain Model

```csharp
public class Customer : AggregateRoot<Guid>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

### Commands & Queries	

Command
```csharp
public class CustomerCreateCommand
    : CommandRequestBase<Customer>
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public override ValidationResult Validate() =>
        new Validator().Validate(this);

    public class Validator : AbstractValidator<CustomerCreateCommand>
    {
        public Validator()
        {
            this.RuleFor(c => c.FirstName).NotNull().NotEmpty().WithMessage("Must not be empty.");
            this.RuleFor(c => c.LastName).NotNull().NotEmpty().WithMessage("Must not be empty.");
        }
    }
}
```

Command Handler
```csharp
public class CustomerCreateCommandHandler
    : CommandHandlerBase<CustomerCreateCommand, Customer>
{
    private readonly IGenericRepository<Customer> repository;

    public CustomerCreateCommandHandler(
        ILoggerFactory loggerFactory,
        IGenericRepository<Customer> repository)
        : base(loggerFactory)
    {
        this.repository = repository;
    }

    public override async Task<CommandResponse<Customer>> Process(
        CustomerCreateCommand request,
        CancellationToken cancellationToken)
    {
        var customer = new Customer { FirstName = request.FirstName, LastName = request.LastName };
        await this.repository.UpsertAsync(customer, cancellationToken).AnyContext();

        return new CommandResponse<Customer> // TODO: use .For?
        {
            Result = customer
        };
    }
}
```

### Service Registrations ([Program.cs](./src/Presentation.Web.Server/Program.cs))

```csharp
builder.Services.AddCommands();
builder.Services.AddQueries();

builder.Services
    .AddSqlServerDbContext<CoreDbContext>(o => o
        .UseConnectionString(builder.Configuration.GetConnectionString("Default")))
    .WithDatabaseMigratorService();

```

### Test the API

Start the application (CTRL-F5) and use the following HTTP requests to test the API:
[Core-API.http](./Core-API.http)