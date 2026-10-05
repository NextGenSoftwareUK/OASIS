using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks
{
    /// <summary>
    /// HTTP client for QuickBooks API (Intuit API).
    /// Handles authentication, customer management, and invoice creation.
    /// </summary>
    public class QuickBooksApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<QuickBooksApiClient> _logger;
        private readonly bool _useSandbox;
        private string _accessToken;
        private string _realmId;

        public QuickBooksApiClient(
            string accessToken,
            string realmId,
            ILogger<QuickBooksApiClient> logger = null,
            bool useSandbox = true,
            HttpClient httpClient = null)
        {
            _accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            _realmId = realmId ?? throw new ArgumentNullException(nameof(realmId));
            _logger = logger;
            _useSandbox = useSandbox;
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Updates the access token (used when token is refreshed).
        /// </summary>
        public void UpdateAccessToken(string accessToken)
        {
            _accessToken = accessToken;
        }

        /// <summary>
        /// Gets the base URL for QuickBooks API based on sandbox/production.
        /// </summary>
        private string GetBaseUrl()
        {
            return _useSandbox
                ? $"https://sandbox-quickbooks.api.intuit.com/v3/company/{_realmId}"
                : $"https://quickbooks.api.intuit.com/v3/company/{_realmId}";
        }

        /// <summary>
        /// Finds a customer by email address.
        /// </summary>
        public async Task<OASISResult<QuickBooksCustomer>> FindCustomerByEmailAsync(string email)
        {
            var result = new OASISResult<QuickBooksCustomer>();

            try
            {
                _logger?.LogInformation($"Finding QuickBooks customer by email: {email}");

                // Query QuickBooks for customer by email
                var query = $"SELECT * FROM Customer WHERE PrimaryEmailAddr = '{email}'";
                var queryResult = await QueryAsync<QuickBooksCustomer>("Customer", query);

                if (queryResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, queryResult.Message);
                    return result;
                }

                // QuickBooks returns results in QueryResponse.maxResults
                // This is a simplified version - actual implementation would parse the response properly
                if (queryResult.Result != null && queryResult.Result.Count > 0)
                {
                    result.Result = queryResult.Result[0];
                    result.IsError = false;
                }
                else
                {
                    result.Result = null;
                    result.IsError = false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while finding customer by email {email}");
                OASISErrorHandling.HandleError(ref result, $"Failed to find customer: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Creates a new customer in QuickBooks.
        /// </summary>
        public async Task<OASISResult<QuickBooksCustomer>> CreateCustomerAsync(QuickBooksCustomer customer)
        {
            var result = new OASISResult<QuickBooksCustomer>();

            try
            {
                _logger?.LogInformation($"Creating QuickBooks customer: {customer.DisplayName}");

                var url = $"{GetBaseUrl()}/customer";
                var response = await PostAsync<QuickBooksCustomer>(url, customer);

                if (response.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, response.Message);
                    return result;
                }

                result.Result = response.Result;
                result.IsError = false;

                _logger?.LogInformation($"Successfully created QuickBooks customer: {result.Result.Id}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while creating customer");
                OASISErrorHandling.HandleError(ref result, $"Failed to create customer: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Creates an invoice in QuickBooks.
        /// </summary>
        public async Task<OASISResult<QuickBooksInvoice>> CreateInvoiceAsync(QuickBooksInvoice invoice)
        {
            var result = new OASISResult<QuickBooksInvoice>();

            try
            {
                _logger?.LogInformation($"Creating QuickBooks invoice for customer {invoice.CustomerRef?.value}");

                var url = $"{GetBaseUrl()}/invoice";
                var response = await PostAsync<QuickBooksInvoice>(url, invoice);

                if (response.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, response.Message);
                    return result;
                }

                result.Result = response.Result;
                result.IsError = false;

                _logger?.LogInformation($"Successfully created QuickBooks invoice: {result.Result.Id}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while creating invoice");
                OASISErrorHandling.HandleError(ref result, $"Failed to create invoice: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Executes a query against QuickBooks API.
        /// </summary>
        private async Task<OASISResult<List<QuickBooksCustomer>>> QueryAsync<QuickBooksCustomer>(string entity, string query)
        {
            var result = new OASISResult<List<QuickBooksCustomer>>();

            try
            {
                var url = $"{GetBaseUrl()}/query?query={Uri.EscapeDataString(query)}";
                var response = await GetAsync<QuickBooksResponse<QuickBooksQueryResponse<QuickBooksCustomer>>>(url);

                if (response.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, response.Message);
                    return result;
                }

                // Extract results from response
                result.Result = response.Result?.QueryResponse?.maxResults ?? new List<QuickBooksCustomer>();
                result.IsError = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while querying {entity}");
                OASISErrorHandling.HandleError(ref result, $"Failed to query: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Sends a GET request to QuickBooks API.
        /// </summary>
        private async Task<OASISResult<T>> GetAsync<T>(string url)
        {
            var result = new OASISResult<T>();

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError($"QuickBooks API error: {response.StatusCode} - {content}");
                    OASISErrorHandling.HandleError(ref result, $"API error: {content}");
                    return result;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                result.Result = JsonSerializer.Deserialize<T>(content, options);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception in GET request to {url}");
                OASISErrorHandling.HandleError(ref result, $"Request failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Sends a POST request to QuickBooks API.
        /// </summary>
        private async Task<OASISResult<T>> PostAsync<T>(string url, object data)
        {
            var result = new OASISResult<T>();

            try
            {
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError($"QuickBooks API error: {response.StatusCode} - {responseContent}");
                    OASISErrorHandling.HandleError(ref result, $"API error: {responseContent}");
                    return result;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                result.Result = JsonSerializer.Deserialize<T>(responseContent, options);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception in POST request to {url}");
                OASISErrorHandling.HandleError(ref result, $"Request failed: {ex.Message}");
            }

            return result;
        }
    }
}




