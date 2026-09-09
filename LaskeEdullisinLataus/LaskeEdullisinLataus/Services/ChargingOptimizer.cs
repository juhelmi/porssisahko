using System;
using System.Collections.Generic;

namespace LaskeEdullisinLataus.Services;

public sealed class ChargingOptimizer
{
    public ChargingPlanResult FindCheapestContinuousWindow(
        ChargingInput input,
        IReadOnlyList<PriceSlot> priceSlots)
    {
        if (priceSlots.Count == 0)
        {
            return new ChargingPlanResult
            {
                Success = false,
                ErrorMessage = "Price list is empty."
            };
        }

        var batteryEnergyNeededKWh = input.BatteryCapacityKWh *
                                     (input.TargetSocPercent - input.CurrentSocPercent) / 100m;

        if (batteryEnergyNeededKWh <= 0m)
        {
            return new ChargingPlanResult
            {
                Success = false,
                ErrorMessage = "Target SOC must be greater than current SOC."
            };
        }

        if (input.ChargePowerKW <= 0m)
        {
            return new ChargingPlanResult
            {
                Success = false,
                ErrorMessage = "Charge power must be greater than zero."
            };
        }

        var requiredHours = batteryEnergyNeededKWh / input.ChargePowerKW;
        var totalGridPowerKw = input.TotalPowerKW;

        decimal? bestCost = null;
        int bestStartIndex = -1;
        DateTimeOffset bestEndLocal = default;
        decimal bestGridEnergy = 0m;
        List<ChargeSegment> bestSegments = [];

        for (var startIndex = 0; startIndex < priceSlots.Count; startIndex++)
        {
            var remainingHours = requiredHours;
            var totalCostEur = 0m;
            var totalGridEnergyKWh = 0m;
            var segments = new List<ChargeSegment>();
            var feasible = true;
            DateTimeOffset? previousSlotEnd = null;

            for (var i = startIndex; i < priceSlots.Count && remainingHours > 0m; i++)
            {
                var slot = priceSlots[i];
                var slotHours = (decimal)(slot.EndDate - slot.StartDate).TotalHours;

                if (previousSlotEnd is not null && slot.StartDate > previousSlotEnd.Value.AddSeconds(1))
                {
                    feasible = false;
                    break;
                }

                if (slotHours <= 0m)
                {
                    continue;
                }

                var chargeHoursThisSlot = Math.Min(remainingHours, slotHours);
                var gridEnergyThisSlot = totalGridPowerKw * chargeHoursThisSlot;
                var slotCostEur = gridEnergyThisSlot * slot.PriceCentsPerKWh / 100m;
                var actualEnd = slot.StartDate.AddHours((double)chargeHoursThisSlot).ToLocalTime();

                segments.Add(new ChargeSegment(
                    slot.StartDate.ToLocalTime(),
                    actualEnd,
                    slot.PriceCentsPerKWh,
                    gridEnergyThisSlot,
                    slotCostEur));

                totalGridEnergyKWh += gridEnergyThisSlot;
                totalCostEur += slotCostEur;
                remainingHours -= chargeHoursThisSlot;
                previousSlotEnd = slot.EndDate;
            }

            if (remainingHours > 0m)
            {
                feasible = false;
            }

            if (!feasible || segments.Count == 0)
            {
                continue;
            }

            if (bestCost is null || totalCostEur < bestCost.Value)
            {
                bestCost = totalCostEur;
                bestStartIndex = startIndex;
                bestSegments = segments;
                bestEndLocal = segments[^1].EndLocal;
                bestGridEnergy = totalGridEnergyKWh;
            }
        }

        if (bestStartIndex < 0 || bestCost is null)
        {
            return new ChargingPlanResult
            {
                Success = false,
                ErrorMessage = "Not enough future prices available for the requested charge amount."
            };
        }

        return new ChargingPlanResult
        {
            Success = true,
            StartLocal = priceSlots[bestStartIndex].StartDate.ToLocalTime(),
            EndLocal = bestEndLocal,
            BatteryEnergyNeededKWh = batteryEnergyNeededKWh,
            GridEnergyUsedKWh = bestGridEnergy,
            RequiredDurationHours = requiredHours,
            TotalCostEur = bestCost.Value,
            Segments = bestSegments
        };
    }
}