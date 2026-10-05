import { Injectable, Logger, UnauthorizedException, HttpException, HttpStatus } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { ConfigService } from '@nestjs/config';
import { OasisAuthService } from './oasis-auth.service';
import { UserSyncService } from './user-sync.service';
import { RegisterDto, LoginDto, AuthResponseDto } from '../dto';
import { User } from '../../users/entities/user.entity';

/**
 * Main authentication service
 * Generates Pangea-specific JWT tokens (not OASIS tokens)
 * Based on Shipex Pro pattern
 */
@Injectable()
export class AuthService {
  private readonly logger = new Logger(AuthService.name);

  constructor(
    private oasisAuthService: OasisAuthService,
    private userSyncService: UserSyncService,
    private jwtService: JwtService,
    private configService: ConfigService,
  ) {}

  /**
   * Register a new user
   * 1. Register with OASIS Avatar API
   * 2. Sync to local database
   * 3. Generate Pangea JWT token
   */
  async register(registerDto: RegisterDto): Promise<AuthResponseDto> {
    try {
      this.logger.log(`Starting registration for email: ${registerDto.email}, username: ${registerDto.username}`);
      
      // 1. Register with OASIS
      const oasisAvatar = await this.oasisAuthService.register({
        email: registerDto.email,
        password: registerDto.password,
        username: registerDto.username,
        firstName: registerDto.firstName,
        lastName: registerDto.lastName,
      });

      this.logger.log(`OASIS registration successful, avatarId: ${oasisAvatar.avatarId}`);

      // 2. Sync to local database
      const user = await this.userSyncService.syncOasisUserToLocal(oasisAvatar);

      this.logger.log(`User synced to local DB, userId: ${user.id}`);

      // 3. Generate Pangea JWT token
      const token = this.generateJwtToken(user);

      return {
        user: {
          id: user.id,
          email: user.email,
          username: user.username || '',
          firstName: user.firstName || undefined,
          lastName: user.lastName || undefined,
          avatarId: user.avatarId || '',
          role: user.role,
        },
        token,
        expiresAt: this.getTokenExpiration(),
      };
    } catch (error: any) {
      this.logger.error(`Registration failed: ${error.message}`);
      this.logger.error(`Error stack: ${error.stack}`);
      // Re-throw with proper HTTP exception if it's not already one
      if (error instanceof HttpException || error.status) {
        throw error;
      }
      throw new HttpException(
        error.message || 'Registration failed',
        HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }

  /**
   * Login user
   * 1. Authenticate with OASIS Avatar API
   * 2. Sync to local database
   * 3. Generate Pangea JWT token
   */
  async login(loginDto: LoginDto): Promise<AuthResponseDto> {
    try {
      // 1. Authenticate with OASIS
      const oasisAvatar = await this.oasisAuthService.login(
        loginDto.email,
        loginDto.password,
      );

      // 2. Sync to local database
      const user = await this.userSyncService.syncOasisUserToLocal(oasisAvatar);

      // 3. Update last login
      await this.userSyncService.updateLastLogin(user.id);

      // 4. Generate Pangea JWT token
      const token = this.generateJwtToken(user);

      return {
        user: {
          id: user.id,
          email: user.email,
          username: user.username || '',
          firstName: user.firstName || undefined,
          lastName: user.lastName || undefined,
          avatarId: user.avatarId || '',
          role: user.role,
        },
        token,
        expiresAt: this.getTokenExpiration(),
      };
    } catch (error: any) {
      this.logger.error(`Login failed: ${error.message}`);
      throw new UnauthorizedException('Invalid email or password');
    }
  }

  /**
   * Get user profile
   */
  async getProfile(userId: string): Promise<User> {
    const user = await this.userSyncService.getUserById(userId);
    if (!user) {
      throw new UnauthorizedException('User not found');
    }
    return user;
  }

  /**
   * Update user profile
   */
  async updateProfile(
    userId: string,
    updateData: {
      firstName?: string;
      lastName?: string;
      email?: string;
    },
  ): Promise<User> {
    const user = await this.userSyncService.getUserById(userId);
    if (!user) {
      throw new UnauthorizedException('User not found');
    }

    if (!user.avatarId) {
      throw new Error('User avatar ID not found');
    }

    // Update in OASIS
    const updatedOasisAvatar = await this.oasisAuthService.updateUserProfile(
      user.avatarId,
      updateData,
    );

    // Sync updated data back to local database
    return await this.userSyncService.syncOasisUserToLocal(updatedOasisAvatar);
  }

  /**
   * Request password reset
   */
  async forgotPassword(email: string): Promise<void> {
    await this.oasisAuthService.forgotPassword(email);
    // Don't reveal if email exists or not
  }

  /**
   * Reset password
   */
  async resetPassword(token: string, newPassword: string): Promise<void> {
    await this.oasisAuthService.resetPassword(token, newPassword);
  }

  /**
   * Validate user from JWT payload
   */
  async validateUser(userId: string): Promise<User | null> {
    return this.userSyncService.getUserById(userId);
  }

  /**
   * Generate Pangea JWT token (not OASIS token)
   */
  private generateJwtToken(user: User): string {
    const payload = {
      sub: user.id,
      email: user.email,
      username: user.username,
      avatarId: user.avatarId,
      role: user.role,
    };

    return this.jwtService.sign(payload);
  }

  /**
   * Get token expiration date
   */
  private getTokenExpiration(): Date {
    const expiresIn = this.configService.get<string>('JWT_EXPIRES_IN') || '7d';
    // Parse expiresIn (e.g., "7d", "24h", "30m")
    const now = new Date();
    if (expiresIn.endsWith('d')) {
      const days = parseInt(expiresIn);
      now.setDate(now.getDate() + days);
    } else if (expiresIn.endsWith('h')) {
      const hours = parseInt(expiresIn);
      now.setHours(now.getHours() + hours);
    } else if (expiresIn.endsWith('m')) {
      const minutes = parseInt(expiresIn);
      now.setMinutes(now.getMinutes() + minutes);
    } else {
      // Default to 7 days
      now.setDate(now.getDate() + 7);
    }
    return now;
  }
}












