using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Zypherpunk.Stablecoin.Holons;

namespace NextGenSoftware.OASIS.API.Zypherpunk.Stablecoin.Services
{
    /// <summary>
    /// Service for aggregating ZEC price from multiple oracle sources
    /// </summary>
    public class OracleService
    {
        private readonly IHolonManager _holonManager;
        private readonly List<IOracleSource> _sources;
        private const string ORACLE_HOLON_NAME = "ZcashPriceOracle";
        
        public OracleService()
        {
            _holonManager = HolonManager.Instance;
            _sources = new List<IOracleSource>();
            
            // Initialize oracle sources
            // TODO: Add real oracle sources when available
            // _sources.Add(new ChainlinkOracleSource());
            // _sources.Add(new DEXAggregatorOracleSource());
            // _sources.Add(new CustomOracleSource());
            
            // For now, add a mock source
            _sources.Add(new MockOracleSource());
        }
        
        /// <summary>
        /// Get current ZEC price (aggregated from multiple sources)
        /// </summary>
        public async Task<OASISResult<decimal>> GetZECPriceAsync()
        {
            var result = new OASISResult<decimal>();
            
            try
            {
                var prices = new List<PricePoint>();
                
                // Query all active sources
                foreach (var source in _sources.Where(s => s.IsActive))
                {
                    try
                    {
                        var priceResult = await source.GetPriceAsync();
                        if (!priceResult.IsError && priceResult.Result > 0)
                        {
                            prices.Add(new PricePoint
                            {
                                Price = priceResult.Result,
                                Timestamp = DateTime.UtcNow,
                                Source = source.Name,
                                Proof = priceResult.Proof
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but continue with other sources
                        Console.WriteLine($"Oracle source {source.Name} failed: {ex.Message}");
                    }
                }
                
                if (prices.Count == 0)
                {
                    result.IsError = true;
                    result.Message = "No oracle sources available";
                    return result;
                }
                
                // Calculate weighted average
                var weightedPrice = CalculateWeightedAverage(prices);
                
                // Update oracle holon
                await UpdateOracleHolonAsync(weightedPrice, prices);
                
                result.Result = weightedPrice;
                result.IsError = false;
                result.Message = "Price retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            
            return result;
        }
        
        /// <summary>
        /// Calculate weighted average price from multiple sources
        /// </summary>
        private decimal CalculateWeightedAverage(List<PricePoint> prices)
        {
            if (prices.Count == 0) return 0;
            
            // For now, simple average (can be enhanced with weights)
            return prices.Average(p => p.Price);
        }
        
        /// <summary>
        /// Update oracle holon with new price
        /// </summary>
        private async Task UpdateOracleHolonAsync(decimal price, List<PricePoint> history)
        {
            // TODO: Load existing oracle holon or create new one
            var oracleHolon = new ZcashPriceOracleHolon
            {
                CurrentPrice = price,
                LastUpdateTime = DateTime.UtcNow,
                PriceHistory = history.TakeLast(100).ToList() // Keep last 100 price points
            };
            
            // Calculate 24h change if we have history
            if (oracleHolon.PriceHistory.Count > 1)
            {
                var oldestPrice = oracleHolon.PriceHistory
                    .Where(p => p.Timestamp >= DateTime.UtcNow.AddHours(-24))
                    .OrderBy(p => p.Timestamp)
                    .FirstOrDefault()?.Price ?? price;
                
                oracleHolon.PreviousPrice = oldestPrice;
                oracleHolon.PriceChange24h = price - oldestPrice;
                oracleHolon.PriceChangePercent24h = oldestPrice > 0 
                    ? (oracleHolon.PriceChange24h / oldestPrice) * 100 
                    : 0;
            }
            
            await _holonManager.SaveHolonAsync(oracleHolon);
        }
    }
    
    /// <summary>
    /// Interface for oracle sources
    /// </summary>
    public interface IOracleSource
    {
        string Name { get; }
        bool IsActive { get; }
        decimal Weight { get; }
        Task<OASISResult<decimal>> GetPriceAsync();
        string Proof { get; }
    }
    
    /// <summary>
    /// Mock oracle source for testing
    /// </summary>
    public class MockOracleSource : IOracleSource
    {
        public string Name => "MockOracle";
        public bool IsActive => true;
        public decimal Weight => 1.0m;
        public string Proof => "mock_proof";
        
        public async Task<OASISResult<decimal>> GetPriceAsync()
        {
            // Mock price (around $30-40 range for ZEC)
            var random = new Random();
            var price = 30m + (decimal)(random.NextDouble() * 10);
            
            await Task.Delay(100); // Simulate network delay
            
            return new OASISResult<decimal>
            {
                Result = price,
                IsError = false,
                Message = "Price retrieved from mock oracle"
            };
        }
    }
}

