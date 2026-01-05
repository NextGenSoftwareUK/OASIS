using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;

/// <summary>
/// Repository interface for Shipex Pro data access
/// This will be implemented by Agent A's MongoDB repository
/// </summary>
public interface IShipexProRepository
{
    // Merchant operations
    Task<OASISResult<Merchant>> GetMerchantAsync(Guid merchantId);
    Task<OASISResult<Merchant>> GetMerchantByEmailAsync(string email);
    Task<OASISResult<Merchant>> GetMerchantByApiKeyHashAsync(string apiKeyHash);
    Task<OASISResult<Merchant>> SaveMerchantAsync(Merchant merchant);
    
    // Quote operations
    Task<OASISResult<Quote>> GetQuoteAsync(Guid quoteId);
    Task<OASISResult<Quote>> SaveQuoteAsync(Quote quote);
    Task<OASISResult<List<Quote>>> GetQuotesByMerchantIdAsync(Guid merchantId);
    
    // Order operations
    Task<OASISResult<Order>> GetOrderAsync(Guid orderId);
    Task<OASISResult<Order>> SaveOrderAsync(Order order);
    Task<OASISResult<Order>> UpdateOrderAsync(Order order);
    Task<OASISResult<List<Order>>> GetOrdersByMerchantIdAsync(Guid merchantId);
    
    // Markup operations
    Task<OASISResult<List<MarkupConfiguration>>> GetActiveMarkupsAsync(Guid? merchantId = null);
    Task<OASISResult<MarkupConfiguration>> GetMarkupAsync(Guid markupId);
    Task<OASISResult<MarkupConfiguration>> SaveMarkupAsync(MarkupConfiguration markup);
    Task<OASISResult<bool>> DeleteMarkupAsync(Guid markupId);
    
    // Shipment operations
    Task<OASISResult<Shipment>> GetShipmentAsync(Guid shipmentId);
    Task<OASISResult<Shipment>> SaveShipmentAsync(Shipment shipment);
    Task<OASISResult<Shipment>> UpdateShipmentAsync(Shipment shipment);
    Task<OASISResult<Shipment>> GetShipmentByTrackingNumberAsync(string trackingNumber);
    
    // Invoice operations
    Task<OASISResult<Invoice>> GetInvoiceAsync(Guid invoiceId);
    Task<OASISResult<Invoice>> SaveInvoiceAsync(Invoice invoice);
    Task<OASISResult<Invoice>> UpdateInvoiceAsync(Invoice invoice);
    Task<OASISResult<List<Invoice>>> GetInvoicesByShipmentIdAsync(Guid shipmentId);
    
    // Secret operations (for Secret Vault)
    Task<OASISResult<SecretRecord>> GetSecretRecordAsync(string key);
    Task<OASISResult<SecretRecord>> SaveSecretRecordAsync(SecretRecord secretRecord);
    Task<OASISResult<bool>> DeleteSecretRecordAsync(string key);
    Task<OASISResult<List<SecretRecord>>> GetSecretRecordsByMerchantIdAsync(Guid merchantId);
}
