using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services
{
    /// <summary>
    /// Billing worker for creating QuickBooks invoices automatically.
    /// Implements IQuickBooksService interface.
    /// </summary>
    public class QuickBooksBillingWorker : IQuickBooksService
    {
        private readonly IShipexProRepository _repository;
        private readonly ILogger<QuickBooksBillingWorker> _logger;
        private readonly Func<Guid, Task<QuickBooksApiClient>> _getApiClient; // Factory to get API client for merchant

        public QuickBooksBillingWorker(
            IShipexProRepository repository,
            ILogger<QuickBooksBillingWorker> logger = null,
            Func<Guid, Task<QuickBooksApiClient>> getApiClient = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger;
            _getApiClient = getApiClient;
        }

        public async Task<OASISResult<Invoice>> CreateInvoiceAsync(Shipment shipment)
        {
            var result = new OASISResult<Invoice>();

            try
            {
                _logger?.LogInformation($"Creating QuickBooks invoice for shipment {shipment.ShipmentId}");

                // 1. Get API client for merchant (requires access token from secret vault)
                if (_getApiClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "API client factory not configured");
                    return result;
                }

                var apiClient = await _getApiClient(shipment.MerchantId);
                if (apiClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get QuickBooks API client. Merchant may not be connected to QuickBooks.");
                    return result;
                }

                // 2. Find or create QuickBooks Customer
                var customerResult = await FindOrCreateCustomerAsync(apiClient, shipment.MerchantId);
                if (customerResult.IsError || customerResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to find or create customer: {customerResult.Message}");
                    return result;
                }

                var customer = customerResult.Result;

                // 3. Create invoice with line items
                var invoice = new QuickBooksInvoice
                {
                    CustomerRef = new QuickBooksReference
                    {
                        value = customer.Id,
                        name = customer.DisplayName
                    },
                    Line = new List<QuickBooksLineItem>
                    {
                        new QuickBooksLineItem
                        {
                            Amount = shipment.CarrierCost,
                            Description = $"Shipping Service - {shipment.TrackingNumber}",
                            Qty = 1,
                            UnitPrice = shipment.CarrierCost
                        },
                        new QuickBooksLineItem
                        {
                            Amount = shipment.MarkupAmount,
                            Description = "Shipping Markup",
                            Qty = 1,
                            UnitPrice = shipment.MarkupAmount
                        }
                    },
                    TotalAmt = shipment.AmountCharged,
                    TxnDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    PrivateNote = $"Shipment ID: {shipment.ShipmentId}, Tracking: {shipment.TrackingNumber}"
                };

                // 4. Create invoice via QuickBooks API
                var quickBooksInvoiceResult = await apiClient.CreateInvoiceAsync(invoice);
                if (quickBooksInvoiceResult.IsError || quickBooksInvoiceResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to create invoice in QuickBooks: {quickBooksInvoiceResult.Message}");
                    return result;
                }

                var quickBooksInvoice = quickBooksInvoiceResult.Result;

                // 5. Store invoice link in database
                var invoiceRecord = new Invoice
                {
                    InvoiceId = Guid.NewGuid(),
                    ShipmentId = shipment.ShipmentId,
                    MerchantId = shipment.MerchantId,
                    QuickBooksInvoiceId = quickBooksInvoice.Id,
                    QuickBooksCustomerId = customer.Id,
                    Amount = shipment.AmountCharged,
                    LineItems = new List<InvoiceLineItem>
                    {
                        new InvoiceLineItem
                        {
                            Description = $"Shipping Service - {shipment.TrackingNumber}",
                            Amount = shipment.CarrierCost
                        },
                        new InvoiceLineItem
                        {
                            Description = "Shipping Markup",
                            Amount = shipment.MarkupAmount
                        }
                    },
                    Status = InvoiceStatus.Draft,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var saveResult = await _repository.SaveInvoiceAsync(invoiceRecord);
                if (saveResult.IsError)
                {
                    _logger?.LogError($"Failed to save invoice record: {saveResult.Message}");
                    OASISErrorHandling.HandleError(ref result, $"Failed to save invoice: {saveResult.Message}");
                    return result;
                }

                result.Result = invoiceRecord;
                result.IsError = false;

                _logger?.LogInformation($"Successfully created QuickBooks invoice {quickBooksInvoice.Id} for shipment {shipment.ShipmentId}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while creating invoice for shipment {shipment.ShipmentId}");
                OASISErrorHandling.HandleError(ref result, $"Failed to create invoice: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<Invoice>> GetInvoiceAsync(Guid invoiceId)
        {
            var result = new OASISResult<Invoice>();

            try
            {
                _logger?.LogInformation($"Getting invoice {invoiceId}");

                var invoiceResult = await _repository.GetInvoiceAsync(invoiceId);
                if (invoiceResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, invoiceResult.Message);
                    return result;
                }

                if (invoiceResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Invoice {invoiceId} not found");
                    return result;
                }

                result.Result = invoiceResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while getting invoice {invoiceId}");
                OASISErrorHandling.HandleError(ref result, $"Failed to get invoice: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<bool>> CheckPaymentStatusAsync(Guid invoiceId)
        {
            var result = new OASISResult<bool>();

            try
            {
                _logger?.LogInformation($"Checking payment status for invoice {invoiceId}");

                // Get invoice from database
                var invoiceResult = await _repository.GetInvoiceAsync(invoiceId);
                if (invoiceResult.IsError || invoiceResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, invoiceResult.IsError ? invoiceResult.Message : "Invoice not found");
                    return result;
                }

                var invoice = invoiceResult.Result;

                // Get API client
                if (_getApiClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "API client factory not configured");
                    return result;
                }

                var apiClient = await _getApiClient(invoice.MerchantId);
                if (apiClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get QuickBooks API client");
                    return result;
                }

                // Query QuickBooks for invoice payment status
                // This is a simplified version - actual implementation would query QuickBooks API
                // For now, we'll check if status is "Paid" in our database
                result.Result = invoice.Status == InvoiceStatus.Paid;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while checking payment status for invoice {invoiceId}");
                OASISErrorHandling.HandleError(ref result, $"Failed to check payment status: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<bool>> RefreshAccessTokenAsync(Guid merchantId)
        {
            var result = new OASISResult<bool>();

            try
            {
                _logger?.LogInformation($"Refreshing QuickBooks access token for merchant {merchantId}");

                // This would use QuickBooksOAuthService to refresh the token
                // Implementation depends on secret vault service (Agent F)
                // For now, return not implemented
                OASISErrorHandling.HandleError(ref result, "Token refresh requires secret vault service (Agent F)");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while refreshing token for merchant {merchantId}");
                OASISErrorHandling.HandleError(ref result, $"Failed to refresh token: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Finds or creates a QuickBooks customer for a merchant.
        /// </summary>
        private async Task<OASISResult<QuickBooksCustomer>> FindOrCreateCustomerAsync(
            QuickBooksApiClient apiClient,
            Guid merchantId)
        {
            var result = new OASISResult<QuickBooksCustomer>();

            try
            {
                // Get merchant to get email
                var merchantResult = await _repository.GetMerchantAsync(merchantId);
                if (merchantResult.IsError || merchantResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Merchant not found");
                    return result;
                }

                var merchant = merchantResult.Result;
                var email = merchant.ContactInfo?.Email;

                if (string.IsNullOrEmpty(email))
                {
                    OASISErrorHandling.HandleError(ref result, "Merchant email not found");
                    return result;
                }

                // Try to find existing customer
                var findResult = await apiClient.FindCustomerByEmailAsync(email);
                if (!findResult.IsError && findResult.Result != null)
                {
                    _logger?.LogInformation($"Found existing QuickBooks customer: {findResult.Result.Id}");
                    result.Result = findResult.Result;
                    result.IsError = false;
                    return result;
                }

                // Create new customer
                var customer = new QuickBooksCustomer
                {
                    DisplayName = merchant.CompanyName ?? merchant.ContactInfo?.Name ?? "Merchant",
                    CompanyName = merchant.CompanyName,
                    Email = email
                };

                var createResult = await apiClient.CreateCustomerAsync(customer);
                if (createResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to create customer: {createResult.Message}");
                    return result;
                }

                _logger?.LogInformation($"Created new QuickBooks customer: {createResult.Result.Id}");
                result.Result = createResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while finding/creating customer for merchant {merchantId}");
                OASISErrorHandling.HandleError(ref result, $"Failed to find or create customer: {ex.Message}");
            }

            return result;
        }
    }
}

