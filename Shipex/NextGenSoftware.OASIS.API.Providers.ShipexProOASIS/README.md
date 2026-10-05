# Shipex Pro OASIS Provider

## Overview

Shipex Pro OASIS Provider is a logistics middleware provider that enables merchants to integrate shipping services through a unified API. The system integrates with:

- **Shipox**: Order management and customer UI
- **iShip**: Carrier services (rates, labels, tracking)
- **QuickBooks**: Automated invoicing and payment tracking

## Project Structure

```
NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/
├── Models/              # Data models for quotes, shipments, invoices, etc.
├── Repositories/        # MongoDB repository layer
├── Services/            # Core service interfaces and implementations
├── Connectors/          # External API connectors (iShip, Shipox, QuickBooks)
├── Middleware/          # Middleware components
└── Helpers/             # Utility classes and helpers
```

## Configuration

Update `appsettings.json` with your MongoDB connection string and API credentials for iShip, Shipox, and QuickBooks.

## Getting Started

1. Ensure MongoDB is running and accessible
2. Update connection strings in `appsettings.json`
3. Configure API keys for external services
4. Build and run the project

## Dependencies

- .NET 8.0
- MongoDB.Driver 2.19.0
- NextGenSoftware.OASIS.API.Core
- NextGenSoftware.OASIS.Common

## Status

🚧 **In Development** - Core infrastructure being built




