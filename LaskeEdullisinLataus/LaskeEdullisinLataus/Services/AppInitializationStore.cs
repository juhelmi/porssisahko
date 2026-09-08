using System;
using System.IO;
using System.Text.Json;

namespace LaskeEdullisinLataus.Services;

public sealed class AppInitializationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public AppInitializationStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LaskeEdullisinLataus",
            "initialization.json");
    }

    public string FilePath { get; }

    public AppInitializationSettings LoadOrCreate()
    {
        if (!File.Exists(FilePath))
        {
            var defaults = AppInitializationSettings.CreateDefault();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var loaded = JsonSerializer.Deserialize<AppInitializationSettings>(json);

            if (loaded is null)
            {
                var defaults = AppInitializationSettings.CreateDefault();
                Save(defaults);
                return defaults;
            }

            return loaded;
        }
        catch
        {
            var defaults = AppInitializationSettings.CreateDefault();
            Save(defaults);
            return defaults;
        }
    }

    public void Save(AppInitializationSettings settings)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}

public sealed class AppInitializationSettings
{
    public decimal CurrentSocPercent { get; init; }
    public decimal TargetSocPercent { get; init; }
    public decimal BatteryCapacityKWh { get; init; }
    public decimal TotalPowerKW { get; init; }
    public decimal ChargePowerKW { get; init; }
    public decimal BaseLossWatts { get; init; }
    public decimal CurrentDependentFactor { get; init; }

    public static AppInitializationSettings CreateDefault()
    {
        return new AppInitializationSettings
        {
            CurrentSocPercent = 40m,
            TargetSocPercent = 80m,
            BatteryCapacityKWh = 77m,
            TotalPowerKW = 5.52m,
            ChargePowerKW = 5.52m - 0.3m - (5.52m * 0.055m),
            BaseLossWatts = 300m,
            CurrentDependentFactor = 0.055m
        };
    }
}