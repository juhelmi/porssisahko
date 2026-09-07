using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<LatestPricesResponse>(stream, JsonOptions, cancellationToken);

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
}