// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Infrastructure.EntityFramework;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

[IntegrationTest("Infrastructure.EntityFramework")]
[Collection(nameof(SqlServerTestContainerCollection))]
public class CoreModuleDbContextTests
{
    private readonly SqlServerTestFixture fixture;
    private readonly ITestOutputHelper output;

    public CoreModuleDbContextTests(SqlServerTestFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        this.output = output;
        this.fixture.Attach(output);
    }

    /// <summary>
    /// Performs a persistence round-trip for a Customer aggregate including value objects.
    /// </summary>
    [Fact]
    public async Task Customer_RoundTrip_PersistsEntity()
    {
        this.fixture.SkipIfUnavailable();

        // Arrange
        await using var ctx = new CoreModuleDbContext(this.fixture.Options);
        var numberResult = CustomerNumber.Create("CUS-2026-100000");
        var number = numberResult.Value;
        var customerResult = Customer.Create("John", "Doe", "john.doe@example.com", number);
        this.output.WriteLine($"[Arrange] Entity create result success={customerResult.IsSuccess}");
        var customer = customerResult.Value;

        // Act
        ctx.Customers.Add(customer);
        await ctx.SaveChangesAsync();
        this.output.WriteLine($"[Act] Entity saved with Id={customer.Id}");
        var loaded = await ctx.Customers.AsNoTracking().FirstAsync(c => c.Id == customer.Id);
        this.output.WriteLine("[Act] Entity reloaded from DB");

        // Assert
        loaded.Email.Value.ShouldBe("john.doe@example.com");
        loaded.Number.Value.ShouldBe("CUS-2026-100000");
        loaded.FirstName.ShouldBe("John");
        loaded.LastName.ShouldBe("Doe");
    }

    /// <summary>
    /// Validates the customer number sequence exists in the EF model with expected start value.
    /// </summary>
    [Fact]
    public async Task Customer_NumbersSequence_Exists()
    {
        this.fixture.SkipIfUnavailable();

        // Arrange
        await using var ctx = new CoreModuleDbContext(this.fixture.Options);
        this.output.WriteLine("[Arrange] DbContext created");

        // Act
        var sequence = ctx.Model.FindSequence(CodeModuleConstants.CustomerNumberSequenceName);
        this.output.WriteLine(sequence is null
            ? "[Act] Sequence NOT found"
            : $"[Act] Sequence found Name={sequence.Name} StartValue={sequence.StartValue}");

        // Assert
        sequence.ShouldNotBeNull();
        sequence.StartValue.ShouldBe(100000);
    }
}