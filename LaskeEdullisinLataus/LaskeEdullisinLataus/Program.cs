using Avalonia;
using System;
using LaskeEdullisinLataus.Services;

namespace LaskeEdullisinLataus;

sealed class Program
{
    public static AppInitializationSettings StartupSettings { get; private set; } = AppInitializationSettings.CreateDefault();
    public static AppInitializationStore SettingsStore { get; private set; } = new();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        SettingsStore = new AppInitializationStore();
        StartupSettings = SettingsStore.LoadOrCreate();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
