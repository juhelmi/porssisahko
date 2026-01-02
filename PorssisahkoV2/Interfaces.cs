using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpiElectricityPrice
{
    public record NPriceEntry
    {
        public DateTime Timestamp { get; init; }
        public double PriceEurKWh { get; init; }
        //decimal? PriceEurMWh = null,
        //decimal? TaxIncludedPrice = null
    }

    public class PriceSeries
    {
        public string Region;
        public int SlotsInHour = 4;
        public /*IReadOnly*/List<NPriceEntry> Entries;
        public DateTime RetrievedAtUtc;

        public PriceSeries(string region, List<NPriceEntry> entries, DateTime retrievedAtUtc)
        {
            Region = region;
            Entries = entries;
            RetrievedAtUtc = retrievedAtUtc;
        }
    }

    public interface ISpotPriceSource
    {
        string SourceName { get; }

        Task<PriceSeries> GetPricesAsync(
            DateTime start,
            DateTime end,
            string region,
            CancellationToken token = default);

        Task<PriceSeries> GetCheapestPricesAsync(
            DateTime start,
            DateTime end,
            string region,
            double timelimitHours,
            bool allowGaps,
            CancellationToken token = default);
        
    }

    public interface ISpotPriceCache
    {
        Task<PriceSeries?> TryGetAsync(DateTime start, DateTime end, string region);
        Task StoreAsync(PriceSeries series);
    }

    public class FallbackSpotPriceSource : ISpotPriceSource
    {
        //private readonly IReadOnlyList<ISpotPriceSource>? _sources;

        public string SourceName => throw new NotImplementedException();

        public Task<PriceSeries> GetPricesAsync(DateTime start, DateTime end, string region, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
        public Task<PriceSeries> GetCheapestPricesAsync(
            DateTime start,
            DateTime end,
            string region,
            double timelimitHours,
            bool allowGaps,
            CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

    }


}
