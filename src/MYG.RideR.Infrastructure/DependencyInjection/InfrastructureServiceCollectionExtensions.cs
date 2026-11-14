using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using MYG.RideR.Application.Abstractions;
using MYG.RideR.Infrastructure.Persistence;

namespace MYG.RideR.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        services.AddSingleton<ITripRepository>(_ => new SqliteTripRepository(connectionString));
        return services;
    }
}
