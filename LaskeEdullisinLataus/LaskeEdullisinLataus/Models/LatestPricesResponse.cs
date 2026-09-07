using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LaskeEdullisinLataus.Models;

public sealed class LatestPricesResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("prices")]
    public List<LatestPriceItem> Prices { get; init; } = [];
}

public sealed class LatestPriceItem
{
    [JsonPropertyName("price")]
    public decimal Price { get; init; }

    [JsonPropertyName("startDate")]
    public DateTimeOffset StartDate { get; init; }

    [JsonPropertyName("endDate")]
    public DateTimeOffset EndDate { get; init; }
}