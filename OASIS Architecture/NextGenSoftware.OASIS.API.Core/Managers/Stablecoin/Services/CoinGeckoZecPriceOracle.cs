using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Services
{
    /// <summary>
    /// ZEC price oracle using CoinGecko API
    /// </summary>
    public class CoinGeckoZecPriceOracle : IZecPriceOracle
    {
        private readonly HttpClient _httpClient;
        private const string COINGECKO_API_URL = "https://api.coingecko.com/api/v3/simple/price?ids=zcash&vs_currencies=usd";

        public CoinGeckoZecPriceOracle(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<OASISResult<decimal>> GetZecPriceAsync(CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<decimal>();

            try
            {
                var response = await _httpClient.GetStringAsync(COINGECKO_API_URL);
                var json = JObject.Parse(response);
                var price = json["zcash"]?["usd"]?.Value<decimal>() ?? 0;

                if (price <= 0)
                {
                    result.IsError = true;
                    result.Message = "Invalid price data from CoinGecko";
                    return result;
                }

                result.Result = price;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error fetching ZEC price from CoinGecko: {ex.Message}", ex);
                return result;
            }
        }
    }
}

