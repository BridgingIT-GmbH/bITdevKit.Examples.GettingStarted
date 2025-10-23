// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests;

using System.Reflection;
using BridgingIT.DevKit.Domain.Model;
using Dumpify;
using NetArchTest.Rules;
using Shouldly;

public class TypesFixture
{
    public Types Types { get; } = Types.FromPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
}

[UnitTest("Architecture")]
public class ArchitectureTests : IClassFixture<TypesFixture>
{
    private readonly ITestOutputHelper output;
    private readonly TypesFixture fixture;

    public ArchitectureTests(ITestOutputHelper output, TypesFixture fixture)
    {
        this.output = output;
        this.fixture = fixture;
    }

    //[Fact]
    //public void ApplicationCommand_Should_ResideInApplication()
    //{
    //    var result = this.fixture.Types
    //        .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
    //            .ImplementInterface(typeof(ICommandRequest<>)).And().DoNotResideInNamespace("BridgingIT.DevKit.Application")
    //        .Should().ResideInNamespaceContaining(
    //            "BridgingIT.DevKit.Examples.GettingStarted.Application").GetResult();

    //    result.IsSuccessful.ShouldBeTrue("Application command should reside in Application.\n" + result.FailingTypes.DumpText());
    //}

    //[Fact]
    //public void ApplicationQuery_Should_ResideInApplication()
    //{
    //    var result = this.fixture.Types
    //        .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
    //            .ImplementInterface(typeof(IQueryRequest<>)).And().DoNotResideInNamespace("BridgingIT.DevKit.Application")
    //        .Should().ResideInNamespaceContaining(
    //            "BridgingIT.DevKit.Examples.GettingStarted.Application").GetResult();

    //    result.IsSuccessful.ShouldBeTrue("Application query should reside in Application.\n" + result.FailingTypes.DumpText());
    //}

    [Fact]
    public void Application_ShouldNot_HaveDependencyOnInfrastructure()
    {
        var result = this.fixture.Types
            .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
                .ResideInNamespace("BridgingIT.DevKit.Examples.GettingStarted.Application")
            .ShouldNot().HaveDependencyOnAny(
                "BridgingIT.DevKit.Examples.GettingStarted.Infrastructure").GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain layer has not allowed dependencies.\n" + result.FailingTypes.DumpText());
    }

    [Fact]
    public void Application_ShouldNot_HaveDependencyOnPresentation()
    {
        var result = this.fixture.Types
            .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
                .ResideInNamespace("BridgingIT.DevKit.Examples.GettingStarted.Application")
            .ShouldNot().HaveDependencyOnAny(
                "BridgingIT.DevKit.Examples.GettingStarted.Presentation").GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain layer has not allowed dependencies.\n" + result.FailingTypes.DumpText());
    }

    [Fact]
    public void Domain_ShouldNot_HaveDependencyOnApplication()
    {
        var result = this.fixture.Types
            .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
                .ResideInNamespace("BridgingIT.DevKit.Examples.GettingStarted.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "BridgingIT.DevKit.Examples.GettingStarted.Application").GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain layer has not allowed dependencies.\n" + result.FailingTypes.DumpText());
    }

    [Fact]
    public void Domain_ShouldNot_HaveDependencyOnInfrastructure()
    {
        var result = this.fixture.Types
            .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
                .ResideInNamespace("BridgingIT.DevKit.Examples.GettingStarted.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "BridgingIT.DevKit.Examples.GettingStarted.Infrastructure").GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain layer has not allowed dependencies.\n" + result.FailingTypes.DumpText());
    }

    [Fact]
    public void Domain_ShouldNot_HaveDependencyOnPresentation()
    {
        var result = this.fixture.Types
            .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
                .ResideInNamespace("BridgingIT.DevKit.Examples.GettingStarted.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "BridgingIT.DevKit.Examples.GettingStarted.Presentation").GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain layer has not allowed dependencies.\n" + result.FailingTypes.DumpText());
    }

    [Fact]
    public void Infrastructure_ShouldNot_HaveDependencyOnPresentation()
    {
        var result = this.fixture.Types
            .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
                .ResideInNamespace("BridgingIT.DevKit.Examples.GettingStarted.Infrastructure")
            .ShouldNot().HaveDependencyOnAny(
                "BridgingIT.DevKit.Examples.GettingStarted.Presentation").GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain layer has not allowed dependencies.\n" + result.FailingTypes.DumpText());
    }

    [Fact]
    public void DomainEntity_ShouldNot_HavePublicConstructor()
    {
        var result = this.fixture.Types
            .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
                .ImplementInterface<IEntity>()
            .ShouldNot().HavePublicConstructor().GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain entity should not have a public constructor.\n" + result.FailingTypes.DumpText());
    }

    [Fact]
    public void DomainEntity_Should_HaveParameterlessConstructor()
    {
        var result = this.fixture.Types
            .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
                .ImplementInterface<IEntity>()
            .Should().HaveParameterlessConstructor().GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain entity should have a parameterless constructor.\n" + result.FailingTypes.DumpText());
    }

    [Fact]
    public void DomainValueObject_ShouldNot_HavePublicConstructor()
    {
        var result = this.fixture.Types
            .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
                .Inherit<ValueObject>()
            .ShouldNot().HavePublicConstructor().GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain valueobjects should not have a public constructor.\n" + result.FailingTypes.DumpText());
    }

    [Fact]
    public void DomainValueObject_Should_HaveParameterlessConstructor()
    {
        var result = this.fixture.Types
            .That().HaveNameStartingWith("BridgingIT.DevKit.Examples.GettingStarted").And()
                .Inherit<ValueObject>()
            .Should().HaveParameterlessConstructor().GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain valueobject should have a parameterless constructor.\n" + result.FailingTypes.DumpText());
    }

    //public void Test2()
    //{
    //    var result = fixture.Types.InCurrentDomain()
    //              .Slice()
    //              .ByNamespacePrefix("BridgingIT.DevKit.Examples.GettingStarted")
    //              .Should()
    //              .NotHaveDependenciesBetweenSlices()
    //              .GetResult();
    //}
}
