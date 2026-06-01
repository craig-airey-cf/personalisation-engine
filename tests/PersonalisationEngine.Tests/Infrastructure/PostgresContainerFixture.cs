using Testcontainers.PostgreSql;

namespace PersonalisationEngine.Tests.Infrastructure;

/// <summary>
/// Starts a single PostgreSQL container for the entire test collection,
/// shared across all integration test classes via ICollectionFixture.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("pe_test")
        .WithUsername("pe_test")
        .WithPassword("pe_test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection : ICollectionFixture<PostgresContainerFixture>;
