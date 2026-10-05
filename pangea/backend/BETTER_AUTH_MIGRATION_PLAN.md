# Better-Auth Migration Plan - Complete Implementation Guide

**Date:** December 23, 2025  
**Status:** Ready for Implementation  
**Context:** Pre-launch migration from OASIS auth to Better-Auth

---

## Overview

This document provides a complete, step-by-step migration plan to replace the current OASIS-based authentication system with Better-Auth while maintaining OASIS Wallet API integration.

---

## Migration Strategy

### Approach: Gradual Migration with Parallel Systems

Since we're **pre-launch**, we can do a clean migration:

1. ✅ **Install Better-Auth** alongside existing auth
2. ✅ **Run migrations** to create Better-Auth tables
3. ✅ **Migrate existing users** (if any) from OASIS → Better-Auth
4. ✅ **Update auth endpoints** to use Better-Auth
5. ✅ **Maintain OASIS Wallet API** integration (via avatarId mapping)
6. ✅ **Remove old OASIS auth code** once verified

---

## Prerequisites

### Current System Analysis

**Current User Table:**
```typescript
users
├── id (UUID, Primary Key)
├── email (unique)
├── password_hash (nullable - OASIS auth only)
├── username
├── firstName
├── lastName
├── avatar_id (unique, nullable) - OASIS Avatar ID
├── wallet_address_solana
├── wallet_address_ethereum
├── role
├── kyc_status
├── is_active
├── last_login
└── timestamps
```

**Current Flow:**
```
Registration/Login → OASIS Avatar API → Sync to PostgreSQL → Pangea JWT
```

**Target Flow:**
```
Registration/Login → Better-Auth (PostgreSQL) → Link to OASIS Avatar (optional) → Session Token
```

---

## Step 1: Install Dependencies

```bash
cd pangea/backend
npm install better-auth @hedystia/better-auth-typeorm
npm install --save-dev @types/better-auth
```

**Note:** We'll use the TypeORM adapter since we're already using TypeORM.

---

## Step 2: Generate Better-Auth Schema

Better-Auth requires specific tables. We'll use their CLI to generate TypeORM entities:

```bash
npx better-auth generate
```

This will create:
- TypeORM entities for Better-Auth tables
- Migration files

**Better-Auth Tables:**
- `user` - User accounts
- `session` - Active sessions
- `account` - OAuth account linking
- `verification` - Email verification tokens
- `api_key` - API keys (if needed)

---

## Step 3: Database Migration Strategy

### Option A: Use Better-Auth's User Table (Recommended)

**Benefits:**
- Standard Better-Auth schema
- Built-in features work out of the box
- Type-safe

**Approach:**
1. Create Better-Auth tables alongside existing `users` table
2. Migrate data from `users` → Better-Auth `user` table
3. Drop old `users` table (or rename to `users_old` for backup)

### Option B: Extend Better-Auth User Table (Custom Fields)

**Benefits:**
- Keep existing schema structure
- Add Better-Auth columns to existing table

**Approach:**
1. Add Better-Auth columns to existing `users` table
2. Migrate existing data
3. Map Better-Auth fields

**Recommendation:** Use **Option A** - cleaner, standard approach.

---

## Step 4: Create Migration Script

### 4.1 Backup Existing Data

```sql
-- Create backup table
CREATE TABLE users_backup AS SELECT * FROM users;

-- Export to CSV
COPY users TO '/tmp/users_backup.csv' CSV HEADER;
```

### 4.2 Create Better-Auth Tables

Run Better-Auth migrations:

```bash
npm run migration:run
# Or use TypeORM CLI
npx typeorm migration:run
```

### 4.3 Migrate User Data

**Migration Script:** `scripts/migrate-to-better-auth.ts`

```typescript
import { DataSource } from 'typeorm';
import { config } from 'dotenv';
import * as bcrypt from 'bcrypt';

config();

async function migrateUsers() {
  const dataSource = new DataSource({
    type: 'postgres',
    url: process.env.DATABASE_URL,
  });

  await dataSource.initialize();

  // Get existing users
  const oldUsers = await dataSource.query(`
    SELECT id, email, username, "firstName", "lastName", 
           avatar_id, role, kyc_status, is_active, created_at, updated_at
    FROM users
  `);

  console.log(`Migrating ${oldUsers.length} users...`);

  for (const oldUser of oldUsers) {
    // Check if user already exists in Better-Auth
    const existing = await dataSource.query(
      `SELECT id FROM "user" WHERE email = $1`,
      [oldUser.email]
    );

    if (existing.length > 0) {
      console.log(`User ${oldUser.email} already exists, skipping...`);
      continue;
    }

    // Create Better-Auth user
    // Note: Better-Auth will handle password hashing, but we need to handle
    // existing users who don't have passwords (OASIS-only users)
    const userId = crypto.randomUUID();
    const createdAt = oldUser.created_at || new Date();
    const updatedAt = oldUser.updated_at || new Date();

    await dataSource.query(
      `INSERT INTO "user" (
        id, email, name, email_verified, created_at, updated_at,
        image, username
      ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8)`,
      [
        userId,
        oldUser.email,
        `${oldUser.firstName || ''} ${oldUser.lastName || ''}`.trim() || null,
        false, // Email not verified yet (users will need to verify)
        createdAt,
        updatedAt,
        null, // Profile picture
        oldUser.username || oldUser.email.split('@')[0],
      ]
    );

    // Create mapping table entry (Better-Auth user ID → OASIS avatarId)
    if (oldUser.avatar_id) {
      await dataSource.query(
        `INSERT INTO user_oasis_mapping (
          user_id, avatar_id, created_at
        ) VALUES ($1, $2, $3)`,
        [userId, oldUser.avatar_id, createdAt]
      );
    }

    // Create Pangea-specific user data
    await dataSource.query(
      `INSERT INTO pangea_user_data (
        user_id, role, kyc_status, is_active, created_at, updated_at
      ) VALUES ($1, $2, $3, $4, $5, $6)`,
      [
        userId,
        oldUser.role || 'user',
        oldUser.kyc_status || 'pending',
        oldUser.is_active !== false,
        createdAt,
        updatedAt,
      ]
    );

    console.log(`Migrated user: ${oldUser.email}`);
  }

  console.log('Migration complete!');
  await dataSource.destroy();
}

migrateUsers().catch(console.error);
```

### 4.4 Create Supporting Tables

**File:** `migrations/XXXXX-CreateOasisMappingTables.ts`

```typescript
import { MigrationInterface, QueryRunner } from 'typeorm';

export class CreateOasisMappingTables1234567890 implements MigrationInterface {
  public async up(queryRunner: QueryRunner): Promise<void> {
    // Mapping table: Better-Auth user ID → OASIS avatarId
    await queryRunner.query(`
      CREATE TABLE IF NOT EXISTS user_oasis_mapping (
        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
        user_id UUID NOT NULL REFERENCES "user"(id) ON DELETE CASCADE,
        avatar_id VARCHAR(255) UNIQUE NOT NULL,
        created_at TIMESTAMP DEFAULT NOW(),
        updated_at TIMESTAMP DEFAULT NOW(),
        INDEX idx_user_oasis_mapping_user_id (user_id),
        INDEX idx_user_oasis_mapping_avatar_id (avatar_id)
      );
    `);

    // Pangea-specific user data (role, KYC status, etc.)
    await queryRunner.query(`
      CREATE TABLE IF NOT EXISTS pangea_user_data (
        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
        user_id UUID NOT NULL UNIQUE REFERENCES "user"(id) ON DELETE CASCADE,
        role VARCHAR(20) DEFAULT 'user',
        kyc_status VARCHAR(20) DEFAULT 'pending',
        is_active BOOLEAN DEFAULT true,
        created_at TIMESTAMP DEFAULT NOW(),
        updated_at TIMESTAMP DEFAULT NOW(),
        INDEX idx_pangea_user_data_user_id (user_id)
      );
    `);
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP TABLE IF EXISTS pangea_user_data;`);
    await queryRunner.query(`DROP TABLE IF EXISTS user_oasis_mapping;`);
  }
}
```

---

## Step 5: Setup Better-Auth Configuration

### 5.1 Create Auth Configuration

**File:** `src/auth/better-auth.config.ts`

```typescript
import { betterAuth } from 'better-auth';
import { typeormAdapter } from '@hedystia/better-auth-typeorm';
import { DataSource } from 'typeorm';
import { ConfigService } from '@nestjs/config';

export function createBetterAuth(dataSource: DataSource, configService: ConfigService) {
  return betterAuth({
    database: typeormAdapter(dataSource),
    
    emailAndPassword: {
      enabled: true,
      requireEmailVerification: false, // Set to true in production
    },
    
    socialProviders: {
      google: {
        clientId: configService.get<string>('GOOGLE_CLIENT_ID') || '',
        clientSecret: configService.get<string>('GOOGLE_CLIENT_SECRET') || '',
      },
      github: {
        clientId: configService.get<string>('GITHUB_CLIENT_ID') || '',
        clientSecret: configService.get<string>('GITHUB_CLIENT_SECRET') || '',
      },
      // Add more providers as needed
    },
    
    session: {
      expiresIn: 60 * 60 * 24 * 7, // 7 days
      updateAge: 60 * 60 * 24, // 1 day
    },
    
    advanced: {
      useSecureCookies: process.env.NODE_ENV === 'production',
      cookiePrefix: 'pangea',
    },
    
    // Base URL for callbacks
    baseURL: configService.get<string>('BASE_URL') || 'http://localhost:3000',
    basePath: '/api/auth',
  });
}
```

### 5.2 Create Better-Auth Service

**File:** `src/auth/services/better-auth.service.ts`

```typescript
import { Injectable } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { DataSource } from 'typeorm';
import { createBetterAuth } from '../better-auth.config';

@Injectable()
export class BetterAuthService {
  private auth: ReturnType<typeof createBetterAuth>;

  constructor(
    private dataSource: DataSource,
    private configService: ConfigService,
  ) {
    this.auth = createBetterAuth(this.dataSource, this.configService);
  }

  getAuth() {
    return this.auth;
  }

  getHandler() {
    return this.auth.handler;
  }
}
```

---

## Step 6: Create NestJS Integration

### 6.1 Create Better-Auth Controller

**File:** `src/auth/controllers/better-auth.controller.ts`

```typescript
import { Controller, All, Req, Res } from '@nestjs/common';
import { Request, Response } from 'express';
import { BetterAuthService } from '../services/better-auth.service';

@Controller('auth')
export class BetterAuthController {
  constructor(private betterAuthService: BetterAuthService) {}

  @All('*')
  async handleAuth(@Req() req: Request, @Res() res: Response) {
    const handler = this.betterAuthService.getHandler();
    return handler(req, res);
  }
}
```

### 6.2 Update Auth Module

**File:** `src/auth/auth.module.ts`

```typescript
import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { BetterAuthService } from './services/better-auth.service';
import { BetterAuthController } from './controllers/better-auth.controller';
import { OasisWalletService } from '../services/oasis-wallet.service';
import { OasisModule } from '../services/oasis.module';
// Keep OASIS wallet services

@Module({
  imports: [
    TypeOrmModule.forFeature([
      // Better-Auth entities will be auto-loaded
    ]),
    OasisModule, // Keep for wallet functionality
  ],
  controllers: [BetterAuthController],
  providers: [BetterAuthService, OasisWalletService],
  exports: [BetterAuthService],
})
export class AuthModule {}
```

### 6.3 Update Main.ts

**Important:** Better-Auth requires raw body access for some endpoints.

**File:** `src/main.ts`

```typescript
async function bootstrap() {
  const app = await NestFactory.create(AppModule, {
    // Better-Auth needs raw body for some endpoints
    // We'll handle this in the controller
  });

  // ... rest of config
  await app.listen(port, '0.0.0.0');
}
```

---

## Step 7: Create OASIS Integration Service

Since we're keeping OASIS Wallet API, we need to link Better-Auth users to OASIS avatars.

### 7.1 Create OASIS Link Service

**File:** `src/auth/services/oasis-link.service.ts`

```typescript
import { Injectable, Logger } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { DataSource } from 'typeorm';
import { OasisAuthService } from './oasis-auth.service';
import { OasisWalletService } from '../../services/oasis-wallet.service';

@Injectable()
export class OasisLinkService {
  private readonly logger = new Logger(OasisLinkService.name);

  constructor(
    private dataSource: DataSource,
    private oasisAuthService: OasisAuthService,
    private oasisWalletService: OasisWalletService,
  ) {}

  /**
   * Link Better-Auth user to OASIS Avatar (creates avatar if doesn't exist)
   * Called when user first accesses wallet features
   */
  async linkOrCreateOasisAvatar(userId: string, email: string): Promise<string> {
    // Check if mapping already exists
    const mapping = await this.dataSource.query(
      `SELECT avatar_id FROM user_oasis_mapping WHERE user_id = $1`,
      [userId]
    );

    if (mapping.length > 0) {
      return mapping[0].avatar_id;
    }

    // Create OASIS avatar for this user
    // Generate random password (user won't use it - they use Better-Auth)
    const randomPassword = this.generateRandomPassword();

    try {
      const oasisAvatar = await this.oasisAuthService.register({
        email,
        password: randomPassword,
        username: email.split('@')[0],
        firstName: null,
        lastName: null,
      });

      // Create mapping
      await this.dataSource.query(
        `INSERT INTO user_oasis_mapping (user_id, avatar_id) VALUES ($1, $2)`,
        [userId, oasisAvatar.avatarId]
      );

      this.logger.log(`Created and linked OASIS avatar ${oasisAvatar.avatarId} for user ${userId}`);
      return oasisAvatar.avatarId;
    } catch (error: any) {
      // If avatar already exists (by email), try to authenticate to get avatarId
      this.logger.warn(`Failed to create OASIS avatar, may already exist: ${error.message}`);
      throw new Error('Failed to link OASIS avatar');
    }
  }

  /**
   * Get OASIS avatar ID for Better-Auth user
   */
  async getAvatarId(userId: string): Promise<string | null> {
    const mapping = await this.dataSource.query(
      `SELECT avatar_id FROM user_oasis_mapping WHERE user_id = $1`,
      [userId]
    );

    return mapping.length > 0 ? mapping[0].avatar_id : null;
  }

  /**
   * Ensure OASIS avatar exists (lazy creation)
   */
  async ensureOasisAvatar(userId: string, email: string): Promise<string> {
    let avatarId = await this.getAvatarId(userId);
    
    if (!avatarId) {
      avatarId = await this.linkOrCreateOasisAvatar(userId, email);
    }
    
    return avatarId;
  }

  private generateRandomPassword(): string {
    return (
      Math.random().toString(36).slice(-12) +
      Math.random().toString(36).slice(-12) +
      'A1!'
    );
  }
}
```

---

## Step 8: Update Wallet Controller to Use Better-Auth

**File:** `src/wallet/wallet.controller.ts`

```typescript
import { Controller, Get, Post, Body, UseGuards, Request } from '@nestjs/common';
import { BetterAuthGuard } from '../auth/guards/better-auth.guard'; // We'll create this
import { OasisLinkService } from '../auth/services/oasis-link.service';
import { OasisWalletService } from '../services/oasis-wallet.service';

@Controller('wallet')
@UseGuards(BetterAuthGuard) // Better-Auth guard instead of JWT guard
export class WalletController {
  constructor(
    private oasisLinkService: OasisLinkService,
    private oasisWalletService: OasisWalletService,
  ) {}

  @Post('generate')
  async generateWallet(@Request() req: any, @Body() body: { providerType: string }) {
    const userId = req.user.id; // Better-Auth user ID
    const email = req.user.email;

    // Ensure OASIS avatar exists (lazy creation)
    const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email);

    // Use existing OASIS wallet service
    const wallet = await this.oasisWalletService.generateWallet(
      avatarId,
      body.providerType as 'SolanaOASIS' | 'EthereumOASIS',
    );

    return { wallet };
  }

  @Get('balance')
  async getBalance(@Request() req: any) {
    const userId = req.user.id;
    const email = req.user.email;

    const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email);
    const wallets = await this.oasisWalletService.getWallets(avatarId);

    return { wallets };
  }
}
```

### 8.1 Create Better-Auth Guard

**File:** `src/auth/guards/better-auth.guard.ts`

```typescript
import { Injectable, CanActivate, ExecutionContext, UnauthorizedException } from '@nestjs/common';
import { BetterAuthService } from '../services/better-auth.service';

@Injectable()
export class BetterAuthGuard implements CanActivate {
  constructor(private betterAuthService: BetterAuthService) {}

  async canActivate(context: ExecutionContext): Promise<boolean> {
    const request = context.switchToHttp().getRequest();
    const auth = this.betterAuthService.getAuth();

    try {
      const session = await auth.api.getSession({ headers: request.headers });
      
      if (!session?.user) {
        throw new UnauthorizedException('Not authenticated');
      }

      // Attach user to request
      request.user = session.user;
      request.session = session;

      return true;
    } catch (error) {
      throw new UnauthorizedException('Authentication failed');
    }
  }
}
```

---

## Step 9: Update Frontend Integration

### 9.1 Install Better-Auth Client

**Frontend:**
```bash
npm install better-auth
```

### 9.2 Create Auth Client

**File:** `frontend/lib/auth.ts`

```typescript
import { createAuthClient } from "better-auth/react";

export const authClient = createAuthClient({
  baseURL: process.env.NEXT_PUBLIC_API_URL || "https://api.pangea.com",
});

export const { signIn, signUp, signOut, useSession } = authClient;
```

### 9.3 Update Login/Register Components

**Before (OASIS):**
```typescript
const response = await fetch('/api/auth/login', {
  method: 'POST',
  body: JSON.stringify({ email, password }),
});
const { token } = await response.json();
localStorage.setItem('token', token);
```

**After (Better-Auth):**
```typescript
import { signIn } from '@/lib/auth';

// Email/password
await signIn.email({
  email: 'user@example.com',
  password: 'password',
});

// Google OAuth
await signIn.social({
  provider: 'google',
  callbackURL: '/dashboard',
});

// Session is automatically managed by Better-Auth
```

---

## Step 10: Migration Execution Checklist

### Pre-Migration
- [ ] Backup database
- [ ] Export all user data to CSV
- [ ] Test migration script on staging/dev database
- [ ] Document current user count
- [ ] Set maintenance window (if needed - pre-launch, so not critical)

### Migration Steps
- [ ] Step 1: Install Better-Auth dependencies
- [ ] Step 2: Generate Better-Auth schema/migrations
- [ ] Step 3: Create OASIS mapping tables (migration)
- [ ] Step 4: Run Better-Auth migrations
- [ ] Step 5: Run user migration script
- [ ] Step 6: Verify migrated user count matches original
- [ ] Step 7: Test authentication flow
- [ ] Step 8: Test OAuth (Google, GitHub)
- [ ] Step 9: Test wallet generation (OASIS integration)
- [ ] Step 10: Update frontend to use Better-Auth client

### Post-Migration
- [ ] Test all auth endpoints
- [ ] Test wallet endpoints
- [ ] Test admin endpoints
- [ ] Verify sessions work correctly
- [ ] Test email verification (if enabled)
- [ ] Test password reset
- [ ] Monitor error logs
- [ ] Remove old OASIS auth code (after verification period)

---

## Step 11: Update Environment Variables

**New Variables:**
```bash
# Better-Auth Configuration
BASE_URL=https://api.pangea.com
BETTER_AUTH_SECRET=your-secret-key-here  # Generate secure random string

# Google OAuth (for Better-Auth)
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret

# GitHub OAuth (optional)
GITHUB_CLIENT_ID=your-github-client-id
GITHUB_CLIENT_SECRET=your-github-client-secret

# OASIS API (still needed for wallets)
OASIS_API_URL=http://api.oasisweb4.com
OASIS_ADMIN_USERNAME=OASIS_ADMIN
OASIS_ADMIN_PASSWORD=Uppermall1!
```

**Generate Secret:**
```bash
# Generate secure random secret
openssl rand -base64 32
```

---

## Step 12: Cleanup Old Code (After Verification)

Once Better-Auth is verified working:

1. **Remove OASIS Auth Service:**
   - `src/auth/services/oasis-auth.service.ts` (keep for wallet API token management)
   - `src/auth/services/user-sync.service.ts` (no longer needed)

2. **Update Auth Service:**
   - Remove `register()` and `login()` methods (handled by Better-Auth)
   - Keep OASIS integration methods (for wallet linking)

3. **Remove Old Auth Controller:**
   - `src/auth/controllers/auth.controller.ts` (replace with Better-Auth controller)

4. **Update Guards:**
   - Replace `JwtAuthGuard` with `BetterAuthGuard`
   - Remove JWT strategy

5. **Clean up DTOs:**
   - Remove `LoginDto`, `RegisterDto` (Better-Auth handles these)

---

## Rollback Plan

If issues occur:

1. **Database Rollback:**
   ```sql
   -- Restore from backup
   DROP TABLE "user", session, account, verification;
   ALTER TABLE users_backup RENAME TO users;
   ```

2. **Code Rollback:**
   ```bash
   git revert <migration-commit>
   ```

3. **Restore Environment:**
   - Revert environment variable changes
   - Redeploy previous version

---

## Testing Plan

### Unit Tests
- [ ] Test OASIS link service (create avatar, get avatarId)
- [ ] Test wallet generation with Better-Auth user
- [ ] Test session management

### Integration Tests
- [ ] Test complete registration flow (Better-Auth → OASIS link)
- [ ] Test login flow (email/password, Google OAuth)
- [ ] Test wallet generation flow
- [ ] Test session persistence

### E2E Tests
- [ ] User registration → wallet creation → balance check
- [ ] OAuth flow (Google) → wallet creation
- [ ] Session expiration and refresh

---

## Timeline Estimate

- **Setup & Configuration:** 2-3 hours
- **Database Migration:** 1-2 hours
- **Code Integration:** 4-6 hours
- **Testing:** 3-4 hours
- **Frontend Updates:** 2-3 hours
- **Total:** ~12-18 hours

**Recommended:** 2-3 days with buffer for testing and fixes.

---

## Success Criteria

✅ **Authentication:**
- Users can register with email/password
- Users can login with email/password
- Users can login with Google OAuth
- Sessions persist correctly
- Password reset works

✅ **OASIS Integration:**
- Wallet generation works (via OASIS API)
- Wallet balance retrieval works
- Avatar linking is automatic/lazy

✅ **User Experience:**
- No disruption to existing functionality
- Modern auth UX (social login, etc.)
- Better error messages

---

**Last Updated:** December 23, 2025  
**Status:** Ready for Implementation
