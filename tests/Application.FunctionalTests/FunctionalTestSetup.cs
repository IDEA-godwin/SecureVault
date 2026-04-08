using Microsoft.Extensions.DependencyInjection;

namespace SecureVault.Application.FunctionalTests;

[SetUpFixture]
public class FunctionalTestSetup
{
    internal static IServiceScopeFactory ScopeFactory { get; private set; } = null!;
    internal static DatabaseResetter? DbResetter { get; private set; }

    private static WebApiFactory? _factory;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Connection string for SQL Server running in Docker on Linux
        // For local development, you can run: docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
        var connectionString = "Server=localhost,1433;Database=SecureVaultDbTest;User Id=sa;Password=YourPassword123!;Encrypt=false;TrustServerCertificate=true;";

        _factory = new WebApiFactory(connectionString);
        ScopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        DbResetter = await DatabaseResetter.CreateAsync(connectionString);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (DbResetter is not null) await DbResetter.DisposeAsync();
        if (_factory is not null) await _factory.DisposeAsync();
    }
}
