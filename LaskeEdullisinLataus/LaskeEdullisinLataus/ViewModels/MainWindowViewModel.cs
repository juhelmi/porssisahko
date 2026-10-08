using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaskeEdullisinLataus.Services;

namespace LaskeEdullisinLataus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AppInitializationStore _initializationStore;
    private readonly PorssisahkoClient _priceClient = new();
    private readonly ChargingOptimizer _optimizer = new();

    [ObservableProperty]
    private string currentSocText = "40";

    [ObservableProperty]
    private string targetSocText = "80";

    [ObservableProperty]
    private string batteryCapacityKwhText = "77";

    [ObservableProperty]
    private string chargeStartTimeText = "21:00";

    [ObservableProperty]
    private string chargeEndTimeText = "07:00";

    [ObservableProperty]
    private string totalPowerKwText = "5.52";

    [ObservableProperty]
    private string baseLossWattsText = "300";

    [ObservableProperty]
    private string currentDependentFactorText = "0.055";

    [ObservableProperty]
    private string priceFilePathText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready.";

    [ObservableProperty]
    private string resultSummary = "Enter values and run calculation.";

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<ChargeSegmentViewModel> CheapestWindowSegments { get; } = [];

    public IAsyncRelayCommand CalculateCommand { get; }
    public IRelayCommand SaveDefaultsCommand { get; }

    public MainWindowViewModel(AppInitializationStore initializationStore, AppInitializationSettings settings)
    {
        _initializationStore = initializationStore;

        CalculateCommand = new AsyncRelayCommand(CalculateAsync, CanCalculate);
        SaveDefaultsCommand = new RelayCommand(SaveDefaults);

        CurrentSocText = settings.CurrentSocPercent.ToString(CultureInfo.InvariantCulture);
        TargetSocText = settings.TargetSocPercent.ToString(CultureInfo.InvariantCulture);
        BatteryCapacityKwhText = settings.BatteryCapacityKWh.ToString(CultureInfo.InvariantCulture);
        ChargeStartTimeText = settings.ChargeStartTimeText;
        ChargeEndTimeText = settings.ChargeEndTimeText;
        TotalPowerKwText = settings.TotalPowerKW.ToString(CultureInfo.InvariantCulture);
        BaseLossWattsText = settings.BaseLossWatts.ToString(CultureInfo.InvariantCulture);
        CurrentDependentFactorText = settings.CurrentDependentFactor.ToString(CultureInfo.InvariantCulture);
        NotifyPowerPropertiesChanged();

        StatusMessage = $"Ready. Initialization loaded from {_initializationStore.FilePath}";
    }

    public MainWindowViewModel()
        : this(new AppInitializationStore(), new AppInitializationStore().LoadOrCreate())
    {
    }

    partial void OnCurrentSocTextChanged(string value) => CalculateCommand.NotifyCanExecuteChanged();
    partial void OnTargetSocTextChanged(string value) => CalculateCommand.NotifyCanExecuteChanged();
    partial void OnBatteryCapacityKwhTextChanged(string value) => CalculateCommand.NotifyCanExecuteChanged();
    partial void OnChargeStartTimeTextChanged(string value) => CalculateCommand.NotifyCanExecuteChanged();
    partial void OnChargeEndTimeTextChanged(string value) => CalculateCommand.NotifyCanExecuteChanged();
    partial void OnTotalPowerKwTextChanged(string value)
    {
        CalculateCommand.NotifyCanExecuteChanged();
        NotifyPowerPropertiesChanged();
    }

    partial void OnBaseLossWattsTextChanged(string value)
    {
        CalculateCommand.NotifyCanExecuteChanged();
        NotifyPowerPropertiesChanged();
    }

    partial void OnCurrentDependentFactorTextChanged(string value)
    {
        CalculateCommand.NotifyCanExecuteChanged();
        NotifyPowerPropertiesChanged();
    }

    public string EffectiveChargePowerText
    {
        get
        {
            if (!TryParseDecimal(TotalPowerKwText, out var totalPowerKw) ||
                !TryParseDecimal(BaseLossWattsText, out var baseLossWatts) ||
                !TryParseDecimal(CurrentDependentFactorText, out var factor))
            {
                return "-";
            }

            var chargePowerKw = totalPowerKw - (baseLossWatts / 1000m) - (totalPowerKw * factor);
            return $"{chargePowerKw:F3} kW";
        }
    }

    public string TotalLossWattsText
    {
        get
        {
            if (!TryParseDecimal(TotalPowerKwText, out var totalPowerKw) ||
                !TryParseDecimal(BaseLossWattsText, out var baseLossWatts) ||
                !TryParseDecimal(CurrentDependentFactorText, out var factor))
            {
                return "-";
            }

            var totalLossWatts = baseLossWatts + (totalPowerKw * 1000m * factor);
            return $"{totalLossWatts:F1} W";
        }
    }

    private bool CanCalculate() => !IsBusy;

    private async Task CalculateAsync()
    {
        if (!TryParseInputs(out var input, out var validationMessage))
        {
            StatusMessage = validationMessage;
            return;
        }

        IsBusy = true;
        CalculateCommand.NotifyCanExecuteChanged();

        try
        {
            StatusMessage = string.IsNullOrWhiteSpace(PriceFilePathText)
                ? "Fetching latest electricity prices..."
                : $"Loading electricity prices from {PriceFilePathText}...";

            var priceSlots = string.IsNullOrWhiteSpace(PriceFilePathText)
                ? await _priceClient.GetLatestPricesAsync(CancellationToken.None)
                : await _priceClient.GetPricesFromFileAsync(PriceFilePathText.Trim(), CancellationToken.None);
            var futureSlots = priceSlots
                .Where(x => x.EndDate > DateTimeOffset.UtcNow)
                .OrderBy(x => x.StartDate)
                .ToList();

            var rangedSlots = futureSlots
                .Where(x => IsWithinRange(x.StartDate.ToLocalTime().TimeOfDay, input.ChargeWindowStart, input.ChargeWindowEnd))
                .ToList();

            if (rangedSlots.Count == 0)
            {
                StatusMessage = "No price slots found inside the selected charging time range.";
                return;
            }

            var result = _optimizer.FindCheapestContinuousWindow(input, rangedSlots);

            if (!result.Success)
            {
                CheapestWindowSegments.Clear();
                ResultSummary = "No feasible charging window found.";
                StatusMessage = result.ErrorMessage;
                return;
            }

            CheapestWindowSegments.Clear();
            foreach (var segment in result.Segments)
            {
                CheapestWindowSegments.Add(new ChargeSegmentViewModel(
                    segment.StartLocal,
                    segment.EndLocal,
                    segment.PriceCentsPerKWh,
                    segment.GridEnergyKWh,
                    segment.CostEur));
            }

            ResultSummary =
                $"Cheapest start: {result.StartLocal:dd.MM.yyyy HH:mm}\n" +
                $"End: {result.EndLocal:dd.MM.yyyy HH:mm}\n" +
                $"Charge duration: {result.RequiredDurationHours:F2} h\n" +
                $"Allowed time: {input.ChargeWindowStart:hh\\:mm} - {input.ChargeWindowEnd:hh\\:mm}\n" +
                $"Total power: {input.TotalPowerKW:F2} kW\n" +
                $"Effective charge power: {input.ChargePowerKW:F2} kW\n" +
                $"Battery energy needed: {result.BatteryEnergyNeededKWh:F2} kWh\n" +
                $"Grid energy used: {result.GridEnergyUsedKWh:F2} kWh\n" +
                $"Total cost: {result.TotalCostEur:F2} EUR";

            var priceSource = string.IsNullOrWhiteSpace(PriceFilePathText) ? "the live API" : "the local JSON file";
            StatusMessage = $"Calculation completed using {rangedSlots.Count} ranged future price slots from {priceSource}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Calculation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            CalculateCommand.NotifyCanExecuteChanged();
        }
    }

    private bool TryParseInputs(out ChargingInput input, out string error)
    {
        input = default;
        error = string.Empty;

        if (!TryParseDecimal(CurrentSocText, out var currentSoc) || currentSoc < 0m || currentSoc > 100m)
        {
            error = "Current SOC must be between 0 and 100.";
            return false;
        }

        if (!TryParseDecimal(TargetSocText, out var targetSoc) || targetSoc <= currentSoc || targetSoc > 100m)
        {
            error = "Target SOC must be greater than current SOC and at most 100.";
            return false;
        }

        if (!TryParseDecimal(BatteryCapacityKwhText, out var batteryCapacityKwh) || batteryCapacityKwh <= 0m)
        {
            error = "Battery capacity must be a positive number.";
            return false;
        }

        if (!TryParseTimeOfDay(ChargeStartTimeText, out var chargeWindowStart))
        {
            error = "Charge start time must be in HH:mm format (for example 21:00).";
            return false;
        }

        if (!TryParseTimeOfDay(ChargeEndTimeText, out var chargeWindowEnd))
        {
            error = "Charge end time must be in HH:mm format (for example 07:00).";
            return false;
        }

        if (!TryParseDecimal(TotalPowerKwText, out var totalPowerKw) || totalPowerKw <= 0m)
        {
            error = "Total power (kW) must be a positive number.";
            return false;
        }

        if (!TryParseDecimal(BaseLossWattsText, out var baseLossWatts) || baseLossWatts < 0m)
        {
            error = "Base loss (W) must be zero or positive.";
            return false;
        }

        if (!TryParseDecimal(CurrentDependentFactorText, out var currentDependentFactor) ||
            currentDependentFactor < 0m || currentDependentFactor >= 1m)
        {
            error = "Current dependant factor must be between 0 and 1 (for example 0.055).";
            return false;
        }

        var chargePowerKw = totalPowerKw - (baseLossWatts / 1000m) - (totalPowerKw * currentDependentFactor);
        if (chargePowerKw <= 0m)
        {
            error = "Computed charge power must be greater than zero. Lower losses or increase total power.";
            return false;
        }

        var totalLossWatts = baseLossWatts + (totalPowerKw * 1000m * currentDependentFactor);

        input = new ChargingInput(
            currentSoc,
            targetSoc,
            batteryCapacityKwh,
            chargeWindowStart,
            chargeWindowEnd,
            totalPowerKw,
            chargePowerKw,
            baseLossWatts,
            currentDependentFactor,
            totalLossWatts);
        return true;
    }

    private void SaveDefaults()
    {
        if (!TryParseInputs(out var input, out var validationMessage))
        {
            StatusMessage = $"Cannot save defaults: {validationMessage}";
            return;
        }

        var settings = new AppInitializationSettings
        {
            CurrentSocPercent = input.CurrentSocPercent,
            TargetSocPercent = input.TargetSocPercent,
            BatteryCapacityKWh = input.BatteryCapacityKWh,
            ChargeStartTimeText = input.ChargeWindowStart.ToString("hh\\:mm", CultureInfo.InvariantCulture),
            ChargeEndTimeText = input.ChargeWindowEnd.ToString("hh\\:mm", CultureInfo.InvariantCulture),
            TotalPowerKW = input.TotalPowerKW,
            ChargePowerKW = input.ChargePowerKW,
            BaseLossWatts = input.BaseLossWatts,
            CurrentDependentFactor = input.CurrentDependentFactor
        };

        _initializationStore.Save(settings);
        StatusMessage = $"Defaults saved to {_initializationStore.FilePath}";
    }

    private void NotifyPowerPropertiesChanged()
    {
        OnPropertyChanged(nameof(EffectiveChargePowerText));
        OnPropertyChanged(nameof(TotalLossWattsText));
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
               decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseTimeOfDay(string text, out TimeSpan value)
    {
        var formats = new[] { "hh\\:mm", "h\\:mm", "HH\\:mm", "H\\:mm" };

        return TimeSpan.TryParseExact(text, formats, CultureInfo.InvariantCulture, out value) ||
               TimeSpan.TryParseExact(text, formats, CultureInfo.CurrentCulture, out value) ||
               TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out value);
    }

    private static bool IsWithinRange(TimeSpan timeOfDay, TimeSpan rangeStart, TimeSpan rangeEnd)
    {
        if (rangeStart == rangeEnd)
        {
            return true;
        }

        if (rangeEnd > rangeStart)
        {
            return timeOfDay >= rangeStart && timeOfDay < rangeEnd;
        }

        return timeOfDay >= rangeStart || timeOfDay < rangeEnd;
    }
}
