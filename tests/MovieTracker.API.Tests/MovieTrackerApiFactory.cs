using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace MovieTracker.API.Tests;

public class MovieTrackerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"MovieTrackerTests_{Guid.NewGuid():N}";
    private readonly string _connectionString = "mongodb://admin:admin123@localhost:27017/?authSource=admin";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["MongoDbSettings:ConnectionString"] = _connectionString,
                ["MongoDbSettings:DatabaseName"] = _databaseName,
                ["SeedSettings:Enabled"] = "false"
            };

            configBuilder.AddInMemoryCollection(testConfig);
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        var client = new MongoClient(_connectionString);
        await client.DropDatabaseAsync(_databaseName);
        await base.DisposeAsync();
    }
}
