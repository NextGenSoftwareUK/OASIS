import { Injectable, Logger } from '@nestjs/common';
import { Cron, CronExpression } from '@nestjs/schedule';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { Order } from '../entities/order.entity';
import { OrderMatchingService } from '../services/order-matching.service';

@Injectable()
export class OrderMatchingJob {
  private readonly logger = new Logger(OrderMatchingJob.name);

  constructor(
    @InjectRepository(Order)
    private orderRepository: Repository<Order>,
    private orderMatchingService: OrderMatchingService,
  ) {}

  /**
   * Process pending orders every 5 seconds
   * This ensures orders are matched quickly after creation
   */
  @Cron('*/5 * * * * *') // Every 5 seconds
  async matchPendingOrders() {
    try {
      this.logger.debug('Running order matching job');

      // Find pending orders
      const pendingOrders = await this.orderRepository.find({
        where: { orderStatus: 'pending' },
        relations: ['user'],
        take: 10, // Process up to 10 orders per run to avoid overload
      });

      if (pendingOrders.length === 0) {
        this.logger.debug('No pending orders to process');
        return;
      }

      this.logger.log(
        `Processing ${pendingOrders.length} pending order(s)`,
      );

      // Process each pending order
      for (const order of pendingOrders) {
        try {
          await this.orderMatchingService.processOrder(order);
        } catch (error) {
          this.logger.error(
            `Failed to process order ${order.orderId}: ${error.message}`,
            error.stack,
          );
          // Continue with next order instead of failing entire job
        }
      }

      this.logger.debug('Order matching job completed');
    } catch (error) {
      this.logger.error(
        `Error in order matching job: ${error.message}`,
        error.stack,
      );
    }
  }

  /**
   * Process open orders periodically to check for new matches
   * This handles cases where new orders might match existing open orders
   */
  @Cron(CronExpression.EVERY_30_SECONDS)
  async matchOpenOrders() {
    try {
      this.logger.debug('Running open order matching job');

      // Find open and partially filled orders
      const openOrders = await this.orderRepository.find({
        where: [
          { orderStatus: 'open' },
          { orderStatus: 'partially_filled' },
        ],
        relations: ['user'],
        take: 20, // Process up to 20 orders per run
      });

      if (openOrders.length === 0) {
        this.logger.debug('No open orders to process');
        return;
      }

      this.logger.log(`Checking ${openOrders.length} open order(s) for matches`);

      // Process each open order
      for (const order of openOrders) {
        try {
          // Only process if order has remaining quantity
          if (order.remainingQuantity > BigInt(0)) {
            await this.orderMatchingService.processOrder(order);
          }
        } catch (error) {
          this.logger.error(
            `Failed to process open order ${order.orderId}: ${error.message}`,
            error.stack,
          );
          // Continue with next order
        }
      }

      this.logger.debug('Open order matching job completed');
    } catch (error) {
      this.logger.error(
        `Error in open order matching job: ${error.message}`,
        error.stack,
      );
    }
  }
}












