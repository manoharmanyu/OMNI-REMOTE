using Microsoft.Extensions.Logging;
using OmniRemote.Core.Abstractions;
using OmniRemote.Core.Catalog;
using OmniRemote.Core.Devices;
using OmniRemote.Core.Simulation;
using OmniRemote.Domain.Devices;
using OmniRemote.Infrastructure.Persistence;
using OmniRemote.Integrations.Bluetooth;
using OmniRemote.Integrations.Infrared;
using OmniRemote.Integrations.Network;
using OmniRemote.Integrations.SmartHomeHub;
using OmniRemote.Mobile.Services;
using OmniRemote.Mobile.ViewModels;
using OmniRemote.Mobile.Views;

namespace OmniRemote.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Default font configuration
            });

        // Logging
        builder.Logging.AddDebug();

        // System Services
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ISimulationRandom, SystemSimulationRandom>();
        builder.Services.AddSingleton<SimulationSettings>();

        // Catalog
        builder.Services.AddSingleton(BrandCatalog.LoadEmbedded());

        // Storage & Persistence
        var appDataDir = FileSystem.Current.AppDataDirectory;
        if (string.IsNullOrEmpty(appDataDir))
            appDataDir = Path.Combine(AppContext.BaseDirectory, "omniremote_data");
        Directory.CreateDirectory(appDataDir);

        var fileStore = new JsonFileStore(appDataDir);
        builder.Services.AddSingleton<IDeviceRepository>(fileStore);
        builder.Services.AddSingleton<IActivityRepository>(fileStore);
        builder.Services.AddSingleton<ISettingsStore>(fileStore);
        builder.Services.AddSingleton<ISecretStore, SecureStorageSecretStore>();

        // Registries & State
        builder.Services.AddSingleton<DeviceRegistry>();
        builder.Services.AddSingleton<DeviceStateStore>();
        builder.Services.AddSingleton<ActivityLog>();

        // Adapters
        builder.Services.AddSingleton<MockDeviceAdapter>();
        builder.Services.AddSingleton<IDeviceAdapter>(sp => sp.GetRequiredService<MockDeviceAdapter>());

        builder.Services.AddSingleton<IBleCentral, MobileBleCentral>();
        builder.Services.AddSingleton<BluetoothLeDeviceAdapter>();
        builder.Services.AddSingleton<IDeviceAdapter>(sp => sp.GetRequiredService<BluetoothLeDeviceAdapter>());

        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<WifiDeviceAdapter>();
        builder.Services.AddSingleton<IDeviceAdapter>(sp => sp.GetRequiredService<WifiDeviceAdapter>());

        builder.Services.AddSingleton<InfraredGatewayAdapter>();
        builder.Services.AddSingleton<IDeviceAdapter>(sp => sp.GetRequiredService<InfraredGatewayAdapter>());

        builder.Services.AddSingleton<SmartHomeHubAdapter>();
        builder.Services.AddSingleton<IDeviceAdapter>(sp => sp.GetRequiredService<SmartHomeHubAdapter>());

        // Command Router
        builder.Services.AddSingleton(new CommandRouterOptions());
        builder.Services.AddSingleton<CommandRouter>();

        // Shell
        builder.Services.AddSingleton<AppShell>();

        // ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<SelectTypeViewModel>();
        builder.Services.AddTransient<SelectBrandViewModel>();
        builder.Services.AddTransient<ConfigureRemoteViewModel>();
        builder.Services.AddTransient<RemoteControlViewModel>();
        builder.Services.AddTransient<ActivityHistoryViewModel>();
        builder.Services.AddTransient<SimulationSettingsViewModel>();

        // Views
        builder.Services.AddTransient<HomeScreen>();
        builder.Services.AddTransient<SelectTypeScreen>();
        builder.Services.AddTransient<SelectBrandScreen>();
        builder.Services.AddTransient<ConfigureRemoteScreen>();
        builder.Services.AddTransient<RemoteControlScreen>();
        builder.Services.AddTransient<ActivityHistoryScreen>();
        builder.Services.AddTransient<SimulationSettingsScreen>();

        return builder.Build();
    }
}
