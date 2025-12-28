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
    public class PortsisahkoV2Client :  IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PortsisahkoV2Client> _logger;
        private const string BaseUrl = "https://api.porssisahko.net/v2";

        private List<PriceEntry> _cachedPrices = new List<PriceEntry>();
        private DateTime _lastFetchTime = DateTime.MinValue;
        private String? _lastFetchFilename = null;
        
        public PortsisahkoV2Client(ILogger<PortsisahkoV2Client> logger, String lastFilename)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _logger = logger;
            _lastFetchFilename = lastFilename;
        }
        
        public PortsisahkoV2Client(HttpClient httpClient, ILogger<PortsisahkoV2Client> logger, string lastFilename)
        {
            _httpClient = httpClient;
            _logger = logger;
            _lastFetchFilename = lastFilename;
        }
        
        /// <summary>
        /// GET /v2/latest-prices. json
        /// Returns latest available prices (today + tomorrow if available)
        /// </summary>
        public async Task<LatestPricesResponse?> GetLatestPricesAsync(bool useCache = true)
        {
            try
            {
                if (useCache && 
                    _cachedPrices.Count > 0 && 
                    (DateTime.Now - _lastFetchTime).TotalMinutes < 30)
                {
                    _logger.LogDebug("Using cached latest prices");
                    return new LatestPricesResponse
                    {
                        Status = "ok",
                        Prices = new List<PriceEntry>(_cachedPrices)
                    };
                } else
                {
                    if (useCache && _lastFetchFilename != null &&
                        System.IO.File.Exists(_lastFetchFilename))
                    {
                        try
                        {
                            var json = await System.IO.File.ReadAllTextAsync(_lastFetchFilename);
                            var fileresponse = System.Text.Json.JsonSerializer.Deserialize<LatestPricesResponse>(json);
                            if (fileresponse?.Prices != null)
                            {
                                _cachedPrices.Clear();
                                _cachedPrices.AddRange(fileresponse.Prices);
                                _lastFetchTime = DateTime.Now;
                                _logger.LogInformation($"Loaded latest prices from {_lastFetchFilename}");
                                return fileresponse;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error loading latest prices from {_lastFetchFilename}");
                        }
                    }
                }
                string url = $"{BaseUrl}/latest-prices.json";
                _logger.LogDebug($"Fetching latest prices from {url}");
                
                var response = await _httpClient.GetFromJsonAsync<LatestPricesResponse>(url);
                
                if (response?. Prices != null)
                {
                    _cachedPrices.Clear();
                    _cachedPrices.AddRange(response.Prices);
                    _lastFetchTime = DateTime.Now;
                    _logger.LogInformation($"Retrieved {response.Prices.Count} latest price entries");
                    // Save to file if filename is provided
                    if (!string.IsNullOrEmpty(_lastFetchFilename))
                    {
                        try
                        {
                            var json = System.Text.Json.JsonSerializer.Serialize(response,
                                new System.Text.Json.JsonSerializerOptions
                                { WriteIndented = true });
                            await System.IO.File.WriteAllTextAsync(_lastFetchFilename, json);
                            _logger.LogInformation($"Saved latest prices to {_lastFetchFilename}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error saving latest prices to {_lastFetchFilename}");
                        }
                    }
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching latest prices");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest prices");
                return null;
            }
        }
        
        
        /// <summary>
        /// Helper: Find cheapest hours in a price list
        /// </summary>
        public List<PriceEntry> FindCheapestTimeSlots(List<PriceEntry>? prices, int count = 5)
        {
            if (prices is null)
            {
                prices = _cachedPrices;
            }
            if (prices == null || prices.Count == 0)
                return new List<PriceEntry>();

            var timeNow = DateTime.Now;
            //prices = prices.Where(p => p.EndDate > timeNow).ToList();
            
            var cheaps = prices
                .OrderBy(p => p.Price)
                .Where(p => p.EndDate > timeNow)
                .Take(count)
                .OrderBy(p => p.StartDate)
                .ToList();

            return cheaps
                .OrderBy(p => p.StartDate)
                .ToList();
        }
        
       
        /// <summary>
        /// Helper: Find most expensive hours in a price list
        /// </summary>
        public List<PriceEntry> FindMostExpensiveHours(List<PriceEntry>? prices, int count = 5)
        {
            if (prices == null || prices.Count == 0)
                return new List<PriceEntry>();
            
            return prices
                .OrderByDescending(p => p.Price)
                .Take(count)
                .OrderBy(p => p.StartDate)
                .ToList();
        }
        
        /// <summary>
        /// Helper: Find cheapest consecutive hours
        /// </summary>
        public List<PriceEntry> FindCheapestConsecutiveTimeSlots(
            List<PriceEntry>? prices, 
            int consecutiveSlots = 3)
        {
            if (prices is null)
            {
                prices = _cachedPrices;
            }

            if (prices == null || prices.Count < consecutiveSlots)
                return new List<PriceEntry>();

            var timeNow = DateTime.Now;
            
            var sortedPrices = prices
                .OrderBy(p => p.StartDate)
                .Where(p => p.EndDate > timeNow)
                .ToList();
            
            double minSum = double.MaxValue;
            int minIndex = 0;
            
            for (int i = 0; i <= sortedPrices.Count - consecutiveSlots; i++)
            {
                double sum = sortedPrices
                    .Skip(i)
                    .Take(consecutiveSlots)
                    .Sum(p => p.Price);
                
                if (sum < minSum)
                {
                    minSum = sum;
                    minIndex = i;
                }
            }
            
            return sortedPrices.Skip(minIndex).Take(consecutiveSlots).ToList();
        }
        
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}