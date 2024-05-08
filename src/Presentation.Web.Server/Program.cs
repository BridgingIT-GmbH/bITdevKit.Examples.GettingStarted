using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;
using BridgingIT.DevKit.Examples.GettingStarted.Infrastructure;
using BridgingIT.DevKit.Examples.GettingStarted.Presentation;
using BridgingIT.DevKit.Presentation;
using BridgingIT.DevKit.Presentation.Web;
using Hellang.Middleware.ProblemDetails;

// ===============================================================================================
// Configure the host
var builder = WebApplication.CreateBuilder(args);
// ===v DevKit registrations v===
builder.Host.ConfigureLogging();
// ===^ DevKit registrations ^===

// ===============================================================================================
// Configure the services
// ===v DevKit registrations v===
builder.Services.AddMediatR();
builder.Services.AddMapping().WithMapster<MapperRegister>();
builder.Services.AddCommands();
builder.Services.AddQueries();

builder.Services
    .AddSqlServerDbContext<AppDbContext>(o => o
        .UseConnectionString(builder.Configuration.GetConnectionString("Default")))
    .WithDatabaseMigratorService();

builder.Services.AddEntityFrameworkRepository<Customer, AppDbContext>()
    //.WithTransactions<NullRepositoryTransaction<Customer>>()
    .WithBehavior<RepositoryTracingBehavior<Customer>>()
    .WithBehavior<RepositoryLoggingBehavior<Customer>>();
// ===^ DevKit registrations ^===

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails(o => Configure.ProblemDetails(o, true));
// builder.Services.AddProblemDetails(Configure.ProblemDetails);

// ===============================================================================================
// Configure the HTTP request pipeline
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ===v DevKit registrations v===
app.UseRequestCorrelation();
app.UseRequestLogging();
// ===^ DevKit registrations ^===

app.UseProblemDetails();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
    // this partial class is needed to set the accessibilty for the Program class to public
    // needed for endpoint testing when using the webapplicationfactory  https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0#basic-tests-with-the-default-webapplicationfactory
}