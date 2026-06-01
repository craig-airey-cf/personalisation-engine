using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalisationEngine.Api.Data;
using PersonalisationEngine.Api.Services.Claude;

namespace PersonalisationEngine.Tests.Infrastructure;

/// <summary>
/// WebApplicationFactory that swaps the real PostgreSQL for the Testcontainer instance
/// and replaces the real ClaudeClient with a stub that always returns a fixed recommendation.
/// </summary>
public sealed class PersonalisationEngineFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    public const string TestApiKey = "test-api-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(cfg =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:ApiKey"] = TestApiKey }));

        builder.ConfigureServices(services =>
        {
            // Replace real DbContext with one pointing at the test container
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Replace real ClaudeClient with stub — no API key needed in tests
            var claudeDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IClaudeClient));
            if (claudeDescriptor is not null)
                services.Remove(claudeDescriptor);

            services.AddScoped<IClaudeClient, StubClaudeClient>();
        });
    }

    /// <summary>Applies EF Core migrations. Call once before tests run.</summary>
    public void MigrateDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
}
