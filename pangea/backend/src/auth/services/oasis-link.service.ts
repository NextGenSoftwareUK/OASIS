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

