import { Injectable, Logger } from '@nestjs/common';
import { Cron, CronExpression } from '@nestjs/schedule';
import { TransactionsService } from '../services/transactions.service';

/**
 * Background job to monitor blockchain for deposits
 * Runs every 5 minutes to check for new deposits
 */
@Injectable()
export class DepositMonitoringJob {
  private readonly logger = new Logger(DepositMonitoringJob.name);

  constructor(private transactionsService: TransactionsService) {}

  @Cron(CronExpression.EVERY_5_MINUTES)
  async handleCron() {
    this.logger.log('Running deposit monitoring job...');
    try {
      await this.transactionsService.monitorDeposits();
      this.logger.log('Deposit monitoring job completed');
    } catch (error) {
      this.logger.error(
        `Deposit monitoring job failed: ${error.message}`,
        error.stack,
      );
    }
  }
}











