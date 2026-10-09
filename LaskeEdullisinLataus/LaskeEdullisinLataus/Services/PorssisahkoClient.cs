using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LaskeEdullisinLataus;
using LaskeEdullisinLataus.Models;

namespace LaskeEdullisinLataus.Services;

public sealed class PorssisahkoClient
{
    private static readonly Uri LatestPricesUri = new("https://api.porssisahko.net/v2/latest-prices.json");
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<PriceSlot>> GetLatestPricesAsync(CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(LatestPricesUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = JsonSerializer.Deserialize<LatestPricesResponse>(responseJson, JsonOptions);
        var priceSlots = ToPriceSlots(payload);

        await SaveLatestPricesAsync(responseJson, priceSlots, cancellationToken);
        return priceSlots;
    }

    public async Task<IReadOnlyList<PriceSlot>> GetPricesFromFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var payload = await JsonSerializer.DeserializeAsync<LatestPricesResponse>(stream, JsonOptions, cancellationToken);

        return ToPriceSlots(payload);
    }

    private static IReadOnlyList<PriceSlot> ToPriceSlots(LatestPricesResponse? payload)
    {
        if (payload?.Prices is null || payload.Prices.Count == 0)
        {
            return [];
        }

        return payload.Prices
            .Where(x => x.EndDate > x.StartDate)
            .Select(x => new PriceSlot(x.StartDate, x.EndDate, x.Price))
            .OrderBy(x => x.StartDate)
            .ToList();
    }

    private static async Task SaveLatestPricesAsync(
        string responseJson,
        IReadOnlyList<PriceSlot> priceSlots,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(AppConstants.PriceExportDirectoryPath);

        var jsonPath = Path.Combine(
            AppConstants.PriceExportDirectoryPath,
            $"{AppConstants.LatestPricesFileBaseName}.json");
        var csvPath = Path.Combine(
            AppConstants.PriceExportDirectoryPath,
            $"{AppConstants.LatestPricesFileBaseName}.csv");

        await File.WriteAllTextAsync(jsonPath, responseJson, cancellationToken);
        await File.WriteAllTextAsync(csvPath, ToCsv(priceSlots), new UTF8Encoding(true), cancellationToken);
    }

    private static string ToCsv(IReadOnlyList<PriceSlot> priceSlots)
    {
        var csv = new StringBuilder("startDate;endDate;priceCentsPerKWh\r\n");

        foreach (var priceSlot in priceSlots)
        {
            csv.Append(priceSlot.StartDate.ToString("O", CultureInfo.InvariantCulture))
                .Append(';')
                .Append(priceSlot.EndDate.ToString("O", CultureInfo.InvariantCulture))
                .Append(';')
                .Append(priceSlot.PriceCentsPerKWh.ToString(CultureInfo.InvariantCulture))
                .Append("\r\n");
        }

        return csv.ToString();
    }
}