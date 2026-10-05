# Simplifying User Account Architecture - Privy Pattern Approach

**Date:** December 23, 2025  
**Status:** Proposal - To Be Implemented  
**Goal:** Eliminate confusion from dual user accounts (OASIS + PostgreSQL) by adopting Privy.io's pattern

---

## Problem Statement

Currently, we have **two user accounts** which is confusing:

1. **OASIS (MongoDB)** - Stores avatar data, passwords, wallets (source of truth)
2. **PostgreSQL** - Stores duplicate user data + app-specific data

**Issues:**
- Two IDs per user: OASIS `avatarId` + PostgreSQL `id` (UUID)
- Duplicated fields: `email`, `username`, `firstName`, `lastName` stored in both places
- Unclear which is the "real" user account
- Confusion about which system to query for user data

---

## How Privy.io Solves This

Privy uses a **reference pattern** where external service ID is the primary key:

```
Privy Service (External)
├── User Account (privy_user_id: "user_abc123")
├── Authentication (email, password, social logins)
├── Wallets (managed by Privy)
└── Profile data (name, email, etc.)

Your Database (PostgreSQL)
├── users table:
│   ├── privy_user_id (PRIMARY KEY) - "user_abc123"
│   ├── role (app-specific)
│   ├── kyc_status (app-specific)
│   ├── last_login (cached)
│   └── [NO email, NO name, NO password - fetch from Privy]
│
└── orders table:
    └── user_id → references privy_user_id
```

**Privy's Key Principles:**
1. ✅ External service ID is the **primary key** (no separate UUID)
2. ✅ Only store **app-specific data** (role, KYC, preferences)
3. ✅ **Don't duplicate** profile data (fetch from Privy API when needed)
4. ✅ External service ID is used as **foreign key** in related tables

---

## Proposed Solution: Apply Privy Pattern to OASIS

### Current Architecture (Confusing)

```
PostgreSQL users table:
├── id (UUID) ← Generated locally
├── avatar_id ← Reference to OASIS (nullable, unique)
├── email ← Duplicated from OASIS
├── username ← Duplicated from OASIS
├── firstName ← Duplicated from OASIS
├── lastName ← Duplicated from OASIS
├── password_hash ← NULL (not used)
├── wallet_address_solana ← Duplicated from OASIS
├── wallet_address_ethereum ← Duplicated from OASIS
└── role, kyc_status (app-specific)
```

### Proposed Architecture (Clean)

```
PostgreSQL users table:
├── avatar_id (PRIMARY KEY) ← From OASIS (UUID string)
├── role (app-specific: 'user', 'admin', 'moderator')
├── kyc_status (app-specific: 'pending', 'approved', 'rejected')
├── is_active (app-specific)
├── last_login (cached for performance)
├── created_at
└── updated_at

[Removed: email, username, firstName, lastName, password_hash, wallet_address_*]
```

**Key Changes:**
- `avatar_id` becomes the **primary key** (no more separate UUID)
- **Remove all duplicate fields** - fetch from OASIS when needed
- Store **only app-specific data** in PostgreSQL
- Use `avatar_id` as foreign key in all related tables

---

## Implementation Plan

### Step 1: Database Migration

#### 1.1 Update Foreign Keys in Related Tables

All tables that reference `users.id` need to reference `users.avatar_id` instead:

**Tables to update:**
- `orders` - `user_id` → `avatar_id`
- `trades` - `buyer_id` → `buyer_avatar_id`, `seller_id` → `seller_avatar_id`
- `user_balances` - `user_id` → `avatar_id`
- `transactions` - `user_id` → `avatar_id`
- `tokenized_assets` - `issuer_id` → `issuer_avatar_id` (if applicable)

**Migration SQL:**
```sql
-- Step 1: Add new columns with avatar_id
ALTER TABLE orders ADD COLUMN avatar_id VARCHAR(255);
ALTER TABLE trades ADD COLUMN buyer_avatar_id VARCHAR(255);
ALTER TABLE trades ADD COLUMN seller_avatar_id VARCHAR(255);
ALTER TABLE user_balances ADD COLUMN avatar_id VARCHAR(255);
ALTER TABLE transactions ADD COLUMN avatar_id VARCHAR(255);

-- Step 2: Populate new columns from existing user_id via users table
UPDATE orders o 
SET avatar_id = u.avatar_id 
FROM users u 
WHERE o.user_id = u.id;

UPDATE trades t 
SET buyer_avatar_id = u.avatar_id 
FROM users u 
WHERE t.buyer_id = u.id;

UPDATE trades t 
SET seller_avatar_id = u.avatar_id 
FROM users u 
WHERE t.seller_id = u.id;

UPDATE user_balances ub 
SET avatar_id = u.avatar_id 
FROM users u 
WHERE ub.user_id = u.id;

UPDATE transactions tx 
SET avatar_id = u.avatar_id 
FROM users u 
WHERE tx.user_id = u.id;

-- Step 3: Drop old foreign key constraints
ALTER TABLE orders DROP CONSTRAINT IF EXISTS orders_user_id_fkey;
ALTER TABLE trades DROP CONSTRAINT IF EXISTS trades_buyer_id_fkey;
ALTER TABLE trades DROP CONSTRAINT IF EXISTS trades_seller_id_fkey;
ALTER TABLE user_balances DROP CONSTRAINT IF EXISTS user_balances_user_id_fkey;
ALTER TABLE transactions DROP CONSTRAINT IF EXISTS transactions_user_id_fkey;

-- Step 4: Drop old columns
ALTER TABLE orders DROP COLUMN user_id;
ALTER TABLE trades DROP COLUMN buyer_id;
ALTER TABLE trades DROP COLUMN seller_id;
ALTER TABLE user_balances DROP COLUMN user_id;
ALTER TABLE transactions DROP COLUMN user_id;

-- Step 5: Rename new columns to standard names (if needed)
-- orders.avatar_id stays as avatar_id
-- trades.buyer_avatar_id and seller_avatar_id stay as-is
-- etc.
```

#### 1.2 Update Users Table

```sql
-- Step 1: Ensure all users have avatar_id (should already be true)
-- Check for any NULL avatar_ids:
SELECT id, email FROM users WHERE avatar_id IS NULL;
-- If any exist, they need to be handled separately

-- Step 2: Make avatar_id NOT NULL and PRIMARY KEY
ALTER TABLE users ALTER COLUMN avatar_id SET NOT NULL;
ALTER TABLE users DROP CONSTRAINT IF EXISTS users_pkey;
ALTER TABLE users ADD PRIMARY KEY (avatar_id);

-- Step 3: Remove duplicate columns
ALTER TABLE users DROP COLUMN IF EXISTS id;
ALTER TABLE users DROP COLUMN IF EXISTS email;
ALTER TABLE users DROP COLUMN IF EXISTS username;
ALTER TABLE users DROP COLUMN IF EXISTS "firstName";
ALTER TABLE users DROP COLUMN IF EXISTS "lastName";
ALTER TABLE users DROP COLUMN IF EXISTS password_hash;
ALTER TABLE users DROP COLUMN IF EXISTS wallet_address_solana;
ALTER TABLE users DROP COLUMN IF EXISTS wallet_address_ethereum;

-- Step 4: Add indexes
CREATE INDEX IF NOT EXISTS idx_users_role ON users(role);
CREATE INDEX IF NOT EXISTS idx_users_kyc_status ON users(kyc_status);
CREATE INDEX IF NOT EXISTS idx_users_last_login ON users(last_login);
```

#### 1.3 Update Foreign Key Constraints

```sql
-- Add foreign key constraints on new columns
ALTER TABLE orders 
ADD CONSTRAINT orders_avatar_id_fkey 
FOREIGN KEY (avatar_id) REFERENCES users(avatar_id);

ALTER TABLE trades 
ADD CONSTRAINT trades_buyer_avatar_id_fkey 
FOREIGN KEY (buyer_avatar_id) REFERENCES users(avatar_id);

ALTER TABLE trades 
ADD CONSTRAINT trades_seller_avatar_id_fkey 
FOREIGN KEY (seller_avatar_id) REFERENCES users(avatar_id);

ALTER TABLE user_balances 
ADD CONSTRAINT user_balances_avatar_id_fkey 
FOREIGN KEY (avatar_id) REFERENCES users(avatar_id);

ALTER TABLE transactions 
ADD CONSTRAINT transactions_avatar_id_fkey 
FOREIGN KEY (avatar_id) REFERENCES users(avatar_id);
```

---

### Step 2: Update TypeORM Entities

#### 2.1 User Entity

**File:** `src/users/entities/user.entity.ts`

```typescript
import {
  Entity,
  Column,
  PrimaryColumn, // Changed from PrimaryGeneratedColumn
  CreateDateColumn,
  UpdateDateColumn,
  Index,
} from 'typeorm';

@Entity('users')
export class User {
  @PrimaryColumn({ name: 'avatar_id', type: 'varchar', length: 255 })
  avatarId: string; // This is now the primary key (from OASIS)

  // App-specific fields only
  @Column({ default: 'user' })
  @Index('idx_users_role')
  role: string;

  @Column({ name: 'kyc_status', default: 'pending' })
  @Index('idx_users_kyc_status')
  kycStatus: string;

  @Column({ name: 'is_active', default: true })
  isActive: boolean;

  @Column({ name: 'last_login', nullable: true })
  @Index('idx_users_last_login')
  lastLogin: Date;

  @CreateDateColumn({ name: 'created_at' })
  createdAt: Date;

  @UpdateDateColumn({ name: 'updated_at' })
  updatedAt: Date;
}
```

#### 2.2 Order Entity

**File:** `src/orders/entities/order.entity.ts`

```typescript
// Update the user relationship
@ManyToOne(() => User)
@JoinColumn({ name: 'avatar_id' }) // Changed from user_id
@Index('idx_orders_avatar_id')
user: User;

@Column({ name: 'avatar_id', type: 'varchar', length: 255 }) // Changed type from uuid to varchar
avatarId: string; // Changed from userId: string
```

#### 2.3 Trade Entity

**File:** `src/trades/entities/trade.entity.ts`

```typescript
// Update buyer relationship
@ManyToOne(() => User)
@JoinColumn({ name: 'buyer_avatar_id' })
@Index('idx_trades_buyer_avatar_id')
buyer: User;

@Column({ name: 'buyer_avatar_id', type: 'varchar', length: 255 })
buyerAvatarId: string; // Changed from buyerId

// Update seller relationship
@ManyToOne(() => User)
@JoinColumn({ name: 'seller_avatar_id' })
@Index('idx_trades_seller_avatar_id')
seller: User;

@Column({ name: 'seller_avatar_id', type: 'varchar', length: 255 })
sellerAvatarId: string; // Changed from sellerId
```

#### 2.4 User Balance Entity

**File:** `src/users/entities/user-balance.entity.ts`

```typescript
@ManyToOne(() => User)
@JoinColumn({ name: 'avatar_id' }) // Changed from user_id
@Index('idx_balances_avatar_id')
user: User;

@Column({ name: 'avatar_id', type: 'varchar', length: 255 }) // Changed type
avatarId: string; // Changed from userId
```

#### 2.5 Transaction Entity

**File:** `src/transactions/entities/transaction.entity.ts`

```typescript
@ManyToOne(() => User)
@JoinColumn({ name: 'avatar_id' }) // Changed from user_id
@Index('idx_transactions_avatar_id')
user: User;

@Column({ name: 'avatar_id', type: 'varchar', length: 255 }) // Changed type
avatarId: string; // Changed from userId
```

---

### Step 3: Update Services

#### 3.1 User Sync Service

**File:** `src/auth/services/user-sync.service.ts`

```typescript
async syncOasisUserToLocal(oasisAvatar: OASISAvatar): Promise<User> {
  // Find by avatarId (which is now the primary key)
  let user = await this.userRepository.findOne({
    where: { avatarId: oasisAvatar.avatarId },
  });

  if (!user) {
    // Create new user with avatarId as primary key
    user = this.userRepository.create({
      avatarId: oasisAvatar.avatarId, // Primary key - no need for separate id
      role: 'user',
      kycStatus: 'pending',
      isActive: true,
      lastLogin: new Date(),
    });
  } else {
    // Update last login and app-specific fields only
    user.lastLogin = new Date();
  }

  return this.userRepository.save(user);
}

async getUserByAvatarId(avatarId: string): Promise<User | null> {
  return this.userRepository.findOne({
    where: { avatarId },
  });
}

// Remove getUserByEmail - fetch from OASIS instead
```

#### 3.2 Auth Service

**File:** `src/auth/services/auth.service.ts`

```typescript
async login(loginDto: LoginDto): Promise<AuthResponseDto> {
  // 1. Authenticate with OASIS
  const oasisAvatar = await this.oasisAuthService.login(
    loginDto.email,
    loginDto.password,
  );

  // 2. Sync to local database (creates/updates app-specific data)
  const user = await this.userSyncService.syncOasisUserToLocal(oasisAvatar);

  // 3. Update last login
  await this.userSyncService.updateLastLogin(user.avatarId); // Changed from user.id

  // 4. Generate Pangea JWT token (using avatarId as subject)
  const token = this.generateJwtToken(user, oasisAvatar); // Pass both user and oasisAvatar

  return {
    user: {
      id: user.avatarId, // Use avatarId as id
      email: oasisAvatar.email, // From OASIS
      username: oasisAvatar.username, // From OASIS
      firstName: oasisAvatar.firstName, // From OASIS
      lastName: oasisAvatar.lastName, // From OASIS
      avatarId: user.avatarId,
      role: user.role,
    },
    token,
    expiresAt: this.getTokenExpiration(),
  };
}

private generateJwtToken(user: User, oasisAvatar: OASISAvatar): string {
  const payload = {
    sub: user.avatarId, // Use avatarId as subject (was user.id)
    email: oasisAvatar.email, // Include in token for convenience
    role: user.role,
  };

  return this.jwtService.sign(payload, {
    expiresIn: this.configService.get<string>('JWT_EXPIRES_IN') || '7d',
  });
}

async getProfile(avatarId: string): Promise<UserProfileDto> {
  // Get app-specific data from PostgreSQL
  const user = await this.userSyncService.getUserByAvatarId(avatarId);
  if (!user) {
    throw new UnauthorizedException('User not found');
  }

  // Get profile data from OASIS
  const oasisAvatar = await this.oasisAuthService.getUserProfile(avatarId);

  // Combine both
  return {
    id: user.avatarId,
    email: oasisAvatar.email,
    username: oasisAvatar.username,
    firstName: oasisAvatar.firstName,
    lastName: oasisAvatar.lastName,
    avatarId: user.avatarId,
    role: user.role,
    kycStatus: user.kycStatus,
    isActive: user.isActive,
    lastLogin: user.lastLogin,
  };
}
```

#### 3.3 Create User Profile Service (Optional - For Caching)

**File:** `src/auth/services/user-profile.service.ts`

```typescript
@Injectable()
export class UserProfileService {
  constructor(
    @InjectRepository(User)
    private userRepository: Repository<User>,
    private oasisAuthService: OasisAuthService,
  ) {}

  /**
   * Get full user profile (combines PostgreSQL + OASIS data)
   */
  async getFullProfile(avatarId: string): Promise<UserProfileDto> {
    // Get app-specific data from PostgreSQL
    const user = await this.userRepository.findOne({ 
      where: { avatarId } 
    });
    
    if (!user) {
      throw new NotFoundException('User not found');
    }
    
    // Get profile data from OASIS
    const oasisAvatar = await this.oasisAuthService.getUserProfile(avatarId);
    
    // Combine both
    return {
      id: user.avatarId,
      email: oasisAvatar.email,
      username: oasisAvatar.username,
      firstName: oasisAvatar.firstName,
      lastName: oasisAvatar.lastName,
      avatarId: user.avatarId,
      role: user.role,
      kycStatus: user.kycStatus,
      isActive: user.isActive,
      lastLogin: user.lastLogin,
      createdAt: user.createdAt,
      updatedAt: user.updatedAt,
    };
  }
}
```

---

### Step 4: Update JWT Strategy

**File:** `src/auth/strategies/jwt.strategy.ts`

```typescript
@Injectable()
export class JwtStrategy extends PassportStrategy(Strategy) {
  constructor(
    private configService: ConfigService,
    private userSyncService: UserSyncService,
  ) {
    super({
      jwtFromRequest: ExtractJwt.fromAuthHeaderAsBearerToken(),
      ignoreExpiration: false,
      secretOrKey: configService.get<string>('JWT_SECRET'),
    });
  }

  async validate(payload: any) {
    // payload.sub is now avatarId (not a separate UUID)
    const avatarId = payload.sub;
    
    const user = await this.userSyncService.getUserByAvatarId(avatarId);
    if (!user) {
      throw new UnauthorizedException('User not found');
    }

    return {
      avatarId: user.avatarId, // Use avatarId consistently
      email: payload.email, // From token payload (from OASIS)
      role: user.role,
    };
  }
}
```

---

### Step 5: Update All Controllers/Services That Use `user.id`

Search and replace `user.id` with `user.avatarId` in:
- Order controllers/services
- Trade controllers/services
- Transaction controllers/services
- Admin controllers/services
- Any other services that reference `user.id`

**Example:**
```typescript
// Before
const orders = await this.ordersService.findByUser(req.user.id);

// After
const orders = await this.ordersService.findByUser(req.user.avatarId);
```

---

## Migration Checklist

### Pre-Migration
- [ ] Backup database
- [ ] Test migration on staging/dev environment first
- [ ] Verify all users have `avatar_id` populated (no NULLs)
- [ ] Document current data counts (users, orders, trades, etc.)

### Migration Steps
- [ ] Run Step 1.1: Update foreign keys in related tables
- [ ] Run Step 1.2: Update users table (make avatar_id primary key, drop columns)
- [ ] Run Step 1.3: Add new foreign key constraints
- [ ] Update TypeORM entities (Step 2)
- [ ] Update services (Step 3)
- [ ] Update JWT strategy (Step 4)
- [ ] Update all controllers/services (Step 5)

### Post-Migration
- [ ] Test user registration flow
- [ ] Test user login flow
- [ ] Test order creation (should reference avatar_id)
- [ ] Test trade creation (should reference buyer_avatar_id, seller_avatar_id)
- [ ] Test admin endpoints
- [ ] Verify JWT tokens work correctly
- [ ] Check that profile endpoints return correct data

---

## Benefits

✅ **Single ID System** - `avatarId` is the only user identifier (no confusion)  
✅ **No Data Duplication** - Profile data (email, name) stays in OASIS only  
✅ **Clear Separation** - OASIS = source of truth, PostgreSQL = app-specific data  
✅ **Matches Privy Pattern** - Familiar architecture pattern  
✅ **Simpler Mental Model** - One ID to rule them all  
✅ **Less Storage** - Smaller user table in PostgreSQL  
✅ **Flexibility** - Can still cache frequently accessed data if needed (Redis/JWT)  

---

## Trade-offs & Considerations

### ⚠️ More OASIS API Calls
**Impact:** Need to fetch profile data (email, name) from OASIS when displaying user info

**Mitigations:**
1. **Include in JWT token** - Store email/username in JWT payload (already done)
2. **Redis caching** - Cache OASIS profile data with short TTL (5-15 minutes)
3. **Batch fetching** - When loading multiple users (e.g., trade history), batch OASIS API calls

### ⚠️ Migration Complexity
**Impact:** Requires careful migration of existing data

**Mitigations:**
1. Test thoroughly on staging first
2. Run migration during low-traffic period
3. Have rollback plan ready
4. Verify data integrity after migration

### ⚠️ Foreign Key Type Change
**Impact:** Changing from UUID to VARCHAR(255) for foreign keys

**Considerations:**
- UUIDs are stored as strings anyway in OASIS
- VARCHAR(255) is fine for UUID strings
- Indexes will still work efficiently
- No performance impact expected

---

## Alternative: Hybrid Approach (If Migration is Too Risky)

If full migration is too risky right now, we could take a **gradual approach**:

1. **Phase 1:** Keep both IDs, but prefer `avatarId` in new code
2. **Phase 2:** Add database views/helpers to use `avatarId` as primary identifier
3. **Phase 3:** Migrate fully when ready

However, **recommended approach is to do it all at once** - cleaner, less technical debt.

---

## Questions to Address

1. **What about existing users without `avatar_id`?**
   - Check if any exist
   - How to handle? (Likely need to create OASIS avatar for them)

2. **JWT token migration:**
   - Existing tokens will be invalid after migration (they reference `user.id`)
   - Users will need to re-login
   - Is this acceptable? (Probably yes - just need to communicate)

3. **Admin account migration:**
   - Admin accounts will use `avatarId` instead of `id`
   - Admin scripts need updating

---

## Next Steps

1. **Review this document** with team
2. **Test migration script** on dev/staging database
3. **Schedule migration window** (low-traffic period)
4. **Execute migration** following checklist
5. **Monitor** for issues post-migration
6. **Update frontend** to use `avatarId` consistently (if needed)

---

**Last Updated:** December 23, 2025  
**Status:** Ready for Review and Implementation

