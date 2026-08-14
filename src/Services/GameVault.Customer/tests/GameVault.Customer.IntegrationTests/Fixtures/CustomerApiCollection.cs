namespace GameVault.Customer.IntegrationTests.Fixtures;

/// <summary>
/// xUnit collection that shares one CustomerApiFactory (and therefore one PostgreSQL
/// container) across all test classes that belong to it. DB state is reset between
/// test classes via IAsyncLifetime.InitializeAsync → factory.ResetDatabaseAsync().
/// </summary>
[CollectionDefinition(nameof(CustomerApiCollection))]
public sealed class CustomerApiCollection : ICollectionFixture<CustomerApiFactory> { }
