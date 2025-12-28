import {
  Injectable,
  Logger,
  BadRequestException,
  InternalServerErrorException,
} from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { Order } from '../entities/order.entity';
import { TradesService } from '../../trades/trades.service';
import { BalanceService } from './balance.service';
import { BlockchainService } from '../../blockchain/services/blockchain.service';
import { WebSocketService } from './websocket.service';
import { User } from '../../users/entities/user.entity';

@Injectable()
export class OrderMatchingService {
  private readonly logger = new Logger(OrderMatchingService.name);

  constructor(
    @InjectRepository(Order)
    private orderRepository: Repository<Order>,
    private tradesService: TradesService,
    private balanceService: BalanceService,
    private blockchainService: BlockchainService,
    private webSocketService: WebSocketService,
  ) {}

  /**
   * Process a new order and attempt to match it with existing orders
   */
  async processOrder(order: Order): Promise<void> {
    this.logger.log(
      `Processing order ${order.orderId} (${order.orderType}, ${order.remainingQuantity} remaining)`,
    );

    try {
      // 1. Find matching orders
      const matches = await this.findMatchingOrders(order);

      if (matches.length === 0) {
        // No matches, set order to open
        this.logger.log(
          `No matches found for order ${order.orderId}, setting status to open`,
        );
        order.orderStatus = 'open';
        await this.orderRepository.save(order);
        this.webSocketService.emitOrderUpdate(order);
        return;
      }

      this.logger.log(
        `Found ${matches.length} potential matches for order ${order.orderId}`,
      );

      // 2. Execute matches
      for (const match of matches) {
        if (order.remainingQuantity === BigInt(0)) {
          this.logger.log(
            `Order ${order.orderId} fully filled, stopping matching`,
          );
          break;
        }

        try {
          await this.executeMatch(order, match);
        } catch (error) {
          this.logger.error(
            `Failed to execute match between order ${order.orderId} and ${match.orderId}: ${error.message}`,
            error.stack,
          );
          // Continue with next match instead of failing entire order
          continue;
        }
      }

      // 3. Update order status
      if (order.remainingQuantity === BigInt(0)) {
        order.orderStatus = 'filled';
        order.filledAt = new Date();
        this.logger.log(`Order ${order.orderId} fully filled`);
      } else if (order.filledQuantity > BigInt(0)) {
        order.orderStatus = 'partially_filled';
        this.logger.log(`Order ${order.orderId} partially filled`);
      } else {
        order.orderStatus = 'open';
        this.logger.log(`Order ${order.orderId} set to open`);
      }

      await this.orderRepository.save(order);
      this.webSocketService.emitOrderUpdate(order);
    } catch (error) {
      this.logger.error(
        `Error processing order ${order.orderId}: ${error.message}`,
        error.stack,
      );
      throw new InternalServerErrorException(
        `Failed to process order: ${error.message}`,
      );
    }
  }

  /**
   * Find matching orders based on price-time priority
   */
  async findMatchingOrders(order: Order): Promise<Order[]> {
    const oppositeType = order.orderType === 'buy' ? 'sell' : 'buy';

    const queryBuilder = this.orderRepository
      .createQueryBuilder('order')
      .where('order.assetId = :assetId', { assetId: order.assetId })
      .andWhere('order.orderType = :type', { type: oppositeType })
      .andWhere('order.orderStatus IN (:...statuses)', {
        statuses: ['open', 'partially_filled'],
      })
      .andWhere('order.remainingQuantity > 0');

    // Price matching logic:
    // For buy orders, find sell orders at or below buy price (best price first = ASC)
    // For sell orders, find buy orders at or above sell price (best price first = DESC)
    if (order.orderType === 'buy') {
      queryBuilder
        .andWhere('order.pricePerTokenUsd <= :price', {
          price: order.pricePerTokenUsd,
        })
        .orderBy('order.pricePerTokenUsd', 'ASC'); // Best price (lowest) first
    } else {
      queryBuilder
        .andWhere('order.pricePerTokenUsd >= :price', {
          price: order.pricePerTokenUsd,
        })
        .orderBy('order.pricePerTokenUsd', 'DESC'); // Best price (highest) first
    }

    // Time priority: earlier orders at same price execute first
    queryBuilder.addOrderBy('order.createdAt', 'ASC');

    return queryBuilder.getMany();
  }

  /**
   * Execute a match between a buy order and a sell order
   */
  async executeMatch(buyOrder: Order, sellOrder: Order): Promise<void> {
    this.logger.log(
      `Executing match: Buy Order ${buyOrder.orderId} <-> Sell Order ${sellOrder.orderId}`,
    );

    // Ensure we have the correct order types
    if (buyOrder.orderType !== 'buy' || sellOrder.orderType !== 'sell') {
      // Swap if needed
      if (buyOrder.orderType === 'sell') {
        [buyOrder, sellOrder] = [sellOrder, buyOrder];
      } else {
        throw new BadRequestException(
          'Invalid order types for matching: both orders must be opposite types',
        );
      }
    }

    // 1. Calculate trade quantity
    const buyRemaining = Number(buyOrder.remainingQuantity);
    const sellRemaining = Number(sellOrder.remainingQuantity);
    const tradeQuantity = Math.min(buyRemaining, sellRemaining);

    if (tradeQuantity <= 0) {
      this.logger.warn(
        `Trade quantity is 0 or negative, skipping match between ${buyOrder.orderId} and ${sellOrder.orderId}`,
      );
      return;
    }

    // 2. Use sell order price (price-time priority: the resting order's price)
    const tradePrice = sellOrder.pricePerTokenUsd;
    const totalValue = tradePrice * tradeQuantity;

    this.logger.log(
      `Match details: quantity=${tradeQuantity}, price=${tradePrice}, totalValue=${totalValue}`,
    );

    // 3. Validate balances
    await this.validateBalances(buyOrder, sellOrder, tradeQuantity);

    // 4. Execute on blockchain
    let transactionHash: string;
    try {
      transactionHash = await this.blockchainService.executeTrade({
        buyer: buyOrder.user,
        seller: sellOrder.user,
        asset: {
          id: buyOrder.assetId,
          blockchain: buyOrder.blockchain,
        },
        quantity: tradeQuantity,
        price: tradePrice,
      });
      this.logger.log(
        `Blockchain execution successful, transaction hash: ${transactionHash}`,
      );
    } catch (error) {
      this.logger.error(
        `Blockchain execution failed: ${error.message}`,
        error.stack,
      );
      throw new InternalServerErrorException(
        `Failed to execute trade on blockchain: ${error.message}`,
      );
    }

    // 5. Create trade record
    let trade;
    try {
      trade = await this.tradesService.create({
        buyerId: buyOrder.user.id,
        sellerId: sellOrder.user.id,
        assetId: buyOrder.assetId,
        buyOrderId: buyOrder.id,
        sellOrderId: sellOrder.id,
        quantity: tradeQuantity,
        pricePerTokenUsd: tradePrice,
        totalValueUsd: totalValue,
        blockchain: buyOrder.blockchain,
        transactionHash,
      });
      this.logger.log(`Trade record created: ${trade.tradeId}`);
    } catch (error) {
      this.logger.error(
        `Failed to create trade record: ${error.message}`,
        error.stack,
      );
      // Note: In production, you might want to handle this differently
      // (e.g., retry logic, compensation, etc.)
      throw new InternalServerErrorException(
        `Failed to create trade record: ${error.message}`,
      );
    }

    // 6. Update orders
    const tradeQuantityBigInt = BigInt(tradeQuantity);
    buyOrder.filledQuantity += tradeQuantityBigInt;
    buyOrder.remainingQuantity -= tradeQuantityBigInt;

    sellOrder.filledQuantity += tradeQuantityBigInt;
    sellOrder.remainingQuantity -= tradeQuantityBigInt;

    // Update sell order status
    if (sellOrder.remainingQuantity === BigInt(0)) {
      sellOrder.orderStatus = 'filled';
      sellOrder.filledAt = new Date();
      this.logger.log(`Sell order ${sellOrder.orderId} fully filled`);
    } else {
      sellOrder.orderStatus = 'partially_filled';
      this.logger.log(
        `Sell order ${sellOrder.orderId} partially filled (${sellOrder.remainingQuantity} remaining)`,
      );
    }

    // Save both orders
    await this.orderRepository.save([buyOrder, sellOrder]);

    // 7. Update balances
    try {
      await this.balanceService.transfer(
        sellOrder.user.id,
        buyOrder.user.id,
        buyOrder.assetId,
        tradeQuantityBigInt,
      );
      this.logger.log(`Balance transfer completed`);
    } catch (error) {
      this.logger.error(
        `Failed to transfer balances: ${error.message}`,
        error.stack,
      );
      // Note: In production, you might want to implement rollback logic here
      throw new InternalServerErrorException(
        `Failed to transfer balances: ${error.message}`,
      );
    }

    // 8. Emit WebSocket events
    this.webSocketService.emitTradeExecution(trade);
    this.webSocketService.emitOrderUpdate(buyOrder);
    this.webSocketService.emitOrderUpdate(sellOrder);

    // Emit balance updates for both users
    try {
      const buyerBalance = await this.balanceService.getBalance(
        buyOrder.user.id,
        buyOrder.assetId,
      );
      const sellerBalance = await this.balanceService.getBalance(
        sellOrder.user.id,
        sellOrder.assetId,
      );

      this.webSocketService.emitBalanceUpdate(buyOrder.user.id, {
        assetId: buyOrder.assetId,
        balance: buyerBalance.balance.toString(),
        availableBalance: buyerBalance.availableBalance.toString(),
        lockedBalance: buyerBalance.lockedBalance.toString(),
      });

      this.webSocketService.emitBalanceUpdate(sellOrder.user.id, {
        assetId: sellOrder.assetId,
        balance: sellerBalance.balance.toString(),
        availableBalance: sellerBalance.availableBalance.toString(),
        lockedBalance: sellerBalance.lockedBalance.toString(),
      });
    } catch (error) {
      this.logger.warn(
        `Failed to emit balance updates: ${error.message}`,
      );
    }

    this.logger.log(
      `Match execution completed successfully: Trade ${trade.tradeId}`,
    );
  }

  /**
   * Validate balances before executing a trade
   */
  async validateBalances(
    buyOrder: Order,
    sellOrder: Order,
    quantity: number,
  ): Promise<void> {
    this.logger.log(
      `Validating balances for trade: quantity=${quantity}, buyOrder=${buyOrder.orderId}, sellOrder=${sellOrder.orderId}`,
    );

    // Validate seller has tokens
    const sellerBalance = await this.balanceService.getBalance(
      sellOrder.user.id,
      sellOrder.assetId,
    );

    if (sellerBalance.availableBalance < BigInt(quantity)) {
      throw new BadRequestException(
        `Seller has insufficient balance. Required: ${quantity}, Available: ${sellerBalance.availableBalance}`,
      );
    }

    // Validate buyer has funds (payment token: USDC, SOL, etc.)
    const totalCost = sellOrder.pricePerTokenUsd * quantity;
    const buyerPaymentBalance = await this.balanceService.getPaymentTokenBalance(
      buyOrder.user.id,
      buyOrder.blockchain,
    );

    // Convert totalCost to the appropriate unit (assuming 6 decimals for USDC, 9 for SOL)
    // This is a simplified check - actual implementation should handle decimals properly
    const requiredPaymentAmount = BigInt(Math.floor(totalCost * 1_000_000)); // Assuming 6 decimals

    if (buyerPaymentBalance < requiredPaymentAmount) {
      throw new BadRequestException(
        `Buyer has insufficient payment balance. Required: ${requiredPaymentAmount}, Available: ${buyerPaymentBalance}`,
      );
    }

    this.logger.log(`Balance validation passed`);
  }
}












