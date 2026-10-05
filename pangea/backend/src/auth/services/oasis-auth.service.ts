import { Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import axios, { AxiosInstance } from 'axios';

/**
 * OASIS Avatar API response structure
 */
interface OASISAvatarResponse {
  result?: {
    Result?: {
      jwtToken?: string;
      avatarId?: string;
      id?: string;
      username?: string;
      email?: string;
      firstName?: string;
      lastName?: string;
    };
    result?: {
      jwtToken?: string;
      avatarId?: string;
      id?: string;
      username?: string;
      email?: string;
      firstName?: string;
      lastName?: string;
    };
    jwtToken?: string;
    avatarId?: string;
    id?: string;
    username?: string;
    email?: string;
    firstName?: string;
    lastName?: string;
  };
  jwtToken?: string;
  avatarId?: string;
  id?: string;
  username?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  isError?: boolean;
  message?: string;
}

/**
 * OASIS Avatar object extracted from response
 */
export interface OASISAvatar {
  avatarId: string;
  id: string;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  jwtToken?: string; // OASIS JWT token (we won't use this, but store for reference)
}

/**
 * Service for interacting with OASIS Avatar API
 * Based on Shipex Pro pattern
 */
@Injectable()
export class OasisAuthService {
  private readonly logger = new Logger(OasisAuthService.name);
  private readonly axiosInstance: AxiosInstance;
  private readonly baseUrl: string;

  constructor(private configService: ConfigService) {
    this.baseUrl =
      this.configService.get<string>('OASIS_API_URL') ||
      'http://api.oasisweb4.com';

    this.axiosInstance = axios.create({
      baseURL: this.baseUrl,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json',
      },
      // Handle self-signed certificates in development
      httpsAgent: process.env.NODE_ENV === 'development' ? {
        rejectUnauthorized: false,
      } : undefined,
    });
  }

  /**
   * Register a new user with OASIS Avatar API
   */
  async register(data: {
    email: string;
    password: string;
    username: string;
    firstName?: string;
    lastName?: string;
  }): Promise<OASISAvatar> {
    try {
      this.logger.log(`Registering new avatar: ${data.username}`);

      const response = await this.axiosInstance.post<OASISAvatarResponse>(
        '/api/avatar/register',
        {
          username: data.username,
          email: data.email,
          password: data.password,
          confirmPassword: data.password, // OASIS API requires confirmPassword to match password
          firstName: data.firstName || '',
          lastName: data.lastName || '',
          avatarType: 'User',
          acceptTerms: true,
        },
      );

      this.logger.debug(`OASIS registration response: ${JSON.stringify(response.data, null, 2)}`);
      return this.extractAvatarFromResponse(response.data);
    } catch (error: any) {
      this.logger.error(`OASIS registration failed: ${error.message}`);
      this.logger.error(`Error stack: ${error.stack}`);
      if (error.response?.data) {
        this.logger.error(`OASIS API error response: ${JSON.stringify(error.response.data, null, 2)}`);
        throw new Error(
          error.response.data.message ||
            error.response.data.Message ||
            JSON.stringify(error.response.data) ||
            'Registration failed',
        );
      }
      throw new Error(`Failed to register with OASIS API: ${error.message}`);
    }
  }

  /**
   * Authenticate user with OASIS Avatar API
   */
  async login(email: string, password: string): Promise<OASISAvatar> {
    try {
      this.logger.log(`Authenticating avatar: ${email}`);

      const response = await this.axiosInstance.post<OASISAvatarResponse>(
        '/api/avatar/authenticate',
        {
          username: email, // OASIS accepts email as username
          password: password,
        },
      );

      return this.extractAvatarFromResponse(response.data);
    } catch (error: any) {
      this.logger.error(`OASIS authentication failed: ${error.message}`);
      if (error.response?.data) {
        throw new Error(
          error.response.data.message ||
            error.response.data.Message ||
            'Authentication failed',
        );
      }
      throw new Error('Invalid email or password');
    }
  }

  /**
   * Get user profile from OASIS Avatar API
   */
  async getUserProfile(avatarId: string): Promise<OASISAvatar> {
    try {
      this.logger.log(`Fetching avatar profile: ${avatarId}`);

      const response = await this.axiosInstance.get<OASISAvatarResponse>(
        `/api/avatar/${avatarId}`,
      );

      return this.extractAvatarFromResponse(response.data);
    } catch (error: any) {
      this.logger.error(`Failed to fetch avatar profile: ${error.message}`);
      throw new Error('Failed to fetch user profile from OASIS');
    }
  }

  /**
   * Update user profile in OASIS Avatar API
   */
  async updateUserProfile(
    avatarId: string,
    data: {
      firstName?: string;
      lastName?: string;
      email?: string;
    },
  ): Promise<OASISAvatar> {
    try {
      this.logger.log(`Updating avatar profile: ${avatarId}`);

      const response = await this.axiosInstance.put<OASISAvatarResponse>(
        `/api/avatar/${avatarId}`,
        data,
      );

      return this.extractAvatarFromResponse(response.data);
    } catch (error: any) {
      this.logger.error(`Failed to update avatar profile: ${error.message}`);
      throw new Error('Failed to update user profile in OASIS');
    }
  }

  /**
   * Request password reset
   */
  async forgotPassword(email: string): Promise<void> {
    try {
      this.logger.log(`Requesting password reset for: ${email}`);

      await this.axiosInstance.post('/api/avatar/forgot-password', {
        email,
      });
    } catch (error: any) {
      this.logger.error(`Password reset request failed: ${error.message}`);
      // Don't throw error - don't reveal if email exists
    }
  }

  /**
   * Reset password
   */
  async resetPassword(
    token: string,
    newPassword: string,
  ): Promise<void> {
    try {
      this.logger.log('Resetting password');

      await this.axiosInstance.post('/api/avatar/reset-password', {
        token,
        newPassword,
      });
    } catch (error: any) {
      this.logger.error(`Password reset failed: ${error.message}`);
      throw new Error('Failed to reset password');
    }
  }

  /**
   * Refresh JWT token (if OASIS supports it)
   */
  async refreshToken(refreshToken: string): Promise<string> {
    try {
      const response = await this.axiosInstance.post<OASISAvatarResponse>(
        '/api/avatar/refresh-token',
        {
          refreshToken,
        },
      );

      const avatar = this.extractAvatarFromResponse(response.data);
      if (!avatar.jwtToken) {
        throw new Error('No token received from refresh');
      }
      return avatar.jwtToken;
    } catch (error: any) {
      this.logger.error(`Token refresh failed: ${error.message}`);
      throw new Error('Failed to refresh token');
    }
  }

  /**
   * Extract avatar object from OASIS API response
   * Handles nested response structures (Result.result, result.Result, etc.)
   */
  private extractAvatarFromResponse(data: OASISAvatarResponse): OASISAvatar {
    try {
      // Handle nested structures - registration response has result.result structure
      const result: any = data.result || data;
      const avatar =
        (result as any).Result || result.result || result || data;

      // Extract avatar ID (can be avatarId, id, or AvatarId)
      const avatarId =
        avatar?.avatarId ||
        avatar?.AvatarId ||
        avatar?.id ||
        avatar?.Id ||
        result?.avatarId ||
        result?.id;

      if (!avatarId) {
        this.logger.error('Invalid response structure - missing avatarId');
        this.logger.error('Full response:', JSON.stringify(data, null, 2));
        this.logger.error('Result object:', JSON.stringify(result, null, 2));
        this.logger.error('Avatar object:', JSON.stringify(avatar, null, 2));
        throw new Error('Invalid response structure from OASIS API: missing avatarId');
      }

      const extracted: OASISAvatar = {
        avatarId: avatarId.toString(),
        id: avatarId.toString(),
        username: avatar?.username || avatar?.Username || '',
        email: avatar?.email || avatar?.Email || '',
        firstName: avatar?.firstName || avatar?.FirstName,
        lastName: avatar?.lastName || avatar?.LastName,
        jwtToken: avatar?.jwtToken || avatar?.JwtToken,
      };

      // Validate required fields
      if (!extracted.email) {
        this.logger.error('Missing email in extracted avatar:', JSON.stringify(extracted, null, 2));
        throw new Error('Invalid response structure from OASIS API: missing email');
      }

      this.logger.debug(`Extracted avatar: ${JSON.stringify(extracted, null, 2)}`);
      return extracted;
    } catch (error: any) {
      this.logger.error(`Failed to extract avatar from response: ${error.message}`);
      throw error;
    }
  }
}












