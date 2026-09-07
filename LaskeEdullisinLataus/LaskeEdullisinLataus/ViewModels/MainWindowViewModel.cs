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
    private readonly PorssisahkoClient _priceClient = new();
    private readonly ChargingOptimizer _optimizer = new();

    [ObservableProperty]
    private string currentSocText = "40";

    [ObservableProperty]
    private string targetSocText = "80";

    [ObservableProperty]
    private string batteryCapacityKwhText = "77";

    [ObservableProperty]
    private string chargingLossWattsText = "300";

    [ObservableProperty]
    private string chargePowerKwText = "5.52";

    [ObservableProperty]
    private string statusMessage = "Ready.";

    [ObservableProperty]
    private string resultSummary = "Enter values and run calculation.";

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<ChargeSegmentViewModel> CheapestWindowSegments { get; } = [];

    public IAsyncRelayCommand CalculateCommand { get; }

    public MainWindowViewModel()
    {
        CalculateCommand = new AsyncRelayCommand(CalculateAsync, CanCalculate);
    }

    partial void OnCurrentSocTextChanged(string value) => CalculateCommand.NotifyCanExecuteChanged();
    partial void OnTargetSocTextChanged(string value) => CalculateCommand.NotifyCanExecuteChanged();
    partial void OnBatteryCapacityKwhTextChanged(string value) => CalculateCommand.NotifyCanExecuteChanged();
    partial void OnChargingLossWattsTextChanged(string value) => CalculateCommand.NotifyCanExecuteChanged();
    partial void OnChargePowerKwTextChanged(string value) => CalculateCommand.NotifyCanExecuteChanged();

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
            StatusMessage = "Fetching latest electricity prices...";

            var priceSlots = await _priceClient.GetLatestPricesAsync(CancellationToken.None);
            var futureSlots = priceSlots
                .Where(x => x.EndDate > DateTimeOffset.UtcNow)
                .OrderBy(x => x.StartDate)
                .ToList();

            if (futureSlots.Count == 0)
            {
                StatusMessage = "No future price data available from API.";
                return;
            }

            var result = _optimizer.FindCheapestContinuousWindow(input, futureSlots);

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
                $"Battery energy needed: {result.BatteryEnergyNeededKWh:F2} kWh\n" +
                $"Grid energy used: {result.GridEnergyUsedKWh:F2} kWh\n" +
                $"Total cost: {result.TotalCostEur:F2} EUR";

            StatusMessage = $"Calculation completed using {futureSlots.Count} future price slots.";
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

        if (!TryParseDecimal(ChargePowerKwText, out var chargePowerKw) || chargePowerKw <= 0m)
        {
            error = "Charge power (kW) must be a positive number.";
            return false;
        }

        if (!TryParseDecimal(ChargingLossWattsText, out var chargingLossWatts) || chargingLossWatts < 0m)
        {
            error = "Charging loss (W) must be zero or positive.";
            return false;
        }

        input = new ChargingInput(currentSoc, targetSoc, batteryCapacityKwh, chargePowerKw, chargingLossWatts);
        return true;
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
               decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
