using System;
using System. Collections.Generic;
using System. Text.Json.Serialization;

namespace RpiElectricityPrice.Models. V2
{
    // Latest prices response
    public class LatestPricesResponse
    {
        [JsonPropertyName("status")]
        public string?  Status { get; set; }
        
        [JsonPropertyName("prices")]
        public List<PriceEntry>? Prices { get; set; }
    }

    
    
    // Price entry for a specific hour
    public class PriceEntry
    {
        [JsonPropertyName("price")]
        public double Price { get; set; } // c/kWh (including VAT)
        
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }
        
        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }
    }
    
    // Price now response
    public class PriceNowResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("price")]
        public double Price { get; set; } // c/kWh (including VAT)
        
        [JsonPropertyName("priceNoVat")]
        public double PriceNoVat { get; set; } // c/kWh (excluding VAT)
        
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }
        
        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }
    }
    
    // Price range response
    public class PriceRangeResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("prices")]
        public List<PriceEntry>? Prices { get; set; }
    }
    
    // Statistics response
    public class StatisticsResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("statistics")]
        public PriceStatistics? Statistics { get; set; }
    }
    
    public class PriceStatistics
    {
        [JsonPropertyName("min")]
        public double Min { get; set; }
        
        [JsonPropertyName("max")]
        public double Max { get; set; }
        
        [JsonPropertyName("average")]
        public double Average { get; set; }
        
        [JsonPropertyName("median")]
        public double Median { get; set; }
    }
    
    // Error response
    public class ErrorResponse
    {
        [JsonPropertyName("status")]
        public string?  Status { get; set; }
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}