using System;
using System.Collections.Generic;
using System. Globalization;
using System.Net. Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpiElectricityPrice.Models.V2;

namespace RpiElectricityPrice.Services
{
    public class SpotHintaClient :  IDisposable
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        private const string BaseUrl = "https://api.spot-hinta.fi/";

        private List<SpotHintaEntry> _cachedPrices = new List<SpotHintaEntry>();
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
                        _lastFetchTime = DateTime.Now;
                        response.ReadTime = _lastFetchTime;  // File will know when data was read

                        if (useCache && _lastFetchFilename != null)
                        {
                            _logger.LogDebug($"Saving latest prices to file: {_lastFetchFilename}");
                            var jsonString = System.Text.Json.JsonSerializer.Serialize(response);
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