using System;

namespace LaskeEdullisinLataus.ViewModels;

public sealed class ChargeSegmentViewModel
{
    public ChargeSegmentViewModel(
        DateTimeOffset start,
        DateTimeOffset end,
        decimal priceCentsPerKWh,
        decimal gridEnergyKWh,
        decimal costEur)
    {
        Start = start;
        End = end;
        PriceCentsPerKWh = priceCentsPerKWh;
        GridEnergyKWh = gridEnergyKWh;
        CostEur = costEur;
    }

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
    public decimal PriceCentsPerKWh { get; }
    public decimal GridEnergyKWh { get; }
    public decimal CostEur { get; }
}