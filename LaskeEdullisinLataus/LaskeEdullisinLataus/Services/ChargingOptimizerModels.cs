using System;
using System.Collections.Generic;

namespace LaskeEdullisinLataus.Services;

public readonly record struct ChargingInput(
    decimal CurrentSocPercent,
    decimal TargetSocPercent,
    decimal BatteryCapacityKWh,
    TimeSpan ChargeWindowStart,
    TimeSpan ChargeWindowEnd,
    decimal TotalPowerKW,
    decimal ChargePowerKW,
    decimal BaseLossWatts,
    decimal CurrentDependentFactor,
    decimal TotalLossWatts);

public sealed record PriceSlot(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    decimal PriceCentsPerKWh);

public sealed record ChargeSegment(
    DateTimeOffset StartLocal,
    DateTimeOffset EndLocal,
    decimal PriceCentsPerKWh,
    decimal GridEnergyKWh,
    decimal CostEur);

public sealed class ChargingPlanResult
{
    public bool Success { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public DateTimeOffset StartLocal { get; init; }
    public DateTimeOffset EndLocal { get; init; }
    public decimal BatteryEnergyNeededKWh { get; init; }
    public decimal GridEnergyUsedKWh { get; init; }
    public decimal RequiredDurationHours { get; init; }
    public decimal TotalCostEur { get; init; }
    public IReadOnlyList<ChargeSegment> Segments { get; init; } = [];
}