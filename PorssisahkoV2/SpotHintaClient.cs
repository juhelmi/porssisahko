using Microsoft.Extensions.Logging;
using RpiElectricityPrice;
using RpiElectricityPrice.Models.V2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;

namespace RpiElectricityPrice.Services
{
    public class SpotHintaClient :  IDisposable, ISpotPriceSource
    {
        public readonly string _sourceName;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        private const string BaseUrl = "https://api.spot-hinta.fi/";

        private List<SpotHintaEntry> _cachedPrices = new List<SpotHintaEntry>();
        private PriceSeries _priceSeries;
        private DateTime _lastFetchTime = DateTime.MinValue;
        private String? _lastFetchFilename = null;
        private readonly int _refreshIntervalMinutes = 30;

        public SpotHintaClient(ILogger logger, String lastFetchFilename, int refreshIntervalMinutes)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _logger = logger;
            _lastFetchFilename = lastFetchFilename;
            _refreshIntervalMinutes = refreshIntervalMinutes;
            _sourceName = "FI";
            _priceSeries = new PriceSeries(
                "FI",
                new List<NPriceEntry>(),
                DateTime.UtcNow
            );
        }

        public string SourceName { get { return _sourceName; } }

        private void ConvertOriginalFormatToInterfaceFormat() 
        { 
            _priceSeries.Entries.Clear();
            if (_cachedPrices?.Count > 0 ) 
            foreach (var entry in _cachedPrices)
            {
                if (entry.PriceWithTax.HasValue)
                {
                    NPriceEntry nPrice = new NPriceEntry
                    {
                        Timestamp = entry.Date.DateTime,
                        PriceEurKWh = entry.PriceWithTax.Value * 100
                    };
                    _priceSeries.Entries.Add(nPrice);
                }
            }
        }

        private async Task<int> CheckLastPriceUpdate()
        {
            if ((DateTime.Now - _lastFetchTime).TotalMinutes >= _refreshIntervalMinutes)
            {
                var task = GetLatestPricesAsync();
                await task;
            }
            return 0;
        }

        public async Task<PriceSeries> GetPricesAsync(
            DateTime start,
            DateTime end,
            string region,
            CancellationToken token = default)
        {
            await CheckLastPriceUpdate();
            PriceSeries priceSeries = _priceSeries;
            return priceSeries;
        }

        // Move to common for all Interfaces
        public async Task<PriceSeries> GetCheapestPricesAsync(
            DateTime start,
            DateTime end,
            string region,
            double timelimitHours,
            bool allowGaps,
            CancellationToken token = default)
        {
            await CheckLastPriceUpdate();
            PriceSeries priceSeries = new PriceSeries(
                _priceSeries.Region,
                _priceSeries.Entries,
                _priceSeries.RetrievedAtUtc
            );
            int slots = (int)(timelimitHours * priceSeries.SlotsInHour);
            if (allowGaps)
            {
                // Sorting according to price if gaps are allowed
                var comingHours = priceSeries.Entries
                    .Where(p => p.Timestamp > DateTime.Now)
                    .OrderBy(p => p.PriceEurKWh)
                    .Take(slots)
                    .ToList();
                priceSeries.Entries = comingHours;
            } else
            {
                var timeNow = DateTime.Now;
                var sortedPrices = priceSeries.Entries
                    .OrderBy(p => p.Timestamp)
                    .Where(p => p.Timestamp > timeNow)
                    .ToList();

                double minSum = double.MaxValue;
                int minIndex = 0;

                for (int i = 0; i <= sortedPrices.Count - slots; i++)
                {
                    double sum = sortedPrices
                        .Skip(i)
                        .Take(slots)
                        .Sum(p => p.PriceEurKWh);

                    if (sum < minSum)
                    {
                        minSum = sum;
                        minIndex = i;
                    }
                }
                priceSeries.Entries = sortedPrices.Skip(minIndex).Take(slots).ToList();
            }
            return priceSeries;
        }

        public async Task<SpotHintaResponse?> GetLatestPricesAsync(bool useCache = true)
        {
            try
            {
                if (useCache && 
                    _cachedPrices.Count > 0 && 
                    (DateTime.Now - _lastFetchTime).TotalMinutes < _refreshIntervalMinutes)
                {
                    _logger.LogDebug("Using cached latest prices");
                    return new SpotHintaResponse
                    {
                        Status = "ok",
                        Prices = new List<SpotHintaEntry>(_cachedPrices)
                    };
                } else
                {
                    if (useCache && _lastFetchFilename != null &&
                        System.IO.File.Exists(_lastFetchFilename))
                    {
                        _logger.LogDebug($"Loading latest prices from file: {_lastFetchFilename}");
                        var fileResponse = await System.IO.File.ReadAllTextAsync(_lastFetchFilename);
                        var latestFromFile = System.Text.Json.JsonSerializer.Deserialize<SpotHintaList>(fileResponse);
                        if (latestFromFile != null)
                        {
                            _cachedPrices = new List<SpotHintaEntry>(latestFromFile.data ?? new List<SpotHintaEntry>());
                            ConvertOriginalFormatToInterfaceFormat();
                            _lastFetchTime = latestFromFile.ReadTime ?? DateTime.Now;
                            if ((DateTime.Now - _lastFetchTime).TotalMinutes < _refreshIntervalMinutes)
                            {
                                var lastResponse = new SpotHintaResponse
                                {
                                    Status = "ok",
                                    Prices = new List<SpotHintaEntry>(_cachedPrices)
                                };
                                return lastResponse;
                            }
                        }
                    }

                    var url = $"{BaseUrl}TodayAndDayForward?HomeAssistant=false&HomeAssistant15Min=true";
                    _logger.LogDebug($"Fetching latest prices from URL: {url}");
                    var response = await _httpClient.GetFromJsonAsync<SpotHintaList>(url);
                    if (response != null && response.data != null && response.data.Count > 0)
                    {
                        _cachedPrices = new List<SpotHintaEntry>(response.data);
                        ConvertOriginalFormatToInterfaceFormat();
                        _lastFetchTime = DateTime.Now;
                        response.ReadTime = _lastFetchTime;  // File will know when data was read

                        if (useCache && _lastFetchFilename != null)
                        {
                            _logger.LogDebug($"Saving latest prices to file: {_lastFetchFilename}");
                            var jsonString = System.Text.Json.JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
                            await System.IO.File.WriteAllTextAsync(_lastFetchFilename, jsonString);
                        }
                    }
                    var spotResponse = new SpotHintaResponse
                    {
                        Status = "ok",
                        Prices = new List<SpotHintaEntry>(_cachedPrices)
                    };
                    return spotResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching latest prices: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}