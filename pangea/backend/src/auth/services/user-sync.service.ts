import { Injectable, Logger } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { User } from '../../users/entities/user.entity';
import { OASISAvatar } from './oasis-auth.service';

/**
 * Service for syncing OASIS Avatar data to local User database
 * Based on Shipex Pro pattern
 */
@Injectable()
export class UserSyncService {
  private readonly logger = new Logger(UserSyncService.name);

  constructor(
    @InjectRepository(User)
    private userRepository: Repository<User>,
  ) {}

  /**
   * Sync OASIS Avatar to local User database
   * Creates user if doesn't exist, updates if exists
   */
  async syncOasisUserToLocal(oasisAvatar: OASISAvatar): Promise<User> {
    try {
      // Validate required fields
      if (!oasisAvatar.avatarId) {
        throw new Error('OASIS avatar missing avatarId');
      }
      if (!oasisAvatar.email) {
        throw new Error('OASIS avatar missing email');
      }

      this.logger.log(`Syncing OASIS avatar to local DB: ${oasisAvatar.avatarId}, email: ${oasisAvatar.email}`);

      // Try to find existing user by avatarId
      let user = await this.userRepository.findOne({
        where: { avatarId: oasisAvatar.avatarId },
      });

      // If not found by avatarId, try by email
      if (!user) {
        user = await this.userRepository.findOne({
          where: { email: oasisAvatar.email },
        });
      }

      if (user) {
        // Update existing user
        this.logger.log(`Updating existing user: ${user.id}`);
        user.email = oasisAvatar.email;
        user.username = oasisAvatar.username || user.username;
        user.firstName = oasisAvatar.firstName || user.firstName;
        user.lastName = oasisAvatar.lastName || user.lastName;
        user.avatarId = oasisAvatar.avatarId;
        user.lastLogin = new Date();
      } else {
        // Create new user
        this.logger.log(`Creating new user for avatar: ${oasisAvatar.avatarId}`);
        const userData = {
          email: oasisAvatar.email,
          username: oasisAvatar.username || null,
          firstName: oasisAvatar.firstName || null,
          lastName: oasisAvatar.lastName || null,
          avatarId: oasisAvatar.avatarId,
          role: 'user',
          kycStatus: 'pending',
          isActive: true,
          lastLogin: new Date(),
        };
        this.logger.debug(`User data to create: ${JSON.stringify(userData, null, 2)}`);
        user = this.userRepository.create(userData);
      }

      const savedUser = await this.userRepository.save(user);
      this.logger.log(`User synced successfully: ${savedUser.id}`);
      return savedUser;
    } catch (error: any) {
      this.logger.error(`Failed to sync user: ${error.message}`);
      this.logger.error(`Error stack: ${error.stack}`);
      if (error.code) {
        this.logger.error(`Database error code: ${error.code}`);
      }
      if (error.detail) {
        this.logger.error(`Database error detail: ${error.detail}`);
      }
      throw new Error(`Failed to sync user to local database: ${error.message}`);
    }
  }

  /**
   * Get user by avatar ID
   */
  async getUserByAvatarId(avatarId: string): Promise<User | null> {
    return this.userRepository.findOne({
      where: { avatarId },
    });
  }

  /**
   * Get user by email
   */
  async getUserByEmail(email: string): Promise<User | null> {
    return this.userRepository.findOne({
      where: { email },
    });
  }

  /**
   * Get user by ID
   */
  async getUserById(userId: string): Promise<User | null> {
    return this.userRepository.findOne({
      where: { id: userId },
    });
  }

  /**
   * Update user's last login timestamp
   */
  async updateLastLogin(userId: string): Promise<void> {
    await this.userRepository.update(userId, {
      lastLogin: new Date(),
    });
  }
}












