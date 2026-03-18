namespace SITAG.Api.Tests.Infrastructure;

/// <summary>
/// Collection fixture that shares one <see cref="SitagWebApplicationFactory"/> instance
/// across all test classes in the collection. This prevents Serilog's static bootstrap
/// logger from being frozen twice.
/// </summary>
[CollectionDefinition("Integration")]
public sealed class SharedTestCollection : ICollectionFixture<SitagWebApplicationFactory> { }
