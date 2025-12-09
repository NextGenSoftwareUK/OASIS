# Shipex Pro - MongoDB Database Schema

## Overview

This document defines the complete MongoDB database schema for the Shipex Pro logistics middleware system. The schema includes 6 main collections that support the entire logistics workflow from quote requests through invoice creation.

## Collections

### 1. quotes

Stores rate quotes with markup calculations. Each quote contains carrier rates and client prices (with markup applied).

#### Schema

```json
{
  "_id": "ObjectId",
  "quoteId": "Guid (string)",
  "merchantId": "Guid (string)",
  "shipmentDetails": {
    "dimensions": {
      "length": "decimal",
      "width": "decimal",
      "height": "decimal"
    },
    "weight": "decimal",
    "origin": {
      "addressLine1": "string",
      "addressLine2": "string (optional)",
      "city": "string",
      "state": "string",
      "postalCode": "string",
      "country": "string"
    },
    "destination": {
      "addressLine1": "string",
      "addressLine2": "string (optional)",
      "city": "string",
      "state": "string",
      "postalCode": "string",
      "country": "string"
    },
    "serviceLevel": "string (standard|express|overnight)"
  },
  "carrierRates": [
    {
      "carrier": "string (UPS|FedEx|DHL|USPS)",
      "rate": "decimal",
      "estimatedDays": "integer",
      "serviceName": "string"
    }
  ],
  "clientQuotes": [
    {
      "carrier": "string",
      "carrierRate": "decimal",
      "markupAmount": "decimal",
      "clientPrice": "decimal",
      "markupConfigId": "Guid (string, optional)"
    }
  ],
  "selectedQuote": "ObjectId (optional, reference to selected clientQuote)",
  "expiresAt": "DateTime (ISO 8601)",
  "createdAt": "DateTime (ISO 8601)",
  "updatedAt": "DateTime (ISO 8601)"
}
```

#### Indexes

- `{ quoteId: 1 }` - Unique index for fast quote lookups
- `{ merchantId: 1, createdAt: -1 }` - Compound index for merchant quote history
- `{ expiresAt: 1 }` - TTL index for automatic cleanup of expired quotes (optional)
- `{ merchantId: 1, expiresAt: 1 }` - Compound index for finding expired quotes by merchant

#### Validation Rules

- `quoteId` is required and must be unique
- `merchantId` is required
- `shipmentDetails` is required
- `carrierRates` array must contain at least one rate
- `clientQuotes` array must contain at least one quote
- `expiresAt` must be in the future when created
- `createdAt` and `updatedAt` are automatically set

---

### 2. markups

Stores markup configuration per merchant/carrier. Supports both fixed amount and percentage-based markups.

#### Schema

```json
{
  "_id": "ObjectId",
  "markupId": "Guid (string)",
  "merchantId": "Guid (string, optional, null = global/default)",
  "carrier": "string (UPS|FedEx|DHL|USPS|All)",
  "markupType": "string (Fixed|Percentage)",
  "markupValue": "decimal",
  "effectiveFrom": "DateTime (ISO 8601)",
  "effectiveTo": "DateTime (ISO 8601, optional)",
  "isActive": "boolean",
  "createdAt": "DateTime (ISO 8601)",
  "updatedAt": "DateTime (ISO 8601)"
}
```

#### Indexes

- `{ markupId: 1 }` - Unique index for fast markup lookups
- `{ merchantId: 1, carrier: 1, isActive: 1 }` - Compound index for active markups by merchant/carrier
- `{ merchantId: 1, effectiveFrom: 1, effectiveTo: 1 }` - Compound index for date range queries
- `{ carrier: 1, isActive: 1 }` - Index for global markups by carrier

#### Validation Rules

- `markupId` is required and must be unique
- `carrier` is required
- `markupType` must be either "Fixed" or "Percentage"
- `markupValue` must be positive
- `effectiveFrom` is required
- `effectiveTo` must be after `effectiveFrom` if provided
- `isActive` defaults to true

#### Business Rules

- If `merchantId` is null, the markup applies globally to all merchants
- Merchant-specific markups override global markups
- Only one active markup per merchant/carrier combination at a time
- Date ranges cannot overlap for the same merchant/carrier

---

### 3. shipments

Stores shipment records with full lifecycle tracking. Links to quotes and tracks status changes through the entire shipping process.

#### Schema

```json
{
  "_id": "ObjectId",
  "shipmentId": "Guid (string)",
  "merchantId": "Guid (string)",
  "quoteId": "Guid (string)",
  "carrierShipmentId": "string (from iShip/Shipox)",
  "trackingNumber": "string",
  "status": "string (QuoteRequested|QuoteProvided|QuoteAccepted|ShipmentCreated|LabelGenerated|InTransit|Delivered|Cancelled|Error)",
  "label": {
    "pdfUrl": "string (optional)",
    "pdfBase64": "string (optional)",
    "signedUrl": "string (optional, temporary signed URL)"
  },
  "amountCharged": "decimal",
  "carrierCost": "decimal",
  "markupAmount": "decimal",
  "customerId": "Guid (string, optional)",
  "createdAt": "DateTime (ISO 8601)",
  "updatedAt": "DateTime (ISO 8601)",
  "statusHistory": [
    {
      "status": "string",
      "timestamp": "DateTime (ISO 8601)",
      "source": "string (webhook|api)"
    }
  ]
}
```

#### Indexes

- `{ shipmentId: 1 }` - Unique index for fast shipment lookups
- `{ trackingNumber: 1 }` - Unique index for tracking number lookups
- `{ merchantId: 1, createdAt: -1 }` - Compound index for merchant shipment history
- `{ status: 1, updatedAt: -1 }` - Compound index for status-based queries
- `{ quoteId: 1 }` - Index for linking shipments to quotes
- `{ carrierShipmentId: 1 }` - Index for external carrier shipment ID lookups
- `{ merchantId: 1, status: 1 }` - Compound index for merchant status queries

#### Validation Rules

- `shipmentId` is required and must be unique
- `merchantId` is required
- `quoteId` is required
- `trackingNumber` is required once shipment is created
- `status` is required and must be a valid enum value
- `amountCharged`, `carrierCost`, and `markupAmount` must be non-negative
- `statusHistory` array is automatically maintained

#### Business Rules

- Status transitions must follow the lifecycle: QuoteRequested → QuoteProvided → QuoteAccepted → ShipmentCreated → LabelGenerated → InTransit → Delivered
- Status changes are automatically logged in `statusHistory`
- Label must be present when status is LabelGenerated or later
- Tracking number must be present when status is ShipmentCreated or later

---

### 4. invoices

Stores QuickBooks invoice tracking. Links invoices to shipments and tracks payment status.

#### Schema

```json
{
  "_id": "ObjectId",
  "invoiceId": "Guid (string)",
  "shipmentId": "Guid (string)",
  "merchantId": "Guid (string)",
  "quickBooksInvoiceId": "string (external QuickBooks ID)",
  "quickBooksCustomerId": "string (external QuickBooks customer ID)",
  "amount": "decimal",
  "lineItems": [
    {
      "description": "string",
      "amount": "decimal"
    }
  ],
  "status": "string (Draft|Sent|Paid)",
  "paidAt": "DateTime (ISO 8601, optional)",
  "createdAt": "DateTime (ISO 8601)",
  "updatedAt": "DateTime (ISO 8601)"
}
```

#### Indexes

- `{ invoiceId: 1 }` - Unique index for fast invoice lookups
- `{ shipmentId: 1 }` - Unique index (one invoice per shipment)
- `{ quickBooksInvoiceId: 1 }` - Unique index for QuickBooks ID lookups
- `{ merchantId: 1, createdAt: -1 }` - Compound index for merchant invoice history
- `{ status: 1, createdAt: -1 }` - Compound index for status-based queries
- `{ merchantId: 1, status: 1 }` - Compound index for merchant status queries

#### Validation Rules

- `invoiceId` is required and must be unique
- `shipmentId` is required and must be unique (one invoice per shipment)
- `merchantId` is required
- `amount` must be positive
- `lineItems` array must contain at least one item
- `status` is required and must be a valid enum value
- `paidAt` is required when status is "Paid"

#### Business Rules

- One invoice per shipment (1:1 relationship)
- Invoice is created when shipment status becomes "Delivered"
- `quickBooksInvoiceId` is set after successful creation in QuickBooks
- Status transitions: Draft → Sent → Paid

---

### 5. webhook_events

Stores audit trail for all webhook events from external systems (iShip, Shipox). Supports retry logic and error tracking.

#### Schema

```json
{
  "_id": "ObjectId",
  "eventId": "Guid (string)",
  "source": "string (iShip|Shipox)",
  "eventType": "string (shipment.status.changed|tracking.updated|shipment.shipped|etc.)",
  "payload": "string (JSON string of full webhook data)",
  "signature": "string (HMAC signature for verification)",
  "processingStatus": "string (Pending|Processed|Failed)",
  "errorMessage": "string (optional)",
  "retryCount": "integer",
  "processedAt": "DateTime (ISO 8601, optional)",
  "createdAt": "DateTime (ISO 8601)"
}
```

#### Indexes

- `{ eventId: 1 }` - Unique index for fast event lookups
- `{ source: 1, createdAt: -1 }` - Compound index for source-based queries
- `{ processingStatus: 1, retryCount: 1 }` - Compound index for failed events needing retry
- `{ processingStatus: 1, createdAt: -1 }` - Compound index for pending/failed event queries
- `{ source: 1, eventType: 1, createdAt: -1 }` - Compound index for event type queries

#### Validation Rules

- `eventId` is required and must be unique
- `source` is required
- `eventType` is required
- `payload` is required (JSON string)
- `signature` is required for verification
- `processingStatus` defaults to "Pending"
- `retryCount` defaults to 0

#### Business Rules

- Events are stored immediately upon receipt
- Processing happens asynchronously
- Failed events can be retried up to a maximum retry count
- Events older than 30 days can be archived (optional TTL)

---

### 6. merchants

Stores merchant configuration and credentials. Manages API keys, rate limits, and integration settings.

#### Schema

```json
{
  "_id": "ObjectId",
  "merchantId": "Guid (string)",
  "companyName": "string",
  "contactInfo": {
    "email": "string",
    "phone": "string (optional)",
    "address": "string (optional)"
  },
  "apiKeyHash": "string (hashed API key)",
  "rateLimitTier": "string (Basic|Standard|Premium|Enterprise)",
  "isActive": "boolean",
  "quickBooksConnected": "boolean",
  "configuration": {
    "autoCreateInvoices": "boolean",
    "defaultCurrency": "string",
    "timeZone": "string"
  },
  "createdAt": "DateTime (ISO 8601)",
  "updatedAt": "DateTime (ISO 8601)"
}
```

#### Indexes

- `{ merchantId: 1 }` - Unique index for fast merchant lookups
- `{ apiKeyHash: 1 }` - Unique index for API key authentication
- `{ companyName: 1 }` - Index for company name searches
- `{ isActive: 1 }` - Index for active merchant queries
- `{ rateLimitTier: 1 }` - Index for tier-based queries

#### Validation Rules

- `merchantId` is required and must be unique
- `companyName` is required
- `contactInfo.email` is required and must be valid email format
- `apiKeyHash` is required
- `rateLimitTier` is required and must be a valid enum value
- `isActive` defaults to true

#### Business Rules

- API keys are hashed using secure hashing algorithm (e.g., bcrypt)
- Inactive merchants cannot make API calls
- Rate limits are enforced based on `rateLimitTier`
- `quickBooksConnected` indicates OAuth2 connection status

---

## Relationships

### Entity Relationships

```
merchants (1) ──→ (many) quotes
merchants (1) ──→ (many) shipments
merchants (1) ──→ (many) invoices
merchants (1) ──→ (many) markups

quotes (1) ──→ (1) shipments
shipments (1) ──→ (1) invoices

markups (many) ──→ (many) quotes (via markupConfigId)
```

### Referential Integrity

- `quotes.merchantId` → `merchants.merchantId`
- `shipments.merchantId` → `merchants.merchantId`
- `shipments.quoteId` → `quotes.quoteId`
- `invoices.merchantId` → `merchants.merchantId`
- `invoices.shipmentId` → `shipments.shipmentId`
- `markups.merchantId` → `merchants.merchantId` (optional, null for global)

**Note**: MongoDB does not enforce referential integrity automatically. Application code must ensure data consistency.

---

## Index Strategy

### Performance Considerations

1. **Primary Lookups**: All collections have unique indexes on their primary ID fields
2. **Foreign Key Lookups**: Indexes on foreign key fields (merchantId, quoteId, shipmentId)
3. **Query Patterns**: Compound indexes support common query patterns:
   - Merchant history queries (merchantId + createdAt)
   - Status-based queries (status + updatedAt)
   - Date range queries (effectiveFrom + effectiveTo)
4. **Text Search**: Consider text indexes on companyName, trackingNumber if full-text search is needed

### Index Maintenance

- Indexes are created automatically on application startup
- Monitor index usage and remove unused indexes
- Consider partial indexes for filtered queries (e.g., only active merchants)

---

## Data Migration

### Initial Setup

1. Create all collections
2. Create all indexes
3. Insert default/global markup configurations
4. Set up TTL indexes if needed for data retention

### Schema Evolution

- MongoDB is schema-less, but application code enforces schema
- Use version numbers in documents if schema changes are needed
- Migrate existing documents when schema changes

---

## Security Considerations

1. **API Keys**: Always stored as hashes, never plain text
2. **Sensitive Data**: Consider encrypting sensitive fields (e.g., customer addresses)
3. **Access Control**: Use MongoDB authentication and authorization
4. **Audit Trail**: All webhook events are stored for compliance
5. **Data Retention**: Implement policies for old data cleanup

---

## Example Documents

### Example Quote Document

```json
{
  "_id": ObjectId("..."),
  "quoteId": "550e8400-e29b-41d4-a716-446655440000",
  "merchantId": "123e4567-e89b-12d3-a456-426614174000",
  "shipmentDetails": {
    "dimensions": {
      "length": 10.5,
      "width": 5.0,
      "height": 3.0
    },
    "weight": 2.5,
    "origin": {
      "addressLine1": "123 Main St",
      "city": "New York",
      "state": "NY",
      "postalCode": "10001",
      "country": "US"
    },
    "destination": {
      "addressLine1": "456 Oak Ave",
      "city": "Los Angeles",
      "state": "CA",
      "postalCode": "90001",
      "country": "US"
    },
    "serviceLevel": "standard"
  },
  "carrierRates": [
    {
      "carrier": "UPS",
      "rate": 15.50,
      "estimatedDays": 3,
      "serviceName": "Ground"
    },
    {
      "carrier": "FedEx",
      "rate": 18.75,
      "estimatedDays": 2,
      "serviceName": "Standard"
    }
  ],
  "clientQuotes": [
    {
      "carrier": "UPS",
      "carrierRate": 15.50,
      "markupAmount": 3.10,
      "clientPrice": 18.60,
      "markupConfigId": "789e0123-e45f-67g8-h901-234567890123"
    },
    {
      "carrier": "FedEx",
      "carrierRate": 18.75,
      "markupAmount": 3.75,
      "clientPrice": 22.50,
      "markupConfigId": "789e0123-e45f-67g8-h901-234567890123"
    }
  ],
  "expiresAt": "2025-01-16T12:00:00Z",
  "createdAt": "2025-01-15T12:00:00Z",
  "updatedAt": "2025-01-15T12:00:00Z"
}
```

### Example Shipment Document

```json
{
  "_id": ObjectId("..."),
  "shipmentId": "660e8400-e29b-41d4-a716-446655440001",
  "merchantId": "123e4567-e89b-12d3-a456-426614174000",
  "quoteId": "550e8400-e29b-41d4-a716-446655440000",
  "carrierShipmentId": "ISHIP-12345",
  "trackingNumber": "1Z999AA10123456784",
  "status": "InTransit",
  "label": {
    "pdfUrl": "https://api.iship.com/labels/12345.pdf",
    "signedUrl": "https://api.iship.com/labels/12345.pdf?signature=..."
  },
  "amountCharged": 18.60,
  "carrierCost": 15.50,
  "markupAmount": 3.10,
  "customerId": "789e0123-e45f-67g8-h901-234567890123",
  "createdAt": "2025-01-15T10:00:00Z",
  "updatedAt": "2025-01-16T14:00:00Z",
  "statusHistory": [
    {
      "status": "QuoteRequested",
      "timestamp": "2025-01-15T09:00:00Z",
      "source": "api"
    },
    {
      "status": "ShipmentCreated",
      "timestamp": "2025-01-15T10:00:00Z",
      "source": "api"
    },
    {
      "status": "InTransit",
      "timestamp": "2025-01-16T14:00:00Z",
      "source": "webhook"
    }
  ]
}
```

---

## Version History

- **v1.0** (2025-01-15): Initial schema design
  - All 6 collections defined
  - Indexes specified
  - Validation rules documented

---

**Document Status**: ✅ Complete - Ready for Implementation (Task 1.3)




