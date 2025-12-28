# OASIS Authentication Integration

This module implements OASIS Avatar API authentication integration for Pangea Markets, following the Shipex Pro pattern (Option A).

## Architecture

### Pattern: Hybrid Approach
- **OASIS API**: Used for authentication verification (login/register)
- **Local Database**: Stores user data linked by `avatarId`
- **Pangea JWT**: Generates our own JWT tokens (not OASIS tokens)

### Flow

1. **Registration/Login**:
   - User registers/logs in via OASIS Avatar API
   - OASIS returns avatar data with `avatarId`
   - Avatar data is synced to local `users` table
   - Pangea generates its own JWT token

2. **Authentication**:
   - Client sends Pangea JWT token in `Authorization: Bearer <token>` header
   - JWT Guard validates token and extracts user ID
   - User data is loaded from local database

## Services

### `OasisAuthService`
- Wraps OASIS Avatar API calls
- Handles nested response structures
- Methods: `register()`, `login()`, `getUserProfile()`, `updateUserProfile()`, etc.

### `UserSyncService`
- Maps OASIS Avatar to local User entity
- Creates or updates user records
- Links by `avatarId`

### `AuthService`
- Main authentication service
- Generates Pangea JWT tokens
- Orchestrates OASIS + local database operations

## Endpoints

### Public Endpoints
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password

### Protected Endpoints (Require JWT)
- `GET /api/user/profile` - Get current user profile
- `PUT /api/user/profile` - Update user profile

## Usage

### Register
```typescript
POST /api/auth/register
{
  "email": "user@example.com",
  "password": "password123",
  "username": "username",
  "firstName": "John",
  "lastName": "Doe"
}
```

### Login
```typescript
POST /api/auth/login
{
  "email": "user@example.com",
  "password": "password123"
}
```

### Protected Route
```typescript
GET /api/user/profile
Headers: {
  "Authorization": "Bearer <jwt_token>"
}
```

## Guards

### `JwtAuthGuard`
- Validates JWT tokens
- Extracts user from token
- Attaches user to `request.user`

### Usage in Controllers
```typescript
@UseGuards(JwtAuthGuard)
@Get('protected')
async protectedRoute(@CurrentUser() user: any) {
  // user is available here
}
```

## Decorators

### `@Public()`
Marks route as public (no authentication required)

### `@CurrentUser()`
Extracts current user from request

## Environment Variables

```env
OASIS_API_URL=https://api.oasisplatform.world
JWT_SECRET=YourSuperSecretKeyForJWTTokenGenerationThatShouldBeAtLeast32Characters
JWT_EXPIRES_IN=7d
```

## Database

### User Entity
- `id` (UUID) - Primary key
- `email` - Unique email
- `username` - Username
- `avatarId` - OASIS Avatar ID (unique, links to OASIS)
- `walletAddressSolana` - Solana wallet address
- `walletAddressEthereum` - Ethereum wallet address
- `role` - User role (default: 'user')
- `kycStatus` - KYC status (default: 'pending')
- `isActive` - Active status
- Timestamps: `createdAt`, `updatedAt`, `lastLogin`

## Testing

See `auth.service.spec.ts` for unit tests.

## Notes

- OASIS JWT tokens are stored but not used for authentication
- Pangea generates its own JWT tokens for better control
- User data is cached locally to reduce OASIS API calls
- Handles nested OASIS response structures automatically












