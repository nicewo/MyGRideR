using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MYG.RideR.Application.DependencyInjection;
using MYG.RideR.Infrastructure.DependencyInjection;
using MYG.RideR.Mobile.Interfaces;
using MYG.RideR.Mobile.Services;
using MYG.RideR.Mobile.ViewModels;
using MYG.RideR.Mobile.Views;

namespace MYG.RideR.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services
            .AddApplicationServices()
            .AddInfrastructureServices()
            .AddSingleton<IDashboardDataService, DashboardMockDataService>()
            .AddSingleton<AppShell>()
            .AddTransient<DashboardViewModel>()
            .AddTransient<TripsViewModel>()
            .AddTransient<StatisticsViewModel>()
            .AddTransient<SettingsViewModel>()
            .AddTransient<DashboardPage>()
            .AddTransient<TripsPage>()
            .AddTransient<StatisticsPage>()
            .AddTransient<SettingsPage>()
            .AddSingleton<IClockService, ClockService>();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
