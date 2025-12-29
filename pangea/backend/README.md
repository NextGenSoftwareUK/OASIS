# Pangea Markets Backend

Backend API for Pangea Markets RWA (Real World Asset) tokenization and trading platform.

## 🚀 Quick Start

### Prerequisites

- Node.js 18+ and npm/yarn
- PostgreSQL 14+
- Redis 6+

### Installation

1. **Clone and navigate to the backend directory:**
   ```bash
   cd backend
   ```

2. **Install dependencies:**
   ```bash
   npm install
   # or
   yarn install
   ```

3. **Set up environment variables:**
   ```bash
   cp .env.example .env
   ```
   
   Edit `.env` and configure:
   - Database connection (PostgreSQL)
   - Redis connection
   - OASIS API credentials
   - JWT secret
   - Blockchain RPC URLs

4. **Set up PostgreSQL database:**
   ```bash
   # Create database
   createdb pangea
   
   # Or using psql
   psql -U postgres
   CREATE DATABASE pangea;
   ```

5. **Start Redis:**
   ```bash
   # Using Docker
   docker run -d -p 6379:6379 redis:latest
   
   # Or using local installation
   redis-server
   ```

6. **Run the application:**
   ```bash
   # Development mode (with hot-reload)
   npm run start:dev
   
   # Production mode
   npm run build
   npm run start:prod
   ```

7. **Verify the setup:**
   ```bash
   curl http://localhost:3000/api/health
   ```

   Expected response:
   ```json
   {
     "status": "ok",
     "timestamp": "2025-01-27T...",
     "service": "Pangea Markets Backend",
     "version": "1.0.0",
     "checks": {
       "database": { "status": "ok", "message": "Database connection successful" },
       "redis": { "status": "ok", "message": "Redis connection successful" }
     }
   }
   ```

## 📁 Project Structure

```
backend/
├── src/
│   ├── auth/              # Authentication module
│   ├── users/             # User management
│   ├── assets/            # Asset management
│   ├── orders/            # Order management
│   ├── trades/            # Trade execution
│   ├── wallet/            # Wallet integration
│   ├── transactions/      # Deposits/withdrawals
│   ├── admin/             # Admin endpoints
│   ├── blockchain/        # Blockchain services
│   ├── smart-contracts/   # Smart contract integration
│   ├── common/            # Shared utilities
│   ├── config/            # Configuration
│   ├── app.module.ts      # Root module
│   ├── app.controller.ts  # Root controller
│   ├── app.service.ts     # Root service
│   └── main.ts            # Application entry point
├── tests/                 # Test files
├── migrations/            # Database migrations
├── package.json
├── tsconfig.json
└── .env.example
```

## 🛠️ Development

### Available Scripts

- `npm run start:dev` - Start development server with hot-reload
- `npm run build` - Build for production
- `npm run start:prod` - Start production server
- `npm run lint` - Run ESLint
- `npm run format` - Format code with Prettier
- `npm test` - Run unit tests
- `npm run test:e2e` - Run end-to-end tests

### Database Migrations

```bash
# Generate migration
npm run migration:generate -- migrations/MigrationName

# Run migrations
npm run migration:run

# Revert last migration
npm run migration:revert
```

## 🔧 Configuration

### Environment Variables

See `.env.example` for all available configuration options:

- **Database**: PostgreSQL connection string or individual parameters
- **Redis**: Redis connection URL or host/port
- **OASIS API**: API URL and key for OASIS platform integration
- **Smart Contract Generator**: URL for contract generation service
- **JWT**: Secret and expiration for authentication tokens
- **Blockchain**: RPC URLs for Solana and Ethereum networks
- **CORS**: Allowed origins for cross-origin requests

## 📚 Technology Stack

- **Framework**: NestJS (TypeScript)
- **Database**: PostgreSQL with TypeORM
- **Cache**: Redis (ioredis)
- **Validation**: class-validator, class-transformer
- **Authentication**: JWT (passport-jwt)
- **Testing**: Jest

## 🔗 API Endpoints

### Health Check
- `GET /api/health` - Service health status

More endpoints will be added as modules are implemented.

## 🧪 Testing

```bash
# Unit tests
npm test

# E2E tests
npm run test:e2e

# Test coverage
npm run test:cov
```

## 📝 Code Style

This project uses:
- **ESLint** for linting
- **Prettier** for code formatting

Run `npm run lint` to check for issues and `npm run format` to auto-fix formatting.

## 🚢 Deployment

1. Build the application:
   ```bash
   npm run build
   ```

2. Set production environment variables

3. Run migrations:
   ```bash
   npm run migration:run
   ```

4. Start the server:
   ```bash
   npm run start:prod
   ```

## 📖 Documentation

- [Implementation Plan](../IMPLEMENTATION_PLAN.md)
- [Task Briefs](../task-briefs/)
- [OASIS API Integration](../OASIS_API_REUSE_ANALYSIS.md)

## 🤝 Contributing

Follow the task briefs in `../task-briefs/` for implementation guidelines.

## 📄 License

Private - Pangea Markets












