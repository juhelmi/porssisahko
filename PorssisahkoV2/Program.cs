// See https://aka.ms/new-console-template for more information

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpiElectricityPrice.Services;

namespace RpiElectricityPrice
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel. Information);
            });
            int refreshIntervalMinutes = 120;
            
            var logger = loggerFactory. CreateLogger<Program>();
            var client = new PortsisahkoV2Client(
                loggerFactory.CreateLogger<PortsisahkoV2Client>(), "last_prices_v2.json", refreshIntervalMinutes);
            
            if (true)
            {
                logger.LogInformation("=== Pörssisähkön Hinta (V2 API) ===\n");
                
                // Example 2: Get latest prices
                await GetLatestPricesExample(client, logger);

            }

            var clientSpot = new SpotHintaClient(
                loggerFactory.CreateLogger<SpotHintaClient>(), "spotHinta.json", refreshIntervalMinutes);
            if (true)
            {
                logger.LogInformation("=== Spot-hinta.fi Prices ===\n");
                await GetSpotHintaPricesExample(
                    clientSpot,
                    logger);
            }

            await Task.Delay(100);
            logger.LogInformation("\n\nDone");
            
            client.Dispose();
            clientSpot.Dispose();
        }
        
        
        static async Task GetSpotHintaPricesExample(
            SpotHintaClient client,
            ILogger logger)
        {
            logger.LogInformation("--- Spot-hinta.fi Latest Prices ---");
            
            var latest = await client.GetLatestPricesAsync();
            if (latest?.Prices != null)
            {
                logger.LogInformation($"Total spots available: {latest.Prices.Count}");
                //logger.LogInformation("\nNext 12 hours:");
                //logger.LogInformation("\nComing hours:");
                
                var now = DateTime.Now;
                var nextHours = latest.Prices
                    .Where(p => p.Date >= now)
                    //.Take(12*4)
                    .ToList();
                
                logger.LogInformation($"\ncoming spots: {nextHours.Count}");
                foreach (var price in nextHours)
                {
                    logger.LogInformation(
                        $"  {price.Date:ddd HH:mm}:  " +
                        $"{(100*price.PriceWithTax):F3} c/kWh rank: {price.Rank}");
                }

                logger.LogInformation($"\nSort spots by rank");
                var sortedRank = nextHours.OrderBy(p => p.Rank).ToList();
                foreach (var price in sortedRank)
                {
                    logger.LogInformation($"  {price.Date:ddd HH:mm}:  " +
                        $"{(100 * price.PriceWithTax):F3} c/kWh rank: {price.Rank}");
                }

                await Task.Delay(100);
            }

            var commonLatest = await client.GetPricesAsync(DateTime.Now, DateTime.Now, "FI");
            if (commonLatest?.Entries?.Count > 0)
            {
                var nextHours = commonLatest.Entries
                    .Where(p => p.Timestamp > DateTime.Now)
                    .ToList();
                foreach (var price in nextHours)
                {
                    logger.LogInformation(
                        $"  {price.PriceEurKWh:F3} c/kWh  time: {price.Timestamp:ddd HH:mm}");
                }
            }
            var consLow = await client.GetCheapestPricesAsync(DateTime.Now, DateTime.Now, "FI", 4, true);
            if (consLow?.Entries?.Count > 0)
            {
                logger.LogInformation("\n");
                var nextHours = consLow.Entries
                    .Where(p => p.Timestamp > DateTime.Now)
                    .ToList();
                foreach (var price in nextHours)
                {
                    logger.LogInformation(
                        $"  {price.PriceEurKWh:F3} c/kWh  time: {price.Timestamp:ddd HH:mm}");
                }
            }

            logger.LogInformation("\n");
        }

        static async Task GetLatestPricesExample(
            PortsisahkoV2Client client, 
            ILogger logger)
        {
            logger.LogInformation("--- Latest Prices ---");
            
            var latest = await client.GetLatestPricesAsync();
            if (latest?.Prices != null)
            {
                logger.LogInformation($"Total hours available: {latest.Prices.Count}");
                //logger.LogInformation("\nNext 12 hours:");
                logger.LogInformation("\nComing hours:");
                
                var now = DateTime.Now;
                var nextHours = latest.Prices
                    .Where(p => p.StartDate >= now)
                    //.Take(12*4)
                    .OrderBy(p => p.StartDate)
                    .ToList();
                
                foreach (var price in nextHours)
                {
                    logger.LogInformation(
                        $"  {price.StartDate:ddd HH:mm} - {price.EndDate:HH:mm}:  " +
                        $"{price.Price:F2} c/kWh");
                }
            }
            
            logger.LogInformation("");

            var slotCout = 3*4; // 3 hours
            logger.LogInformation($"\n=== Cheapest Slots ({slotCout} slots) ===");
            if (latest is not null)
            {
                var cheapest = client.FindCheapestTimeSlots(null, slotCout);
                if (cheapest != null)
                {
                    //logger.LogInformation("\nCheapest upcoming slots:");
                    foreach (var price in cheapest)
                    {
                        logger.LogInformation(
                            $"  {price.StartDate:ddd HH:mm} - {price.EndDate:HH:mm}:  " +
                            $"{price.Price:F2} c/kWh");
                    }
                }
            }

            logger.LogInformation($"\n=== Consecutive cheapest slots ({slotCout} slots) ===");
            if (latest?.Prices != null)
            {
                var consecutive = client.FindCheapestConsecutiveTimeSlots(latest.Prices, slotCout);
                if (consecutive != null)
                {
                    //logger.LogInformation("\nConsecutive cheapest upcoming slots:");
                    foreach (var price in consecutive)
                    {
                        logger.LogInformation(
                            $"  {price.StartDate:ddd HH:mm} - {price.EndDate:HH:mm}:  " +
                            $"{price.Price:F2} c/kWh");
                    }
                }
            }
        }
        
        
    }
}

