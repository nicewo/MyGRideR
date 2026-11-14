using Microsoft.Extensions.DependencyInjection;
using MYG.RideR.Application.Abstractions;
using MYG.RideR.Application.Trips;

namespace MYG.RideR.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITripRecorder, TripRecorder>();
        return services;
    }
}
