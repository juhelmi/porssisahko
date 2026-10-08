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
- Can load prices from a local JSON file instead of calling the live API. Leave the optional price file field blank to use live prices.

The JSON file uses the API response shape, for example:

```json
{
  "status": "success",
  "prices": [
    { "price": 3.25, "startDate": "2026-10-09T00:00:00Z", "endDate": "2026-10-09T01:00:00Z" }
  ]
}
```

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

## Robot Framework UI Tests

The desktop UI tests run on Windows and use RPA Framework's Windows automation library. From this project directory:

```powershell
py -m pip install -r robot-tests/requirements.txt
dotnet build
robot --variable PROJECT_FILE:${PWD}\LaskeEdullisinLataus.csproj robot-tests
```

The suite generates future-dated sample prices for each run and loads them through the optional price JSON file field, so the calculation test does not depend on network access or fixed dates.
