// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests;

public class CoreModuleTestsBase(ITestOutputHelper output, Action<IServiceCollection> additionalServices = null) : TestsBase(output, services =>
    {
        RegisterServices(services);
        additionalServices?.Invoke(services);
    })
{
    private static void RegisterServices(IServiceCollection services)
    {
        services.AddMapping().WithMapster();
        services.AddRequester().AddHandlers();
        services.AddNotifier().AddHandlers();
        services.AddTimeProvider(new DateTimeOffset(2024, 1, 1, 1, 0, 1, TimeSpan.Zero));

        services.AddInMemoryRepository(new InMemoryContext<Customer>())
            .WithSequenceNumberGenerator(CodeModuleConstants.CustomerNumberSequenceName, 100000, schema: "core")
            .WithBehavior<RepositoryLoggingBehavior<Customer>>();
    }
}
