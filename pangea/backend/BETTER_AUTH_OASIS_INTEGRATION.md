# Better-Auth + OASIS Integration Architecture

**Date:** December 23, 2025  
**Status:** Architecture Design  
**Goal:** Maintain OASIS Wallet API integration while using Better-Auth for authentication

---

## Overview

This document explains how to integrate Better-Auth (for authentication) with OASIS (for wallets and avatar features) in a clean, maintainable way.

---

## Architecture Pattern: Separation of Concerns

**Better-Auth:** Handles authentication (email/password, OAuth, 2FA, sessions)  
**OASIS:** Handles wallet generation, key management, blockchain operations  
**Link:** Optional mapping between Better-Auth users and OASIS avatars

---

## Core Principle: Lazy OASIS Avatar Creation

**Key Insight:** Not all users need OASIS avatars immediately.

**Strategy:**
1. User registers/logs in via Better-Auth → No OASIS avatar yet
2. User accesses wallet feature → Create OASIS avatar on-demand
3. Store mapping: `Better-Auth User ID` → `OASIS Avatar ID`
4. Use OASIS Avatar ID for all wallet operations

**Benefits:**
- ✅ Users who never use wallets don't need OASIS avatars
- ✅ Simpler registration flow (no OASIS dependency)
- ✅ OASIS avatar creation is explicit (when needed)

---

## Database Schema

### Better-Auth Tables (Standard)

```sql
-- Better-Auth's user table
CREATE TABLE "user" (
  id UUID PRIMARY KEY,
  email VARCHAR(255) UNIQUE NOT NULL,
  email_verified BOOLEAN DEFAULT false,
  name VARCHAR(255),
  image VARCHAR(255),
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW()
);

-- Better-Auth's session table
CREATE TABLE session (
  id VARCHAR(255) PRIMARY KEY,
  user_id UUID REFERENCES "user"(id) ON DELETE CASCADE,
  expires_at TIMESTAMP NOT NULL,
  token VARCHAR(255) UNIQUE NOT NULL,
  ip_address VARCHAR(45),
  user_agent TEXT,
  created_at TIMESTAMP DEFAULT NOW()
);

-- Better-Auth's account table (for OAuth)
CREATE TABLE account (
  id UUID PRIMARY KEY,
  user_id UUID REFERENCES "user"(id) ON DELETE CASCADE,
  account_id VARCHAR(255) NOT NULL,
  provider VARCHAR(50) NOT NULL, -- 'google', 'github', etc.
  access_token TEXT,
  refresh_token TEXT,
  expires_at TIMESTAMP,
  created_at TIMESTAMP DEFAULT NOW(),
  UNIQUE(provider, account_id)
);
```

### OASIS Integration Tables (Custom)

```sql
-- Mapping: Better-Auth User → OASIS Avatar
CREATE TABLE user_oasis_mapping (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL UNIQUE REFERENCES "user"(id) ON DELETE CASCADE,
  avatar_id VARCHAR(255) UNIQUE NOT NULL, -- OASIS Avatar ID
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW(),
  
  INDEX idx_user_oasis_mapping_user_id (user_id),
  INDEX idx_user_oasis_mapping_avatar_id (avatar_id)
);

-- Pangea-specific user data (role, KYC, etc.)
CREATE TABLE pangea_user_data (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL UNIQUE REFERENCES "user"(id) ON DELETE CASCADE,
  role VARCHAR(20) DEFAULT 'user',
  kyc_status VARCHAR(20) DEFAULT 'pending',
  is_active BOOLEAN DEFAULT true,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW(),
  
  INDEX idx_pangea_user_data_user_id (user_id),
  INDEX idx_pangea_user_data_role (role)
);
```

**Key Design Decisions:**
- ✅ Separate tables (not extending Better-Auth user table)
- ✅ `user_id` references Better-Auth user (foreign key)
- ✅ One-to-one mapping (one Better-Auth user → one OASIS avatar)
- ✅ Optional mapping (avatar only created when needed)

---

## Data Flow Diagrams

### Registration Flow (Email/Password)

```
User Registration
    ↓
Better-Auth → Create User in PostgreSQL
    ↓
Return Session Token
    ↓
[No OASIS avatar created yet]
```

**Code:**
```typescript
// User registers
const session = await authClient.signUp.email({
  email: 'user@example.com',
  password: 'password',
});

// Session is created, user is logged in
// OASIS avatar will be created when user accesses wallet features
```

---

### Registration Flow (Google OAuth)

```
User Clicks "Sign in with Google"
    ↓
Better-Auth → Google OAuth Flow
    ↓
Google Returns Profile
    ↓
Better-Auth → Create User in PostgreSQL
    ↓
Link Google Account to User
    ↓
Return Session Token
    ↓
[No OASIS avatar created yet]
```

**Code:**
```typescript
// OAuth flow
await authClient.signIn.social({
  provider: 'google',
  callbackURL: '/dashboard',
});

// Better-Auth handles the OAuth flow
// User is created automatically
// OASIS avatar created on-demand
```

---

### Wallet Generation Flow (Lazy OASIS Creation)

```
User Requests Wallet Generation
    ↓
Check: Does OASIS avatar exist? (user_oasis_mapping)
    ↓
[If NO]
    ↓
Create OASIS Avatar (via OASIS API)
    - Generate random password (user won't use it)
    - Register with OASIS using Better-Auth user's email
    - Get avatarId from OASIS
    ↓
Store Mapping: user_id → avatar_id
    ↓
[If YES]
    ↓
Get avatarId from mapping
    ↓
Call OASIS Wallet API to Generate Wallet
    ↓
Return Wallet to User
```

**Code:**
```typescript
@Post('wallet/generate')
async generateWallet(@Request() req: any, @Body() body: { providerType: string }) {
  const userId = req.user.id; // Better-Auth user ID
  const email = req.user.email;

  // Ensure OASIS avatar exists (lazy creation)
  const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email);

  // Generate wallet using OASIS API
  const wallet = await this.oasisWalletService.generateWallet(
    avatarId,
    body.providerType,
  );

  return { wallet };
}
```

---

## Implementation Details

### 1. OASIS Link Service

**File:** `src/auth/services/oasis-link.service.ts`

```typescript
import { Injectable, Logger } from '@nestjs/common';
import { InjectDataSource } from '@nestjs/typeorm';
import { DataSource } from 'typeorm';
import { OasisAuthService } from './oasis-auth.service';

@Injectable()
export class OasisLinkService {
  private readonly logger = new Logger(OasisLinkService.name);

  constructor(
    @InjectDataSource()
    private dataSource: DataSource,
    private oasisAuthService: OasisAuthService,
  ) {}

  /**
   * Get OASIS avatar ID for Better-Auth user (if exists)
   */
  async getAvatarId(userId: string): Promise<string | null> {
    const result = await this.dataSource.query(
      `SELECT avatar_id FROM user_oasis_mapping WHERE user_id = $1`,
      [userId]
    );

    return result.length > 0 ? result[0].avatar_id : null;
  }

  /**
   * Create OASIS avatar and link to Better-Auth user
   */
  async createAndLinkAvatar(userId: string, email: string, name?: string): Promise<string> {
    // Check if already linked
    const existing = await this.getAvatarId(userId);
    if (existing) {
      return existing;
    }

    // Generate random password (user won't use it - they use Better-Auth)
    const randomPassword = this.generateRandomPassword();
    
    // Split name into first/last (if provided)
    const nameParts = name?.split(' ') || [];
    const firstName = nameParts[0] || '';
    const lastName = nameParts.slice(1).join(' ') || '';

    try {
      // Create OASIS avatar
      const oasisAvatar = await this.oasisAuthService.register({
        email,
        password: randomPassword,
        username: email.split('@')[0],
        firstName,
        lastName,
      });

      // Store mapping
      await this.dataSource.query(
        `INSERT INTO user_oasis_mapping (user_id, avatar_id) 
         VALUES ($1, $2) 
         ON CONFLICT (user_id) DO NOTHING`,
        [userId, oasisAvatar.avatarId]
      );

      this.logger.log(`Created and linked OASIS avatar ${oasisAvatar.avatarId} for user ${userId}`);
      return oasisAvatar.avatarId;
    } catch (error: any) {
      this.logger.error(`Failed to create OASIS avatar: ${error.message}`);
      throw new Error(`Failed to create OASIS avatar: ${error.message}`);
    }
  }

  /**
   * Ensure OASIS avatar exists (lazy creation)
   * This is the main method called by wallet services
   */
  async ensureOasisAvatar(userId: string, email: string, name?: string): Promise<string> {
    let avatarId = await this.getAvatarId(userId);
    
    if (!avatarId) {
      avatarId = await this.createAndLinkAvatar(userId, email, name);
    }
    
    return avatarId;
  }

  /**
   * Get or create avatar (used when we know user will need OASIS features)
   */
  async getOrCreateAvatar(userId: string, email: string, name?: string): Promise<string> {
    return this.ensureOasisAvatar(userId, email, name);
  }

  private generateRandomPassword(): string {
    // Generate secure random password (user won't use it)
    return (
      Math.random().toString(36).slice(-12) +
      Math.random().toString(36).slice(-12) +
      Math.random().toString(36).slice(-12).toUpperCase() +
      '!@#'
    );
  }
}
```

---

### 2. Update OASIS Wallet Service

The existing `OasisWalletService` already uses `avatarId` - we just need to ensure it's called with the correct avatarId from the mapping.

**File:** `src/services/oasis-wallet.service.ts` (minimal changes needed)

```typescript
// This service stays mostly the same
// It already takes avatarId as parameter
// We just need to get avatarId from OasisLinkService before calling

// Example in wallet controller:
const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email);
const wallet = await this.oasisWalletService.generateWallet(avatarId, providerType);
```

---

### 3. Update Wallet Controller

**File:** `src/wallet/wallet.controller.ts`

```typescript
import { Controller, Get, Post, Body, UseGuards, Request } from '@nestjs/common';
import { BetterAuthGuard } from '../auth/guards/better-auth.guard';
import { OasisLinkService } from '../auth/services/oasis-link.service';
import { OasisWalletService } from '../services/oasis-wallet.service';
import { InjectDataSource } from '@nestjs/typeorm';
import { DataSource } from 'typeorm';

@Controller('wallet')
@UseGuards(BetterAuthGuard)
export class WalletController {
  constructor(
    private oasisLinkService: OasisLinkService,
    private oasisWalletService: OasisWalletService,
    @InjectDataSource()
    private dataSource: DataSource,
  ) {}

  /**
   * Generate wallet for authenticated user
   * POST /api/wallet/generate
   */
  @Post('generate')
  async generateWallet(
    @Request() req: any,
    @Body() body: { providerType: 'SolanaOASIS' | 'EthereumOASIS'; setAsDefault?: boolean },
  ) {
    const userId = req.user.id; // Better-Auth user ID
    const email = req.user.email;
    const name = req.user.name;

    // Ensure OASIS avatar exists (lazy creation)
    const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email, name);

    // Generate wallet using existing OASIS service
    const wallet = await this.oasisWalletService.generateWallet(
      avatarId,
      body.providerType,
      body.setAsDefault || true,
    );

    return {
      success: true,
      wallet,
      avatarId, // Include for reference
    };
  }

  /**
   * Get user's wallets
   * GET /api/wallet
   */
  @Get()
  async getWallets(@Request() req: any) {
    const userId = req.user.id;
    const email = req.user.email;

    // Ensure OASIS avatar exists
    const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email);

    // Get wallets from OASIS
    const wallets = await this.oasisWalletService.getWallets(avatarId);

    return {
      success: true,
      wallets,
    };
  }

  /**
   * Get wallet balance
   * GET /api/wallet/balance
   */
  @Get('balance')
  async getBalance(@Request() req: any) {
    const userId = req.user.id;
    const email = req.user.email;

    const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email);
    const wallets = await this.oasisWalletService.getWallets(avatarId);

    // Get balances for all wallets
    const balances = await Promise.all(
      wallets.map(async (wallet) => {
        try {
          const balance = await this.oasisWalletService.getBalance(
            wallet.walletId,
            wallet.providerType,
          );
          return { ...wallet, balance };
        } catch (error) {
          return { ...wallet, balance: null, error: error.message };
        }
      }),
    );

    return {
      success: true,
      balances,
    };
  }
}
```

---

### 4. OASIS Token Management (Still Needed)

We still need OASIS API tokens for wallet operations. This stays mostly the same.

**File:** `src/services/oasis-token-manager.service.ts` (keep as-is)

```typescript
// This service manages OASIS admin token for API calls
// It's used by OasisWalletService
// No changes needed - it's independent of user authentication
```

**Note:** OASIS token manager uses admin credentials, not user credentials. This is separate from user authentication.

---

## Alternative: Proactive OASIS Avatar Creation

Instead of lazy creation, we could create OASIS avatars immediately on registration:

### Option A: Lazy Creation (Recommended)
- ✅ Simpler registration
- ✅ Users without wallets don't need OASIS avatars
- ✅ Explicit wallet feature requirement

### Option B: Proactive Creation
- Create OASIS avatar during Better-Auth registration hook
- More complex (requires Better-Auth hooks/plugins)
- All users get OASIS avatars (even if they never use wallets)

**Recommendation:** Use **Lazy Creation** - cleaner separation of concerns.

---

## Error Handling

### OASIS Avatar Creation Failure

**Scenario:** User tries to generate wallet, but OASIS avatar creation fails.

**Handling:**
```typescript
async ensureOasisAvatar(userId: string, email: string): Promise<string> {
  try {
    // Try to create avatar
    return await this.createAndLinkAvatar(userId, email);
  } catch (error) {
    // Check if avatar was created but mapping failed
    // Try to recover by finding existing avatar
    
    // Log error for monitoring
    this.logger.error(`Failed to ensure OASIS avatar for user ${userId}: ${error.message}`);
    
    // Throw user-friendly error
    throw new HttpException(
      'Unable to set up wallet. Please try again later.',
      HttpStatus.SERVICE_UNAVAILABLE
    );
  }
}
```

---

## Benefits of This Architecture

✅ **Separation of Concerns:**
- Better-Auth handles authentication (email/password, OAuth, 2FA)
- OASIS handles wallets/blockchain operations
- Clear boundaries

✅ **Flexibility:**
- Users can exist without OASIS avatars
- OASIS features are opt-in (when needed)
- Can add/remove OASIS integration without affecting auth

✅ **Maintainability:**
- Each system has clear responsibilities
- Changes to Better-Auth don't affect OASIS
- Changes to OASIS don't affect authentication

✅ **Performance:**
- No OASIS API calls during registration/login
- OASIS avatar creation only when needed
- Faster authentication flow

✅ **User Experience:**
- Modern auth (OAuth, 2FA, magic links)
- No password required for OAuth users
- Wallet features work seamlessly when accessed

---

## Migration from Existing Users

If migrating existing users who already have OASIS avatars:

**Migration Script:**
```typescript
// For each existing user
const betterAuthUserId = /* migrated Better-Auth user ID */;
const existingAvatarId = /* existing OASIS avatarId */;

// Create mapping
await dataSource.query(
  `INSERT INTO user_oasis_mapping (user_id, avatar_id) 
   VALUES ($1, $2)`,
  [betterAuthUserId, existingAvatarId]
);
```

---

## Future Enhancements

### 1. OASIS Avatar Sync (Optional)

If you want to sync Better-Auth user profile to OASIS avatar:

```typescript
@Post('profile/update')
async updateProfile(@Request() req: any, @Body() body: { name?: string }) {
  const userId = req.user.id;
  const avatarId = await this.oasisLinkService.getAvatarId(userId);
  
  if (avatarId) {
    // Update OASIS avatar profile
    await this.oasisAuthService.updateUserProfile(avatarId, {
      firstName: body.name?.split(' ')[0],
      lastName: body.name?.split(' ').slice(1).join(' '),
    });
  }
  
  // Update Better-Auth user (handled by Better-Auth)
  // ...
}
```

### 2. OASIS Karma/Reputation (If Needed)

If you want to display OASIS karma scores:

```typescript
@Get('profile')
async getProfile(@Request() req: any) {
  const userId = req.user.id;
  const avatarId = await this.oasisLinkService.getAvatarId(userId);
  
  const profile = {
    // Better-Auth user data
    id: req.user.id,
    email: req.user.email,
    name: req.user.name,
    
    // OASIS data (if avatar exists)
    ...(avatarId && {
      oasis: {
        avatarId,
        karma: await this.oasisAuthService.getKarma(avatarId),
        // ... other OASIS data
      },
    }),
  };
  
  return profile;
}
```

---

## Testing Strategy

### Unit Tests

```typescript
describe('OasisLinkService', () => {
  it('should create OASIS avatar on first wallet access', async () => {
    const userId = 'test-user-id';
    const email = 'test@example.com';
    
    const avatarId = await service.ensureOasisAvatar(userId, email);
    
    expect(avatarId).toBeDefined();
    const mapping = await getMapping(userId);
    expect(mapping.avatar_id).toBe(avatarId);
  });
  
  it('should reuse existing avatar on subsequent calls', async () => {
    const userId = 'test-user-id';
    const email = 'test@example.com';
    
    const avatarId1 = await service.ensureOasisAvatar(userId, email);
    const avatarId2 = await service.ensureOasisAvatar(userId, email);
    
    expect(avatarId1).toBe(avatarId2);
  });
});
```

### Integration Tests

```typescript
describe('Wallet Generation with Better-Auth', () => {
  it('should generate wallet for Better-Auth user', async () => {
    // 1. Register user via Better-Auth
    const session = await authClient.signUp.email({
      email: 'test@example.com',
      password: 'password',
    });
    
    // 2. Generate wallet (should create OASIS avatar)
    const response = await fetch('/api/wallet/generate', {
      method: 'POST',
      headers: { Cookie: session.cookie },
      body: JSON.stringify({ providerType: 'SolanaOASIS' }),
    });
    
    expect(response.status).toBe(200);
    const { wallet } = await response.json();
    expect(wallet.walletAddress).toBeDefined();
  });
});
```

---

## Monitoring & Logging

### Key Metrics to Track

1. **OASIS Avatar Creation:**
   - Count of avatars created (lazy creation)
   - Time to create avatar
   - Failure rate

2. **Wallet Operations:**
   - Wallet generation success rate
   - Average time to generate wallet
   - OASIS API error rate

3. **User Behavior:**
   - % of users who create wallets
   - % of users who use OAuth vs email/password
   - Session duration

### Logging Strategy

```typescript
// In OasisLinkService
this.logger.log(`Creating OASIS avatar for Better-Auth user ${userId}`);
this.logger.debug(`OASIS avatar created: ${avatarId}`);

// In WalletController
this.logger.log(`Wallet generation requested by user ${userId}, avatarId: ${avatarId}`);
this.logger.error(`Wallet generation failed: ${error.message}`, error.stack);
```

---

## Security Considerations

1. **OASIS Avatar Passwords:**
   - Random passwords generated (users never see/use them)
   - Passwords stored securely in OASIS (not in Pangea DB)
   - Users authenticate via Better-Auth only

2. **Mapping Table Security:**
   - Foreign key constraints ensure data integrity
   - Cascade delete (if Better-Auth user deleted, mapping deleted)
   - Unique constraints prevent duplicate mappings

3. **OASIS API Token:**
   - Admin token stored securely (environment variable)
   - Token refresh handled automatically
   - Not exposed to frontend

---

## Summary

**Architecture:**
- Better-Auth = Authentication (email/password, OAuth, 2FA, sessions)
- OASIS = Wallet/Blockchain operations (on-demand avatar creation)
- Mapping table = Links Better-Auth users to OASIS avatars

**Key Benefits:**
- ✅ Clean separation of concerns
- ✅ Modern authentication features
- ✅ OASIS wallet features still available
- ✅ Lazy avatar creation (only when needed)
- ✅ Flexible and maintainable

**Implementation:**
- Minimal changes to existing OASIS wallet service
- New OASIS link service handles mapping
- Wallet controllers updated to use Better-Auth + OASIS link

---

**Last Updated:** December 23, 2025  
**Status:** Architecture Complete - Ready for Implementation
