using SecureVault.Application.Common.Interfaces;
using SecureVault.Infrastructure.Data;
using SecureVault.Infrastructure.Data.Interceptors;
using SecureVault.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("SecureVaultDb");
        Guard.Against.Null(connectionString, message: $"Connection string 'SecureVaultDb' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        // Add authentication and authorization services
        // These are needed for the BearerSecuritySchemeTransformer in OpenAPI
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorizationBuilder();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IAccountNumberGenerator, AccountNumberGenerator>();
        builder.Services.AddScoped<IAccountService, AccountService>();

        // Register background services
        builder.Services.AddHostedService<AuditLoggingBackgroundService>();
    }
}
