using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories
{
    /// <summary>
    /// MongoDB repository implementation for Shipex Pro.
    /// Implements all data access operations following OASIS patterns.
    /// </summary>
    public class ShipexProMongoRepository : IShipexProRepository
    {
        private readonly ShipexProMongoDbContext _context;

        public ShipexProMongoRepository(ShipexProMongoDbContext context)
        {
            _context = context;
        }

        #region Merchant Operations

        public async Task<OASISResult<Merchant>> GetMerchantAsync(Guid merchantId)
        {
            OASISResult<Merchant> result = new OASISResult<Merchant>();

            try
            {
                FilterDefinition<Merchant> filter = Builders<Merchant>.Filter.Where(x => x.MerchantId == merchantId);
                result.Result = await _context.Merchants.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Merchant with id {merchantId} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetMerchantAsync method in ShipexProMongoRepository loading Merchant. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Merchant>> GetMerchantByEmailAsync(string email)
        {
            OASISResult<Merchant> result = new OASISResult<Merchant>();

            try
            {
                FilterDefinition<Merchant> filter = Builders<Merchant>.Filter.Where(x => x.ContactInfo.Email == email);
                result.Result = await _context.Merchants.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Merchant with email {email} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetMerchantByEmailAsync method in ShipexProMongoRepository loading Merchant. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Merchant>> GetMerchantByApiKeyHashAsync(string apiKeyHash)
        {
            OASISResult<Merchant> result = new OASISResult<Merchant>();

            try
            {
                FilterDefinition<Merchant> filter = Builders<Merchant>.Filter.Where(x => x.ApiKeyHash == apiKeyHash && x.IsActive);
                result.Result = await _context.Merchants.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Merchant with API key hash not found or inactive.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetMerchantByApiKeyHashAsync method in ShipexProMongoRepository loading Merchant. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Merchant>> SaveMerchantAsync(Merchant merchant)
        {
            OASISResult<Merchant> result = new OASISResult<Merchant>();

            try
            {
                if (merchant.MerchantId == Guid.Empty)
                {
                    merchant.MerchantId = Guid.NewGuid();
                    merchant.CreatedAt = DateTime.UtcNow;
                }

                merchant.UpdatedAt = DateTime.UtcNow;

                // Check if merchant exists
                var existingMerchant = await GetMerchantAsync(merchant.MerchantId);
                
                if (existingMerchant.Result != null)
                {
                    // Update existing merchant
                    await _context.Merchants.ReplaceOneAsync(
                        filter: g => g.MerchantId == merchant.MerchantId,
                        replacement: merchant);
                }
                else
                {
                    // Insert new merchant
                    await _context.Merchants.InsertOneAsync(merchant);
                }

                result.Result = merchant;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SaveMerchantAsync method in ShipexProMongoRepository saving Merchant. Reason: {ex}");
            }

            return result;
        }

        #endregion

        #region Quote Operations

        public async Task<OASISResult<Quote>> GetQuoteAsync(Guid quoteId)
        {
            OASISResult<Quote> result = new OASISResult<Quote>();

            try
            {
                FilterDefinition<Quote> filter = Builders<Quote>.Filter.Where(x => x.QuoteId == quoteId);
                result.Result = await _context.Quotes.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Quote with id {quoteId} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetQuoteAsync method in ShipexProMongoRepository loading Quote. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Quote>> SaveQuoteAsync(Quote quote)
        {
            OASISResult<Quote> result = new OASISResult<Quote>();

            try
            {
                if (quote.QuoteId == Guid.Empty)
                {
                    quote.QuoteId = Guid.NewGuid();
                    quote.CreatedAt = DateTime.UtcNow;
                }

                // Check if quote exists
                var existingQuote = await GetQuoteAsync(quote.QuoteId);
                
                if (existingQuote.Result != null)
                {
                    // Update existing quote
                    await _context.Quotes.ReplaceOneAsync(
                        filter: g => g.QuoteId == quote.QuoteId,
                        replacement: quote);
                }
                else
                {
                    // Insert new quote
                    await _context.Quotes.InsertOneAsync(quote);
                }

                result.Result = quote;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SaveQuoteAsync method in ShipexProMongoRepository saving Quote. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<List<Quote>>> GetQuotesByMerchantIdAsync(Guid merchantId)
        {
            OASISResult<List<Quote>> result = new OASISResult<List<Quote>>();

            try
            {
                FilterDefinition<Quote> filter = Builders<Quote>.Filter.Where(x => x.MerchantId == merchantId);
                var cursor = await _context.Quotes.FindAsync(filter);
                result.Result = await cursor.ToListAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetQuotesByMerchantIdAsync method in ShipexProMongoRepository loading Quotes. Reason: {ex}");
            }

            return result;
        }

        #endregion

        #region Order Operations

        public async Task<OASISResult<Order>> GetOrderAsync(Guid orderId)
        {
            OASISResult<Order> result = new OASISResult<Order>();

            try
            {
                FilterDefinition<Order> filter = Builders<Order>.Filter.Where(x => x.OrderId == orderId);
                result.Result = await _context.Orders.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Order with id {orderId} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetOrderAsync method in ShipexProMongoRepository loading Order. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Order>> SaveOrderAsync(Order order)
        {
            OASISResult<Order> result = new OASISResult<Order>();

            try
            {
                if (order.OrderId == Guid.Empty)
                {
                    order.OrderId = Guid.NewGuid();
                    order.CreatedAt = DateTime.UtcNow;
                }

                order.UpdatedAt = DateTime.UtcNow;

                // Check if order exists
                var existingOrder = await GetOrderAsync(order.OrderId);
                
                if (existingOrder.Result != null)
                {
                    // Update existing order
                    await _context.Orders.ReplaceOneAsync(
                        filter: g => g.OrderId == order.OrderId,
                        replacement: order);
                }
                else
                {
                    // Insert new order
                    await _context.Orders.InsertOneAsync(order);
                }

                result.Result = order;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SaveOrderAsync method in ShipexProMongoRepository saving Order. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Order>> UpdateOrderAsync(Order order)
        {
            OASISResult<Order> result = new OASISResult<Order>();

            try
            {
                order.UpdatedAt = DateTime.UtcNow;

                await _context.Orders.ReplaceOneAsync(
                    filter: g => g.OrderId == order.OrderId,
                    replacement: order);

                result.Result = order;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in UpdateOrderAsync method in ShipexProMongoRepository updating Order. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<List<Order>>> GetOrdersByMerchantIdAsync(Guid merchantId)
        {
            OASISResult<List<Order>> result = new OASISResult<List<Order>>();

            try
            {
                FilterDefinition<Order> filter = Builders<Order>.Filter.Where(x => x.MerchantId == merchantId);
                var cursor = await _context.Orders.FindAsync(filter);
                result.Result = await cursor.ToListAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetOrdersByMerchantIdAsync method in ShipexProMongoRepository loading Orders. Reason: {ex}");
            }

            return result;
        }

        #endregion

        #region Markup Operations

        public async Task<OASISResult<List<MarkupConfiguration>>> GetActiveMarkupsAsync(Guid? merchantId = null)
        {
            OASISResult<List<MarkupConfiguration>> result = new OASISResult<List<MarkupConfiguration>>();

            try
            {
                var now = DateTime.UtcNow;
                FilterDefinition<MarkupConfiguration> filter;

                if (merchantId.HasValue)
                {
                    filter = Builders<MarkupConfiguration>.Filter.And(
                        Builders<MarkupConfiguration>.Filter.Where(x => x.MerchantId == merchantId || x.MerchantId == null),
                        Builders<MarkupConfiguration>.Filter.Where(x => x.IsActive),
                        Builders<MarkupConfiguration>.Filter.Where(x => x.EffectiveFrom <= now),
                        Builders<MarkupConfiguration>.Filter.Or(
                            Builders<MarkupConfiguration>.Filter.Where(x => !x.EffectiveTo.HasValue),
                            Builders<MarkupConfiguration>.Filter.Where(x => x.EffectiveTo >= now)
                        )
                    );
                }
                else
                {
                    filter = Builders<MarkupConfiguration>.Filter.And(
                        Builders<MarkupConfiguration>.Filter.Where(x => x.MerchantId == null),
                        Builders<MarkupConfiguration>.Filter.Where(x => x.IsActive),
                        Builders<MarkupConfiguration>.Filter.Where(x => x.EffectiveFrom <= now),
                        Builders<MarkupConfiguration>.Filter.Or(
                            Builders<MarkupConfiguration>.Filter.Where(x => !x.EffectiveTo.HasValue),
                            Builders<MarkupConfiguration>.Filter.Where(x => x.EffectiveTo >= now)
                        )
                    );
                }

                var cursor = await _context.MarkupConfigurations.FindAsync(filter);
                result.Result = await cursor.ToListAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetActiveMarkupsAsync method in ShipexProMongoRepository loading Markups. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<MarkupConfiguration>> GetMarkupAsync(Guid markupId)
        {
            OASISResult<MarkupConfiguration> result = new OASISResult<MarkupConfiguration>();

            try
            {
                FilterDefinition<MarkupConfiguration> filter = Builders<MarkupConfiguration>.Filter.Where(x => x.MarkupId == markupId);
                result.Result = await _context.MarkupConfigurations.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Markup with id {markupId} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetMarkupAsync method in ShipexProMongoRepository loading Markup. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<MarkupConfiguration>> SaveMarkupAsync(MarkupConfiguration markup)
        {
            OASISResult<MarkupConfiguration> result = new OASISResult<MarkupConfiguration>();

            try
            {
                if (markup.MarkupId == Guid.Empty)
                {
                    markup.MarkupId = Guid.NewGuid();
                    markup.CreatedAt = DateTime.UtcNow;
                }

                markup.UpdatedAt = DateTime.UtcNow;

                var existingMarkup = await GetMarkupAsync(markup.MarkupId);
                
                if (existingMarkup.Result != null)
                {
                    await _context.MarkupConfigurations.ReplaceOneAsync(
                        filter: g => g.MarkupId == markup.MarkupId,
                        replacement: markup);
                }
                else
                {
                    await _context.MarkupConfigurations.InsertOneAsync(markup);
                }

                result.Result = markup;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SaveMarkupAsync method in ShipexProMongoRepository saving Markup. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteMarkupAsync(Guid markupId)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                var deleteResult = await _context.MarkupConfigurations.DeleteOneAsync(
                    filter: g => g.MarkupId == markupId);

                result.Result = deleteResult.DeletedCount > 0;
                
                if (!result.Result)
                {
                    OASISErrorHandling.HandleError(ref result, $"Markup with id {markupId} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in DeleteMarkupAsync method in ShipexProMongoRepository deleting Markup. Reason: {ex}");
            }

            return result;
        }

        #endregion

        #region Shipment Operations

        public async Task<OASISResult<Shipment>> GetShipmentAsync(Guid shipmentId)
        {
            OASISResult<Shipment> result = new OASISResult<Shipment>();

            try
            {
                FilterDefinition<Shipment> filter = Builders<Shipment>.Filter.Where(x => x.ShipmentId == shipmentId);
                result.Result = await _context.Shipments.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Shipment with id {shipmentId} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetShipmentAsync method in ShipexProMongoRepository loading Shipment. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Shipment>> SaveShipmentAsync(Shipment shipment)
        {
            OASISResult<Shipment> result = new OASISResult<Shipment>();

            try
            {
                if (shipment.ShipmentId == Guid.Empty)
                {
                    shipment.ShipmentId = Guid.NewGuid();
                    shipment.CreatedAt = DateTime.UtcNow;
                }

                shipment.UpdatedAt = DateTime.UtcNow;

                var existingShipment = await GetShipmentAsync(shipment.ShipmentId);
                
                if (existingShipment.Result != null)
                {
                    await _context.Shipments.ReplaceOneAsync(
                        filter: g => g.ShipmentId == shipment.ShipmentId,
                        replacement: shipment);
                }
                else
                {
                    await _context.Shipments.InsertOneAsync(shipment);
                }

                result.Result = shipment;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SaveShipmentAsync method in ShipexProMongoRepository saving Shipment. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Shipment>> UpdateShipmentAsync(Shipment shipment)
        {
            OASISResult<Shipment> result = new OASISResult<Shipment>();

            try
            {
                shipment.UpdatedAt = DateTime.UtcNow;

                await _context.Shipments.ReplaceOneAsync(
                    filter: g => g.ShipmentId == shipment.ShipmentId,
                    replacement: shipment);

                result.Result = shipment;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in UpdateShipmentAsync method in ShipexProMongoRepository updating Shipment. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Shipment>> GetShipmentByTrackingNumberAsync(string trackingNumber)
        {
            OASISResult<Shipment> result = new OASISResult<Shipment>();

            try
            {
                FilterDefinition<Shipment> filter = Builders<Shipment>.Filter.Where(x => x.TrackingNumber == trackingNumber);
                result.Result = await _context.Shipments.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Shipment with tracking number {trackingNumber} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetShipmentByTrackingNumberAsync method in ShipexProMongoRepository loading Shipment. Reason: {ex}");
            }

            return result;
        }

        #endregion

        #region Invoice Operations

        public async Task<OASISResult<Invoice>> GetInvoiceAsync(Guid invoiceId)
        {
            OASISResult<Invoice> result = new OASISResult<Invoice>();

            try
            {
                FilterDefinition<Invoice> filter = Builders<Invoice>.Filter.Where(x => x.InvoiceId == invoiceId);
                result.Result = await _context.Invoices.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Invoice with id {invoiceId} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetInvoiceAsync method in ShipexProMongoRepository loading Invoice. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Invoice>> SaveInvoiceAsync(Invoice invoice)
        {
            OASISResult<Invoice> result = new OASISResult<Invoice>();

            try
            {
                if (invoice.InvoiceId == Guid.Empty)
                {
                    invoice.InvoiceId = Guid.NewGuid();
                    invoice.CreatedAt = DateTime.UtcNow;
                }

                invoice.UpdatedAt = DateTime.UtcNow;

                var existingInvoice = await GetInvoiceAsync(invoice.InvoiceId);
                
                if (existingInvoice.Result != null)
                {
                    await _context.Invoices.ReplaceOneAsync(
                        filter: g => g.InvoiceId == invoice.InvoiceId,
                        replacement: invoice);
                }
                else
                {
                    await _context.Invoices.InsertOneAsync(invoice);
                }

                result.Result = invoice;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SaveInvoiceAsync method in ShipexProMongoRepository saving Invoice. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Invoice>> UpdateInvoiceAsync(Invoice invoice)
        {
            OASISResult<Invoice> result = new OASISResult<Invoice>();

            try
            {
                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.Invoices.ReplaceOneAsync(
                    filter: g => g.InvoiceId == invoice.InvoiceId,
                    replacement: invoice);

                result.Result = invoice;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in UpdateInvoiceAsync method in ShipexProMongoRepository updating Invoice. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<List<Invoice>>> GetInvoicesByShipmentIdAsync(Guid shipmentId)
        {
            OASISResult<List<Invoice>> result = new OASISResult<List<Invoice>>();

            try
            {
                FilterDefinition<Invoice> filter = Builders<Invoice>.Filter.Where(x => x.ShipmentId == shipmentId);
                var cursor = await _context.Invoices.FindAsync(filter);
                result.Result = await cursor.ToListAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetInvoicesByShipmentIdAsync method in ShipexProMongoRepository loading Invoices. Reason: {ex}");
            }

            return result;
        }

        #endregion

        #region Secret Operations

        public async Task<OASISResult<SecretRecord>> GetSecretRecordAsync(string key)
        {
            OASISResult<SecretRecord> result = new OASISResult<SecretRecord>();

            try
            {
                FilterDefinition<SecretRecord> filter = Builders<SecretRecord>.Filter.Where(x => x.Key == key && x.IsActive);
                result.Result = await _context.SecretRecords.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Secret with key {key} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetSecretRecordAsync method in ShipexProMongoRepository loading SecretRecord. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<SecretRecord>> SaveSecretRecordAsync(SecretRecord secretRecord)
        {
            OASISResult<SecretRecord> result = new OASISResult<SecretRecord>();

            try
            {
                if (secretRecord.Id == Guid.Empty)
                {
                    secretRecord.Id = Guid.NewGuid();
                    secretRecord.CreatedAt = DateTime.UtcNow;
                }

                // Check if secret exists (deactivate old one if rotating)
                var existingSecret = await GetSecretRecordAsync(secretRecord.Key);
                
                if (existingSecret.Result != null)
                {
                    // Deactivate old secret and create new one (for rotation)
                    existingSecret.Result.IsActive = false;
                    await _context.SecretRecords.ReplaceOneAsync(
                        filter: g => g.Key == secretRecord.Key && g.Id == existingSecret.Result.Id,
                        replacement: existingSecret.Result);
                    
                    // Insert new secret record
                    secretRecord.Id = Guid.NewGuid();
                    secretRecord.CreatedAt = DateTime.UtcNow;
                    await _context.SecretRecords.InsertOneAsync(secretRecord);
                }
                else
                {
                    // Insert new secret
                    await _context.SecretRecords.InsertOneAsync(secretRecord);
                }

                result.Result = secretRecord;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SaveSecretRecordAsync method in ShipexProMongoRepository saving SecretRecord. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteSecretRecordAsync(string key)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                var update = Builders<SecretRecord>.Update.Set(x => x.IsActive, false);
                var updateResult = await _context.SecretRecords.UpdateManyAsync(
                    filter: g => g.Key == key,
                    update: update);

                result.Result = updateResult.ModifiedCount > 0;
                
                if (!result.Result)
                {
                    OASISErrorHandling.HandleError(ref result, $"Secret with key {key} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in DeleteSecretRecordAsync method in ShipexProMongoRepository deleting SecretRecord. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<List<SecretRecord>>> GetSecretRecordsByMerchantIdAsync(Guid merchantId)
        {
            OASISResult<List<SecretRecord>> result = new OASISResult<List<SecretRecord>>();

            try
            {
                FilterDefinition<SecretRecord> filter = Builders<SecretRecord>.Filter.Where(x => x.MerchantId == merchantId && x.IsActive);
                var cursor = await _context.SecretRecords.FindAsync(filter);
                result.Result = await cursor.ToListAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetSecretRecordsByMerchantIdAsync method in ShipexProMongoRepository loading SecretRecords. Reason: {ex}");
            }

            return result;
        }

        #endregion

        #region Webhook Operations

        public async Task<OASISResult<WebhookEvent>> GetWebhookEventAsync(Guid eventId)
        {
            OASISResult<WebhookEvent> result = new OASISResult<WebhookEvent>();

            try
            {
                FilterDefinition<WebhookEvent> filter = Builders<WebhookEvent>.Filter.Where(x => x.EventId == eventId);
                result.Result = await _context.WebhookEvents.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Webhook event with id {eventId} not found.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetWebhookEventAsync method in ShipexProMongoRepository loading WebhookEvent. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<WebhookEvent>> SaveWebhookEventAsync(WebhookEvent webhookEvent)
        {
            OASISResult<WebhookEvent> result = new OASISResult<WebhookEvent>();

            try
            {
                if (webhookEvent.EventId == Guid.Empty)
                {
                    webhookEvent.EventId = Guid.NewGuid();
                }

                if (webhookEvent.ReceivedAt == default(DateTime))
                {
                    webhookEvent.ReceivedAt = DateTime.UtcNow;
                }

                // Check if webhook event exists
                var existingWebhook = await GetWebhookEventAsync(webhookEvent.EventId);
                
                if (existingWebhook.Result != null)
                {
                    // Update existing webhook event
                    await _context.WebhookEvents.ReplaceOneAsync(
                        filter: g => g.EventId == webhookEvent.EventId,
                        replacement: webhookEvent);
                }
                else
                {
                    // Insert new webhook event
                    await _context.WebhookEvents.InsertOneAsync(webhookEvent);
                }

                result.Result = webhookEvent;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SaveWebhookEventAsync method in ShipexProMongoRepository saving WebhookEvent. Reason: {ex}");
            }

            return result;
        }

        #endregion
    }
}
