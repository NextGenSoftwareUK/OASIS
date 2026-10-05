using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories
{
    /// <summary>
    /// MongoDB database context for Shipex Pro collections.
    /// Manages MongoDB connection and collection access, and initializes indexes.
    /// </summary>
    public class ShipexProMongoDbContext
    {
        public MongoClient MongoClient { get; set; }
        public IMongoDatabase MongoDB { get; set; }

        public ShipexProMongoDbContext(string connectionString, string dbName)
        {
            MongoClient = new MongoClient(connectionString);
            MongoDB = MongoClient.GetDatabase(dbName);
            
            InitializeIndexes();
        }

        // Collections
        public IMongoCollection<Quote> Quotes => MongoDB.GetCollection<Quote>("quotes");
        public IMongoCollection<Markup> Markups => MongoDB.GetCollection<Markup>("markups");
        public IMongoCollection<MarkupConfiguration> MarkupConfigurations => MongoDB.GetCollection<MarkupConfiguration>("markup_configurations");
        public IMongoCollection<Shipment> Shipments => MongoDB.GetCollection<Shipment>("shipments");
        public IMongoCollection<Invoice> Invoices => MongoDB.GetCollection<Invoice>("invoices");
        public IMongoCollection<WebhookEvent> WebhookEvents => MongoDB.GetCollection<WebhookEvent>("webhook_events");
        public IMongoCollection<Merchant> Merchants => MongoDB.GetCollection<Merchant>("merchants");
        public IMongoCollection<Order> Orders => MongoDB.GetCollection<Order>("orders");
        public IMongoCollection<SecretRecord> SecretRecords => MongoDB.GetCollection<SecretRecord>("secret_records");

        /// <summary>
        /// Initialize all MongoDB indexes for optimal query performance.
        /// Indexes are created asynchronously and won't block if they already exist.
        /// </summary>
        private void InitializeIndexes()
        {
            try
            {
                // Quotes indexes
                var quotesIndexes = new CreateIndexModel<Quote>[]
                {
                    new CreateIndexModel<Quote>(Builders<Quote>.IndexKeys.Ascending(x => x.QuoteId), new CreateIndexOptions { Unique = true, Name = "idx_quoteId" }),
                    new CreateIndexModel<Quote>(Builders<Quote>.IndexKeys.Ascending(x => x.MerchantId).Descending(x => x.CreatedAt), new CreateIndexOptions { Name = "idx_merchantId_createdAt" }),
                    new CreateIndexModel<Quote>(Builders<Quote>.IndexKeys.Ascending(x => x.ExpiresAt), new CreateIndexOptions { Name = "idx_expiresAt" })
                };
                Quotes.Indexes.CreateMany(quotesIndexes);

                // Markups indexes
                var markupsIndexes = new CreateIndexModel<Markup>[]
                {
                    new CreateIndexModel<Markup>(Builders<Markup>.IndexKeys.Ascending(x => x.MarkupId), new CreateIndexOptions { Unique = true, Name = "idx_markupId" }),
                    new CreateIndexModel<Markup>(Builders<Markup>.IndexKeys.Ascending(x => x.MerchantId).Ascending(x => x.Carrier).Ascending(x => x.IsActive), new CreateIndexOptions { Name = "idx_merchantId_carrier_isActive" }),
                    new CreateIndexModel<Markup>(Builders<Markup>.IndexKeys.Ascending(x => x.Carrier).Ascending(x => x.IsActive), new CreateIndexOptions { Name = "idx_carrier_isActive" })
                };
                Markups.Indexes.CreateMany(markupsIndexes);

                // Shipments indexes
                var shipmentsIndexes = new CreateIndexModel<Shipment>[]
                {
                    new CreateIndexModel<Shipment>(Builders<Shipment>.IndexKeys.Ascending(x => x.ShipmentId), new CreateIndexOptions { Unique = true, Name = "idx_shipmentId" }),
                    new CreateIndexModel<Shipment>(Builders<Shipment>.IndexKeys.Ascending(x => x.TrackingNumber), new CreateIndexOptions { Unique = true, Name = "idx_trackingNumber" }),
                    new CreateIndexModel<Shipment>(Builders<Shipment>.IndexKeys.Ascending(x => x.MerchantId).Descending(x => x.CreatedAt), new CreateIndexOptions { Name = "idx_merchantId_createdAt" }),
                    new CreateIndexModel<Shipment>(Builders<Shipment>.IndexKeys.Ascending(x => x.Status).Descending(x => x.UpdatedAt), new CreateIndexOptions { Name = "idx_status_updatedAt" }),
                    new CreateIndexModel<Shipment>(Builders<Shipment>.IndexKeys.Ascending(x => x.QuoteId), new CreateIndexOptions { Name = "idx_quoteId" })
                };
                Shipments.Indexes.CreateMany(shipmentsIndexes);

                // Invoices indexes
                var invoicesIndexes = new CreateIndexModel<Invoice>[]
                {
                    new CreateIndexModel<Invoice>(Builders<Invoice>.IndexKeys.Ascending(x => x.InvoiceId), new CreateIndexOptions { Unique = true, Name = "idx_invoiceId" }),
                    new CreateIndexModel<Invoice>(Builders<Invoice>.IndexKeys.Ascending(x => x.ShipmentId), new CreateIndexOptions { Unique = true, Name = "idx_shipmentId" }),
                    new CreateIndexModel<Invoice>(Builders<Invoice>.IndexKeys.Ascending(x => x.QuickBooksInvoiceId), new CreateIndexOptions { Unique = true, Name = "idx_quickBooksInvoiceId" }),
                    new CreateIndexModel<Invoice>(Builders<Invoice>.IndexKeys.Ascending(x => x.MerchantId).Descending(x => x.CreatedAt), new CreateIndexOptions { Name = "idx_merchantId_createdAt" }),
                    new CreateIndexModel<Invoice>(Builders<Invoice>.IndexKeys.Ascending(x => x.Status), new CreateIndexOptions { Name = "idx_status" })
                };
                Invoices.Indexes.CreateMany(invoicesIndexes);

                // Webhook Events indexes
                var webhookIndexes = new CreateIndexModel<WebhookEvent>[]
                {
                    new CreateIndexModel<WebhookEvent>(Builders<WebhookEvent>.IndexKeys.Ascending(x => x.EventId), new CreateIndexOptions { Unique = true, Name = "idx_eventId" }),
                    new CreateIndexModel<WebhookEvent>(Builders<WebhookEvent>.IndexKeys.Ascending(x => x.Source).Descending(x => x.CreatedAt), new CreateIndexOptions { Name = "idx_source_createdAt" }),
                    new CreateIndexModel<WebhookEvent>(Builders<WebhookEvent>.IndexKeys.Ascending(x => x.ProcessingStatus).Ascending(x => x.RetryCount), new CreateIndexOptions { Name = "idx_processingStatus_retryCount" })
                };
                WebhookEvents.Indexes.CreateMany(webhookIndexes);

                // Merchants indexes
                // Note: Email index is sparse to allow multiple null emails
                // Only non-null emails must be unique (empty strings are normalized to null in code)
                var merchantsIndexes = new CreateIndexModel<Merchant>[]
                {
                    new CreateIndexModel<Merchant>(Builders<Merchant>.IndexKeys.Ascending(x => x.MerchantId), new CreateIndexOptions { Unique = true, Name = "idx_merchantId" }),
                    new CreateIndexModel<Merchant>(
                        Builders<Merchant>.IndexKeys.Ascending(x => x.ContactInfo.Email), 
                        new CreateIndexOptions 
                        { 
                            Unique = true, 
                            Sparse = true,  // Only index non-null emails (null emails not indexed, so multiple nulls allowed)
                            Name = "idx_email"
                        }),
                    new CreateIndexModel<Merchant>(Builders<Merchant>.IndexKeys.Ascending(x => x.AvatarId), new CreateIndexOptions { Unique = true, Sparse = true, Name = "idx_avatarId" }),
                    new CreateIndexModel<Merchant>(Builders<Merchant>.IndexKeys.Ascending(x => x.IsActive), new CreateIndexOptions { Name = "idx_isActive" })
                };
                Merchants.Indexes.CreateMany(merchantsIndexes);

                // Orders indexes
                var ordersIndexes = new CreateIndexModel<Order>[]
                {
                    new CreateIndexModel<Order>(Builders<Order>.IndexKeys.Ascending(x => x.OrderId), new CreateIndexOptions { Unique = true, Name = "idx_orderId" }),
                    new CreateIndexModel<Order>(Builders<Order>.IndexKeys.Ascending(x => x.MerchantId).Descending(x => x.CreatedAt), new CreateIndexOptions { Name = "idx_merchantId_createdAt" }),
                    new CreateIndexModel<Order>(Builders<Order>.IndexKeys.Ascending(x => x.QuoteId), new CreateIndexOptions { Name = "idx_quoteId" }),
                    new CreateIndexModel<Order>(Builders<Order>.IndexKeys.Ascending(x => x.Status), new CreateIndexOptions { Name = "idx_status" })
                };
                Orders.Indexes.CreateMany(ordersIndexes);

                // Secret Records indexes (for STAR ledger compatibility)
                var secretIndexes = new CreateIndexModel<SecretRecord>[]
                {
                    new CreateIndexModel<SecretRecord>(Builders<SecretRecord>.IndexKeys.Ascending(x => x.Key), new CreateIndexOptions { Unique = true, Name = "idx_key" }),
                    new CreateIndexModel<SecretRecord>(Builders<SecretRecord>.IndexKeys.Ascending(x => x.MerchantId).Ascending(x => x.IsActive), new CreateIndexOptions { Name = "idx_merchantId_isActive" }),
                    new CreateIndexModel<SecretRecord>(Builders<SecretRecord>.IndexKeys.Ascending(x => x.SecretType), new CreateIndexOptions { Name = "idx_secretType" }),
                    new CreateIndexModel<SecretRecord>(Builders<SecretRecord>.IndexKeys.Ascending(x => x.ExpiresAt), new CreateIndexOptions { Name = "idx_expiresAt" })
                };
                SecretRecords.Indexes.CreateMany(secretIndexes);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - indexes may already exist
                // In production, you might want to log this to a logging framework
                System.Diagnostics.Debug.WriteLine($"Error initializing MongoDB indexes: {ex.Message}");
            }
        }
    }
}

