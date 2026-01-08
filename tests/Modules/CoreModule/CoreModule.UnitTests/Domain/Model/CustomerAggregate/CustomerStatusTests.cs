// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Domain.Model;

[UnitTest("Domain")]
public class CustomerStatusTests
{
    /// <summary>
    /// Validates each static instance exposes expected Id and Value.
    /// </summary>
    [Theory]
    [InlineData(1, "Lead", "Lead")]
    [InlineData(2, "Active", "Active")]
    [InlineData(3, "Retired", "Retired")]
    public void StaticInstances_ShouldHaveExpectedIdsAndValues(int expectedId, string expectedValue, string instanceName)
    {
        // Arrange
        var status = instanceName switch
        {
            nameof(CoreModule.Domain.Model.CustomerStatus.Lead) => CoreModule.Domain.Model.CustomerStatus.Lead,
            nameof(CoreModule.Domain.Model.CustomerStatus.Active) => CoreModule.Domain.Model.CustomerStatus.Active,
            nameof(CoreModule.Domain.Model.CustomerStatus.Retired) => CoreModule.Domain.Model.CustomerStatus.Retired,
            _ => throw new ArgumentOutOfRangeException(instanceName)
        };

        // Assert
        status.Id.ShouldBe(expectedId);
        status.Value.ShouldBe(expectedValue);
    }

    /// <summary>
    /// Ensures equality holds for the same enumeration instance.
    /// </summary>
    [Fact]
    public void Equality_SameInstance_ShouldBeEqual()
    {
        // Arrange & Act
        var status1 = CoreModule.Domain.Model.CustomerStatus.Active;
        var status2 = CoreModule.Domain.Model.CustomerStatus.Active;

        // Assert
        status1.ShouldBe(status2);
        (status1 == status2).ShouldBeTrue();
    }

    /// <summary>
    /// Verifies inequality holds for distinct enumeration instances.
    /// </summary>
    [Theory]
    [InlineData(nameof(CustomerStatus.Active), nameof(CustomerStatus.Retired))]
    [InlineData(nameof(CustomerStatus.Lead), nameof(CustomerStatus.Active))]
    [InlineData(nameof(CustomerStatus.Lead), nameof(CustomerStatus.Retired))]
    public void Inequality_DifferentInstances_ShouldNotBeEqual(string firstName, string secondName)
    {
        // Arrange
        var first = firstName switch
        {
            nameof(CoreModule.Domain.Model.CustomerStatus.Lead) => CoreModule.Domain.Model.CustomerStatus.Lead,
            nameof(CoreModule.Domain.Model.CustomerStatus.Active) => CoreModule.Domain.Model.CustomerStatus.Active,
            nameof(CoreModule.Domain.Model.CustomerStatus.Retired) => CoreModule.Domain.Model.CustomerStatus.Retired,
            _ => throw new ArgumentOutOfRangeException(firstName)
        };

        var second = secondName switch
        {
            nameof(CoreModule.Domain.Model.CustomerStatus.Lead) => CoreModule.Domain.Model.CustomerStatus.Lead,
            nameof(CoreModule.Domain.Model.CustomerStatus.Active) => CoreModule.Domain.Model.CustomerStatus.Active,
            nameof(CoreModule.Domain.Model.CustomerStatus.Retired) => CoreModule.Domain.Model.CustomerStatus.Retired,
            _ => throw new ArgumentOutOfRangeException(secondName)
        };

        // Act & Assert
        first.ShouldNotBe(second);
        (first != second).ShouldBeTrue();
    }
}
