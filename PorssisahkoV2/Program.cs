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
            
            var logger = loggerFactory. CreateLogger<Program>();
            var client = new PortsisahkoV2Client(
                loggerFactory.CreateLogger<PortsisahkoV2Client>(), "last_prices_v2.json");
            
            logger.LogInformation("=== Pörssisähkön Hinta (V2 API) ===\n");
            
            // Example 2: Get latest prices
            await GetLatestPricesExample(client, logger);
            
            
            client.Dispose();
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

