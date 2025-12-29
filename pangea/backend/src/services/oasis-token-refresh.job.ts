import { Injectable, Logger } from '@nestjs/common';
import { Cron, CronExpression } from '@nestjs/schedule';
import { OasisTokenManagerService } from './oasis-token-manager.service';

/**
 * Scheduled job to refresh OASIS API token before expiration
 * Runs every 10 minutes to check and refresh token if needed
 */
@Injectable()
export class OasisTokenRefreshJob {
  private readonly logger = new Logger(OasisTokenRefreshJob.name);

  constructor(private tokenManager: OasisTokenManagerService) {}

  /**
   * Check and refresh token every 10 minutes
   * Token expires in 15 minutes, so this ensures we refresh before expiry
   */
  @Cron(CronExpression.EVERY_10_MINUTES)
  async handleCron() {
    this.logger.log('Running OASIS token refresh check...');
    try {
      // This will check if token is expiring and refresh if needed
      await this.tokenManager.getToken();
      this.logger.debug('OASIS token refresh check completed');
    } catch (error) {
      this.logger.error(
        `OASIS token refresh job failed: ${error.message}`,
        error.stack,
      );
    }
  }
}




