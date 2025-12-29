using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorssisahkoV2
{
    public record PriceEntry(
    DateTime Timestamp,
    decimal? PriceEurMWh = null,
    decimal PriceEurKWh,
    decimal? TaxIncludedPrice = null);

    public record PriceSeries(
        string Region,
        IReadOnlyList<PriceEntry> Entries,
        DateTime RetrievedAtUtc);

    public interface ISpotPriceSource
    {
        string SourceName { get; }

        Task<PriceSeries> GetPricesAsync(
            DateTime start,
            DateTime end,
            string region,
            CancellationToken token = default);
    }

    public interface ISpotPriceCache
    {
        Task<PriceSeries?> TryGetAsync(DateTime start, DateTime end, string region);
        Task StoreAsync(PriceSeries series);
    }

    public class FallbackSpotPriceSource : ISpotPriceSource
    {
        private readonly IReadOnlyList<ISpotPriceSource> _sources;

        public string SourceName => throw new NotImplementedException();

        public Task<PriceSeries> GetPricesAsync(DateTime start, DateTime end, string region, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }


}
