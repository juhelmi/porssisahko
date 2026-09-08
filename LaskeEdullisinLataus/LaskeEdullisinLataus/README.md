# LaskeEdullisinLataus

Avalonia desktop app for finding the cheapest continuous EV charging window from the latest electricity prices at:

- https://api.porssisahko.net/v2/latest-prices.json

## Main Features

- Reads latest market prices from Porssisahko v2 API.
- Calculates the cheapest continuous charging period.
- User inputs:
  - Current SOC (%)
  - Target SOC (%)
  - Estimated battery capacity (kWh)
  - Total power (kW)
  - Base loss (W)
  - Current dependant factor (default 0.055)
- Shows computed values:
  - Effective charge power (kW)
  - Computed total loss (W)
- Displays selected charging segments and total estimated cost.

## Charging Model

- Charge power is computed as:

  chargePower = totalPower - baseLoss(kW) - totalPower * currentFactor

- Where:
  - baseLoss(kW) = baseLoss(W) / 1000
  - currentFactor default is 0.055

- Total loss is computed as:

  totalLoss(W) = baseLoss(W) + totalPower(kW) * 1000 * currentFactor

## Persisted User Values

The app stores and reloads default input values.

- Save current values with button: "Save Current Values As Default"
- Values are loaded automatically on app startup.
- Stored fields:
  - Current SOC
  - Target SOC
  - Estimated battery capacity
  - Total power
  - Computed charge power
  - Base loss
  - Current dependant factor

Settings file location:

- %LOCALAPPDATA%\\LaskeEdullisinLataus\\initialization.json

## Build and Run

From project folder:

- dotnet restore
- dotnet run

Target framework:

- .NET 10

UI framework:

- Avalonia
