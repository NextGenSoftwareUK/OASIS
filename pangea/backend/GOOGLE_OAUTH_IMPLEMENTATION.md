# Google OAuth Implementation Guide

**Date:** December 23, 2025  
**Status:** Implementation Plan  
**Goal:** Add Google OAuth authentication to Pangea Markets while maintaining OASIS integration

---

## Overview

Currently, Pangea uses **OASIS Avatar API** for authentication (email/password). This guide outlines how to add **Google OAuth** as an additional authentication method while maintaining compatibility with OASIS.

---

## Current Authentication Flow

```
User → Email/Password → OASIS Avatar API → avatarId → PostgreSQL → Pangea JWT
```

---

## Proposed Flow with Google OAuth

```
User → Google OAuth → Google Profile → Link/Create OASIS Avatar → avatarId → PostgreSQL → Pangea JWT
```

**Key Points:**
- Google OAuth happens at **Pangea backend** level
- After Google auth, we link to **OASIS avatar** (by email)
- If no OASIS avatar exists, we **create one** using Google account info
- OASIS avatar becomes the source of truth (as with email/password auth)

---

## Architecture Options

### Option 1: OAuth-First Approach (Recommended)

**Flow:**
1. User clicks "Sign in with Google"
2. Google OAuth flow (handled by Pangea backend)
3. Get Google profile (email, name, picture)
4. **Check if OASIS avatar exists** (by email)
   - If **exists**: Link Google account to existing avatar, login
   - If **doesn't exist**: Create OASIS avatar using Google info, login
5. Sync to PostgreSQL
6. Generate Pangea JWT token

**Pros:**
- ✅ Seamless user experience
- ✅ Works with existing OASIS infrastructure
- ✅ Users can still login with email/password if they set one later
- ✅ Single avatarId per user (OASIS is source of truth)

**Cons:**
- ⚠️ Need to handle OASIS avatar creation without password
- ⚠️ May need to store Google OAuth token for future OASIS avatar updates

---

### Option 2: OAuth-Only Users (Simpler, but less integrated)

**Flow:**
1. User clicks "Sign in with Google"
2. Google OAuth flow
3. Get Google profile
4. **Create Pangea user directly** (bypass OASIS for OAuth users)
5. Store Google profile data in PostgreSQL
6. Generate Pangea JWT token

**Pros:**
- ✅ Simpler implementation
- ✅ No OASIS dependency for OAuth users

**Cons:**
- ❌ OAuth users don't get OASIS features (wallets, karma, etc.)
- ❌ Two user types (OASIS users vs OAuth users)
- ❌ More complexity in codebase

**Recommendation:** Use **Option 1** - maintain single user model with OASIS as source of truth.

---

## Implementation Plan (Option 1)

### Step 1: Install Dependencies

```bash
npm install @nestjs/passport passport passport-google-oauth20
npm install --save-dev @types/passport-google-oauth20
```

---

### Step 2: Configure Google OAuth Credentials

1. **Go to Google Cloud Console:** https://console.cloud.google.com/
2. **Create OAuth 2.0 Credentials:**
   - Create new project (or use existing)
   - Enable Google+ API
   - Create OAuth 2.0 Client ID
   - Set authorized redirect URIs:
     - Development: `http://localhost:3000/api/auth/google/callback`
     - Production: `https://your-domain.com/api/auth/google/callback`
3. **Get credentials:**
   - Client ID
   - Client Secret

---

### Step 3: Environment Variables

**File:** `.env`

```bash
# Google OAuth
GOOGLE_CLIENT_ID=your-google-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-google-client-secret
GOOGLE_CALLBACK_URL=http://localhost:3000/api/auth/google/callback
```

**Railway Environment Variables:**
- Add `GOOGLE_CLIENT_ID`
- Add `GOOGLE_CLIENT_SECRET`
- Add `GOOGLE_CALLBACK_URL` (production URL)

---

### Step 4: Create Google OAuth Strategy

**File:** `src/auth/strategies/google.strategy.ts`

```typescript
import { Injectable } from '@nestjs/common';
import { PassportStrategy } from '@nestjs/passport';
import { Strategy, VerifyCallback } from 'passport-google-oauth20';
import { ConfigService } from '@nestjs/config';

@Injectable()
export class GoogleStrategy extends PassportStrategy(Strategy, 'google') {
  constructor(private configService: ConfigService) {
    super({
      clientID: configService.get<string>('GOOGLE_CLIENT_ID'),
      clientSecret: configService.get<string>('GOOGLE_CLIENT_SECRET'),
      callbackURL: configService.get<string>('GOOGLE_CALLBACK_URL') || '/api/auth/google/callback',
      scope: ['email', 'profile'],
    });
  }

  async validate(
    accessToken: string,
    refreshToken: string,
    profile: any,
    done: VerifyCallback,
  ): Promise<any> {
    const { name, emails, photos } = profile;
    
    const user = {
      email: emails[0].value,
      firstName: name.givenName,
      lastName: name.familyName,
      picture: photos[0].value,
      accessToken,
      refreshToken,
    };

    done(null, user);
  }
}
```

---

### Step 5: Update Auth Module

**File:** `src/auth/auth.module.ts`

```typescript
import { Module } from '@nestjs/common';
import { JwtModule } from '@nestjs/jwt';
import { PassportModule } from '@nestjs/passport';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { TypeOrmModule } from '@nestjs/typeorm';
import { User } from '../users/entities/user.entity';
import { AuthService } from './services/auth.service';
import { OasisAuthService } from './services/oasis-auth.service';
import { UserSyncService } from './services/user-sync.service';
import { JwtStrategy } from './strategies/jwt.strategy';
import { GoogleStrategy } from './strategies/google.strategy'; // Add this
import { AuthController } from './controllers/auth.controller';
import { UserController } from './controllers/user.controller';

@Module({
  imports: [
    TypeOrmModule.forFeature([User]),
    PassportModule.register({ defaultStrategy: 'jwt' }),
    JwtModule.registerAsync({
      imports: [ConfigModule],
      inject: [ConfigService],
      useFactory: (configService: ConfigService) => ({
        secret:
          configService.get<string>('JWT_SECRET') ||
          'YourSuperSecretKeyForJWTTokenGenerationThatShouldBeAtLeast32Characters',
        signOptions: { expiresIn: '7d' },
      }),
    }),
  ],
  controllers: [AuthController, UserController],
  providers: [
    AuthService, 
    OasisAuthService, 
    UserSyncService, 
    JwtStrategy,
    GoogleStrategy, // Add this
  ],
  exports: [AuthService, JwtStrategy, PassportModule],
})
export class AuthModule {}
```

---

### Step 6: Update Auth Service

**File:** `src/auth/services/auth.service.ts`

Add new method for Google OAuth:

```typescript
/**
 * Authenticate or register user via Google OAuth
 */
async googleAuth(googleUser: {
  email: string;
  firstName?: string;
  lastName?: string;
  picture?: string;
  accessToken: string;
}): Promise<AuthResponseDto> {
  try {
    this.logger.log(`Google OAuth authentication for: ${googleUser.email}`);

    // Check if OASIS avatar exists (by email)
    let oasisAvatar: OASISAvatar;
    
    try {
      // Try to get existing avatar by email (if OASIS supports this)
      // If not supported, we'll need to create a new avatar
      oasisAvatar = await this.oasisAuthService.getUserProfileByEmail(googleUser.email);
    } catch (error) {
      // Avatar doesn't exist, create new one
      this.logger.log(`No existing OASIS avatar found, creating new one for: ${googleUser.email}`);
      
      // Generate a random password for OASIS (user won't use it, but OASIS requires it)
      const randomPassword = this.generateRandomPassword();
      
      // Create OASIS avatar with Google account info
      oasisAvatar = await this.oasisAuthService.register({
        email: googleUser.email,
        password: randomPassword, // Random password (user will use Google to login)
        username: googleUser.email.split('@')[0], // Use email prefix as username
        firstName: googleUser.firstName || '',
        lastName: googleUser.lastName || '',
      });
      
      // Store Google OAuth token in user metadata (optional, for future use)
      // You might want to add a `googleAccessToken` field to User entity or store in metadata
    }

    // Sync to local database
    const user = await this.userSyncService.syncOasisUserToLocal(oasisAvatar);

    // Update last login
    await this.userSyncService.updateLastLogin(user.id);

    // Generate Pangea JWT token
    const token = this.generateJwtToken(user);

    return {
      user: {
        id: user.id,
        email: oasisAvatar.email,
        username: oasisAvatar.username,
        firstName: oasisAvatar.firstName || googleUser.firstName,
        lastName: oasisAvatar.lastName || googleUser.lastName,
        avatarId: user.avatarId || '',
        role: user.role,
        picture: googleUser.picture, // Include Google profile picture
      },
      token,
      expiresAt: this.getTokenExpiration(),
    };
  } catch (error: any) {
    this.logger.error(`Google OAuth authentication failed: ${error.message}`);
    throw new HttpException(
      'Google authentication failed',
      HttpStatus.INTERNAL_SERVER_ERROR,
    );
  }
}

/**
 * Generate random password for OASIS avatar creation (for OAuth users)
 */
private generateRandomPassword(): string {
  return (
    Math.random().toString(36).slice(-12) +
    Math.random().toString(36).slice(-12) +
    'A1!' // Add complexity requirement
  );
}
```

**Note:** The `getUserProfileByEmail` method might not exist in `OasisAuthService`. We'll need to check OASIS API or implement a workaround (see Step 7).

---

### Step 7: Handle OASIS Avatar Lookup by Email

OASIS API might not support "get avatar by email" directly. We have two options:

#### Option A: Try to authenticate with email (if password is unknown, it will fail)

```typescript
async findOrCreateAvatarFromGoogle(googleUser: {
  email: string;
  firstName?: string;
  lastName?: string;
}): Promise<OASISAvatar> {
  // Since we can't query OASIS by email directly, we'll always create new
  // This means OAuth users will have separate avatars (which is fine)
  // OR we could store email->avatarId mapping in PostgreSQL
  
  // For now, always create new avatar
  const randomPassword = this.generateRandomPassword();
  
  return await this.oasisAuthService.register({
    email: googleUser.email,
    password: randomPassword,
    username: googleUser.email.split('@')[0],
    firstName: googleUser.firstName || '',
    lastName: googleUser.lastName || '',
  });
}
```

#### Option B: Store email mapping in PostgreSQL (Recommended)

Add a method to check PostgreSQL first:

```typescript
async findOrCreateAvatarFromGoogle(googleUser: {
  email: string;
  firstName?: string;
  lastName?: string;
}): Promise<OASISAvatar> {
  // Check if user exists in PostgreSQL (by email)
  const existingUser = await this.userSyncService.getUserByEmail(googleUser.email);
  
  if (existingUser && existingUser.avatarId) {
    // User exists, fetch avatar from OASIS
    return await this.oasisAuthService.getUserProfile(existingUser.avatarId);
  }
  
  // Create new OASIS avatar
  const randomPassword = this.generateRandomPassword();
  
  return await this.oasisAuthService.register({
    email: googleUser.email,
    password: randomPassword,
    username: googleUser.email.split('@')[0],
    firstName: googleUser.firstName || '',
    lastName: googleUser.lastName || '',
  });
}
```

---

### Step 8: Add Auth Controller Endpoints

**File:** `src/auth/controllers/auth.controller.ts`

```typescript
import { Controller, Get, Req, Res, UseGuards } from '@nestjs/common';
import { AuthGuard } from '@nestjs/passport';
import { Request, Response } from 'express';
import { AuthService } from '../services/auth.service';

@Controller('auth')
export class AuthController {
  constructor(private readonly authService: AuthService) {}

  // ... existing endpoints ...

  /**
   * Initiate Google OAuth flow
   * GET /api/auth/google
   */
  @Get('google')
  @UseGuards(AuthGuard('google'))
  async googleAuth(@Req() req: Request) {
    // Passport handles the redirect
  }

  /**
   * Google OAuth callback
   * GET /api/auth/google/callback
   */
  @Get('google/callback')
  @UseGuards(AuthGuard('google'))
  async googleAuthRedirect(@Req() req: Request, @Res() res: Response) {
    const googleUser = req.user as any; // User from GoogleStrategy.validate()
    
    try {
      const authResult = await this.authService.googleAuth(googleUser);
      
      // Redirect to frontend with token
      // Option 1: Redirect with token in URL (less secure)
      const frontendUrl = process.env.FRONTEND_URL || 'http://localhost:3001';
      res.redirect(`${frontendUrl}/auth/callback?token=${authResult.token}`);
      
      // Option 2: Set token in HTTP-only cookie (more secure)
      // res.cookie('token', authResult.token, { httpOnly: true, secure: true });
      // res.redirect(`${frontendUrl}/auth/callback`);
      
    } catch (error) {
      // Redirect to frontend with error
      const frontendUrl = process.env.FRONTEND_URL || 'http://localhost:3001';
      res.redirect(`${frontendUrl}/auth/error?message=${encodeURIComponent(error.message)}`);
    }
  }
}
```

---

### Step 9: Update User Entity (Optional - Store OAuth Info)

**File:** `src/users/entities/user.entity.ts`

Add optional fields to track OAuth:

```typescript
@Column({ name: 'auth_provider', nullable: true })
authProvider: string; // 'oauth-google', 'oauth-facebook', 'oasis', etc.

@Column({ name: 'auth_provider_id', nullable: true })
authProviderId: string; // Google user ID

@Column({ name: 'profile_picture_url', nullable: true })
profilePictureUrl: string; // Google profile picture
```

---

### Step 10: Frontend Integration

**Frontend changes needed:**

1. **Add "Sign in with Google" button**

```typescript
// React example
const handleGoogleLogin = () => {
  window.location.href = 'https://your-backend.com/api/auth/google';
};
```

2. **Handle OAuth callback**

```typescript
// On /auth/callback page
useEffect(() => {
  const urlParams = new URLSearchParams(window.location.search);
  const token = urlParams.get('token');
  
  if (token) {
    // Store token
    localStorage.setItem('token', token);
    // Redirect to dashboard
    router.push('/dashboard');
  }
}, []);
```

---

## Alternative: Check OASIS Avatar by Email First

Since OASIS might not support querying by email directly, we can use PostgreSQL as the lookup:

**Updated Flow:**
1. User authenticates with Google
2. Get Google profile (email, name)
3. **Check PostgreSQL** for user with this email
   - If exists → Get `avatarId` → Fetch from OASIS → Login
   - If doesn't exist → Create OASIS avatar → Sync to PostgreSQL → Login

This works because:
- Users who registered with email/password already have records in PostgreSQL
- OAuth users will create new records
- Email is unique, so we can match them

**Implementation:**

```typescript
async googleAuth(googleUser: {
  email: string;
  firstName?: string;
  lastName?: string;
  picture?: string;
}): Promise<AuthResponseDto> {
  // Check PostgreSQL first (by email)
  let existingUser = await this.userSyncService.getUserByEmail(googleUser.email);
  
  let oasisAvatar: OASISAvatar;
  
  if (existingUser && existingUser.avatarId) {
    // User exists, fetch from OASIS
    oasisAvatar = await this.oasisAuthService.getUserProfile(existingUser.avatarId);
    
    // Update last login
    await this.userSyncService.updateLastLogin(existingUser.id);
    
    // Generate token
    const token = this.generateJwtToken(existingUser);
    
    return {
      user: {
        id: existingUser.id,
        email: oasisAvatar.email,
        username: oasisAvatar.username,
        firstName: oasisAvatar.firstName || googleUser.firstName,
        lastName: oasisAvatar.lastName || googleUser.lastName,
        avatarId: existingUser.avatarId,
        role: existingUser.role,
        picture: googleUser.picture,
      },
      token,
      expiresAt: this.getTokenExpiration(),
    };
  } else {
    // New user, create OASIS avatar
    const randomPassword = this.generateRandomPassword();
    
    oasisAvatar = await this.oasisAuthService.register({
      email: googleUser.email,
      password: randomPassword,
      username: googleUser.email.split('@')[0],
      firstName: googleUser.firstName || '',
      lastName: googleUser.lastName || '',
    });
    
    // Sync to PostgreSQL
    const user = await this.userSyncService.syncOasisUserToLocal(oasisAvatar);
    await this.userSyncService.updateLastLogin(user.id);
    
    const token = this.generateJwtToken(user);
    
    return {
      user: {
        id: user.id,
        email: oasisAvatar.email,
        username: oasisAvatar.username,
        firstName: oasisAvatar.firstName,
        lastName: oasisAvatar.lastName,
        avatarId: user.avatarId || '',
        role: user.role,
        picture: googleUser.picture,
      },
      token,
      expiresAt: this.getTokenExpiration(),
    };
  }
}
```

---

## Security Considerations

1. **Random Password Generation:**
   - OAuth users get random passwords in OASIS (they'll never use them)
   - Consider marking these accounts as "OAuth-only" in metadata

2. **Token Storage:**
   - Don't store Google access tokens in database (security risk)
   - If needed, encrypt them
   - Or just don't store them (user can re-authenticate)

3. **Email Verification:**
   - Google OAuth emails are pre-verified
   - OASIS might still send verification email (can be ignored for OAuth users)

4. **Account Linking:**
   - If user registers with email/password first, then uses Google OAuth with same email
   - Our flow will link them (same email = same user)
   - This is desired behavior

---

## Testing

### Test Cases

1. **New OAuth User:**
   - Sign in with Google (new email)
   - Should create OASIS avatar
   - Should create PostgreSQL user
   - Should return JWT token

2. **Existing Email/Password User:**
   - Sign in with Google (same email as existing account)
   - Should link to existing OASIS avatar
   - Should login with existing PostgreSQL user
   - Should return JWT token

3. **Existing OAuth User:**
   - Sign in with Google (previously used OAuth)
   - Should login with existing account
   - Should return JWT token

---

## Implementation Checklist

- [ ] Install dependencies (`@nestjs/passport`, `passport-google-oauth20`)
- [ ] Create Google OAuth credentials in Google Cloud Console
- [ ] Add environment variables (`GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GOOGLE_CALLBACK_URL`)
- [ ] Create `GoogleStrategy` (`src/auth/strategies/google.strategy.ts`)
- [ ] Update `AuthModule` (add `GoogleStrategy`)
- [ ] Add `googleAuth()` method to `AuthService`
- [ ] Add Google OAuth endpoints to `AuthController`
- [ ] Update `User` entity (optional - add OAuth fields)
- [ ] Test OAuth flow
- [ ] Update frontend (add "Sign in with Google" button)
- [ ] Handle OAuth callback in frontend
- [ ] Test with existing users (email/password)
- [ ] Test with new OAuth users

---

## Next Steps

1. **Other OAuth Providers:**
   - Apple Sign-In (for iOS users)
   - Facebook (if needed)
   - GitHub (for developers)

2. **Account Management:**
   - Allow users to link multiple OAuth providers
   - Allow users to set password for OAuth accounts (for email/password fallback)

3. **OASIS Integration:**
   - Check if OASIS API supports OAuth in future
   - If yes, migrate to OASIS OAuth endpoints

---

## Questions to Address

1. **Password Handling:**
   - Should we allow OAuth users to set a password later? (for email/password fallback)
   - Or keep them OAuth-only?

2. **Profile Picture:**
   - Store Google profile picture URL in PostgreSQL?
   - Or fetch from OASIS avatar?

3. **Username:**
   - Use email prefix as username? (e.g., `john.doe@email.com` → `john.doe`)
   - Or let user choose later?

---

**Last Updated:** December 23, 2025  
**Status:** Ready for Implementation

