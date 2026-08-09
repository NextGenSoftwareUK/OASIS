using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{    public partial class ONETAPIGateway
    {
        private async Task<string> DetermineOptimalNetworkTypeAsync(string endpoint, object parameters)
        {
            // Intelligent network type determination
            if (endpoint.Contains("blockchain") || endpoint.Contains("crypto") || endpoint.Contains("nft"))
            {
                return "web3";
            }
            else if (endpoint.Contains("social") || endpoint.Contains("payment") || endpoint.Contains("api"))
            {
                return "web2";
            }
            else
            {
                return "hybrid"; // Use hybrid for optimal performance
            }
        }

        private async Task<APIBridge?> FindOptimalBridgeAsync(string endpoint, string networkType)
        {
            // Find the best bridge for the endpoint
            if (_apiBridges.ContainsKey(networkType))
            {
                return _apiBridges[networkType];
            }
            else if (_apiBridges.ContainsKey("hybrid"))
            {
                return _apiBridges["hybrid"];
            }

            // Return default bridge if no specific match found
            return _apiBridges.Values.FirstOrDefault(b => b.Status == "Active");
        }

        // Shared, never-recreated HttpClient - ExecuteAPICallAsync previously did `new HttpClient()` on every
        // single call with no disposal, which leaks a socket per call under load (HttpClient is designed to
        // be long-lived and reused, not constructed per-request).
        private static readonly HttpClient _apiHttpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(5000) };

        private string GenerateCacheKey(string endpoint, object parameters, string networkType)
        {
            // parameters is nullable at every call site - GetHashCode() on a null reference threw NRE here.
            var parametersKey = parameters?.GetHashCode().ToString() ?? "none";
            return $"{endpoint}_{networkType}_{parametersKey}";
        }

        private async Task<OASISResult<object>> ExecuteAPICallAsync(APIEndpoint endpoint, string apiEndpoint, object parameters, string networkType)
        {
            var result = new OASISResult<object>();

            try
            {
                // Build the request URL from the resolved endpoint (falling back to the raw apiEndpoint path
                // if no concrete URL was resolved), honour the endpoint's configured HTTP method instead of
                // always issuing GET, and append parameters as a query string when provided - previously
                // apiEndpoint/parameters/networkType were accepted but silently ignored entirely.
                var url = !string.IsNullOrEmpty(endpoint?.Url) ? endpoint.Url
                    : !string.IsNullOrEmpty(endpoint?.Endpoint) ? endpoint.Endpoint
                    : apiEndpoint;

                if (parameters is System.Collections.IDictionary paramDict && paramDict.Count > 0)
                {
                    var queryParts = new List<string>();
                    foreach (System.Collections.DictionaryEntry entry in paramDict)
                        queryParts.Add($"{Uri.EscapeDataString(entry.Key.ToString())}={Uri.EscapeDataString(entry.Value?.ToString() ?? string.Empty)}");
                    url += (url.Contains('?') ? "&" : "?") + string.Join("&", queryParts);
                }

                var method = string.IsNullOrEmpty(endpoint?.Method) ? "GET" : endpoint.Method.ToUpperInvariant();
                using var request = new HttpRequestMessage(new HttpMethod(method), url);
                request.Headers.Add("X-ONET-Network-Type", networkType ?? "auto");

                var httpResponse = await _apiHttpClient.SendAsync(request);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    result.Result = new { Success = true, Data = content, StatusCode = httpResponse.StatusCode };
                    result.IsError = false;
                }
                else
                {
                    result.Result = new { Success = false, Error = httpResponse.ReasonPhrase, StatusCode = httpResponse.StatusCode };
                    result.IsError = true;
                }
            }
            catch (Exception ex)
            {
                result.Result = new { Success = false, Error = ex.Message };
                result.IsError = true;
                OASISErrorHandling.HandleError(ref result, $"Error executing API call: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<APIEndpoint> SelectEndpointAsync(APIBridge bridge, string endpoint)
        {
            // Perform real load balancer selection
            try
            {
                var availableBridges = _apiBridges.Values.Where(b => b.Status == "Active").ToList();
                if (!availableBridges.Any())
                {
                    return new APIEndpoint
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Default OASIS Endpoint",
                        Url = "https://api.oasis.com",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                // Prefer a bridge that actually advertises the requested endpoint - previously the `endpoint`
                // parameter was accepted but never consulted, so every call fell straight through to plain
                // round-robin regardless of which endpoint was actually being requested.
                var matchingBridges = !string.IsNullOrEmpty(endpoint)
                    ? availableBridges.Where(b => b.Endpoints.Any(e => e.Contains(endpoint, StringComparison.OrdinalIgnoreCase))).ToList()
                    : new List<APIBridge>();

                var candidateBridges = matchingBridges.Any() ? matchingBridges : availableBridges;

                // Use round-robin selection across whichever candidate set applies.
                var index = _requestCount % candidateBridges.Count;
                _requestCount++;
                var selectedBridge = candidateBridges[index];

                // Convert APIBridge to APIEndpoint - prefer the specific endpoint URL that matched, if any.
                var matchedUrl = !string.IsNullOrEmpty(endpoint)
                    ? selectedBridge.Endpoints.FirstOrDefault(e => e.Contains(endpoint, StringComparison.OrdinalIgnoreCase))
                    : null;

                return new APIEndpoint
                {
                    Id = selectedBridge.Id,
                    Name = selectedBridge.Name,
                    Url = matchedUrl ?? selectedBridge.Endpoints.FirstOrDefault() ?? "https://api.oasis.com",
                    IsActive = selectedBridge.IsActive,
                    NetworkType = selectedBridge.NetworkType,
                    BridgeId = selectedBridge.Id,
                    CreatedAt = selectedBridge.CreatedAt
                };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<APIEndpoint>();
                OASISErrorHandling.HandleError(ref result, $"Error selecting bridge: {ex.Message}", ex);
                return new APIEndpoint
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Default OASIS Endpoint",
                    Url = "https://api.oasis.com",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<string> GetStatusAsync()
        {
            // Perform real status check
            try
            {
                var activeBridges = _apiBridges.Values.Count(b => b.Status == "Active");
                var totalBridges = _apiBridges.Count;
                var healthPercentage = (double)activeBridges / totalBridges * 100;

                return $"Active - {activeBridges}/{totalBridges} bridges healthy ({healthPercentage:F1}%)";
            }
            catch (Exception ex)
            {
                var result = new OASISResult<string>();
                OASISErrorHandling.HandleError(ref result, $"Error checking status: {ex.Message}", ex);
                return "Error";
            }
        }
    }
}