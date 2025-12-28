import {
  Injectable,
  NotFoundException,
  BadRequestException,
  Logger,
} from '@nestjs/common';
import { InjectRepository, InjectDataSource } from '@nestjs/typeorm';
import { Repository, DataSource } from 'typeorm';
import { Order } from '../entities/order.entity';
import { TokenizedAsset } from '../../assets/entities/tokenized-asset.entity';
import { CreateOrderDto, UpdateOrderDto } from '../dto';
import { BalanceService } from './balance.service';
import { OrderMatchingService } from './order-matching.service';

@Injectable()
export class OrdersService {
  private readonly logger = new Logger(OrdersService.name);

  constructor(
    @InjectRepository(Order)
    private orderRepository: Repository<Order>,
    @InjectDataSource()
    private dataSource: DataSource,
    private balanceService: BalanceService,
    private orderMatchingService: OrderMatchingService,
  ) {}

  private get assetRepository(): Repository<TokenizedAsset> {
    return this.dataSource.getRepository(TokenizedAsset);
  }

  /**
   * Generate unique order ID
   * Format: ORD-{YEAR}-{SEQUENCE}
   * Example: ORD-2025-001
   */
  private generateOrderId(): string {
    const year = new Date().getFullYear();
    // In production, get the next sequence from database
    const sequence = Math.floor(Math.random() * 100000)
      .toString()
      .padStart(6, '0');
    return `ORD-${year}-${sequence}`;
  }

  /**
   * Create a new order
   */
  async create(dto: CreateOrderDto, userId: string): Promise<Order> {
    this.logger.log(
      `Creating ${dto.orderType} order for user ${userId}, asset ${dto.assetId}`,
    );

    // Validate order
    await this.validateOrder(dto, userId);

    // Determine order type defaults
    const isMarketOrder = dto.isMarketOrder ?? false;
    const isLimitOrder = dto.isLimitOrder ?? !isMarketOrder;

    // Lock balance if sell order
    if (dto.orderType === 'sell') {
      await this.balanceService.lockBalance(
        userId,
        dto.assetId,
        BigInt(dto.quantity),
      );
    }

    // Calculate total value
    const totalValueUsd = dto.pricePerTokenUsd * dto.quantity;

    // Create order
    const order = this.orderRepository.create({
      orderId: this.generateOrderId(),
      userId,
      assetId: dto.assetId,
      orderType: dto.orderType,
      pricePerTokenUsd: dto.pricePerTokenUsd,
      quantity: BigInt(dto.quantity),
      remainingQuantity: BigInt(dto.quantity),
      filledQuantity: BigInt(0),
      totalValueUsd,
      isMarketOrder,
      isLimitOrder,
      expiresAt: dto.expiresAt ? new Date(dto.expiresAt) : null,
      blockchain: dto.blockchain || 'solana',
      orderStatus: 'pending',
    });

    const savedOrder = await this.orderRepository.save(order);

    // Trigger order matching (this will set status to 'open' or 'filled')
    try {
      await this.orderMatchingService.processOrder(savedOrder);
    } catch (error) {
      this.logger.error(
        `Error processing order ${savedOrder.orderId}: ${error.message}`,
      );
      // If matching fails, unlock balance if it was a sell order
      if (dto.orderType === 'sell') {
        await this.balanceService.unlockBalance(
          userId,
          dto.assetId,
          BigInt(dto.quantity),
        );
      }
      savedOrder.orderStatus = 'rejected';
      await this.orderRepository.save(savedOrder);
      throw new BadRequestException(
        `Failed to process order: ${error.message}`,
      );
    }

    return savedOrder;
  }

  /**
   * Validate order before creation
   */
  async validateOrder(dto: CreateOrderDto, userId: string): Promise<void> {
    // Validate asset exists
    const asset = await this.assetRepository.findOne({
      where: { id: dto.assetId },
    });

    if (!asset) {
      throw new NotFoundException('Asset not found');
    }

    if (asset.status !== 'trading' && asset.status !== 'listed') {
      throw new BadRequestException('Asset not available for trading');
    }

    // Validate price and quantity
    if (dto.pricePerTokenUsd <= 0 || dto.quantity <= 0) {
      throw new BadRequestException('Invalid price or quantity');
    }

    // Validate balance for sell orders
    if (dto.orderType === 'sell') {
      const balance = await this.balanceService.getBalance(
        userId,
        dto.assetId,
      );

      if (balance.availableBalance < BigInt(dto.quantity)) {
        throw new BadRequestException('Insufficient balance');
      }
    } else if (dto.orderType === 'buy') {
      // Validate user has enough funds (USDC or native token)
      const totalCost = dto.pricePerTokenUsd * dto.quantity;
      // TODO: Check payment token balance when payment system is implemented
      // For now, we'll allow the order to be created
      this.logger.warn(
        `Buy order validation: Payment token balance check not yet implemented. Total cost: ${totalCost} USD`,
      );
    }

    // Validate market order doesn't have price (should use best available)
    if (dto.isMarketOrder && dto.pricePerTokenUsd) {
      this.logger.warn(
        'Market order with price specified - price will be ignored',
      );
    }
  }

  /**
   * Cancel an order
   */
  async cancel(orderId: string, userId: string): Promise<Order> {
    const order = await this.orderRepository.findOne({
      where: { orderId, userId },
    });

    if (!order) {
      throw new NotFoundException('Order not found');
    }

    if (order.orderStatus !== 'open' && order.orderStatus !== 'pending') {
      throw new BadRequestException(
        `Order cannot be cancelled. Current status: ${order.orderStatus}`,
      );
    }

    // Unlock balance if sell order
    if (order.orderType === 'sell') {
      await this.balanceService.unlockBalance(
        userId,
        order.assetId,
        order.remainingQuantity,
      );
    }

    order.orderStatus = 'cancelled';
    return this.orderRepository.save(order);
  }

  /**
   * Update an order (only price, quantity, or expiration)
   */
  async update(
    orderId: string,
    dto: UpdateOrderDto,
    userId: string,
  ): Promise<Order> {
    const order = await this.orderRepository.findOne({
      where: { orderId, userId },
    });

    if (!order) {
      throw new NotFoundException('Order not found');
    }

    if (order.orderStatus !== 'open' && order.orderStatus !== 'pending') {
      throw new BadRequestException('Order cannot be updated');
    }

    // If quantity is being updated for a sell order, adjust locked balance
    if (dto.quantity !== undefined && order.orderType === 'sell') {
      const quantityDiff = BigInt(dto.quantity) - order.quantity;
      if (quantityDiff > 0) {
        // Need to lock more
        await this.balanceService.lockBalance(
          userId,
          order.assetId,
          quantityDiff,
        );
      } else if (quantityDiff < 0) {
        // Can unlock some
        await this.balanceService.unlockBalance(
          userId,
          order.assetId,
          -quantityDiff,
        );
      }
      order.quantity = BigInt(dto.quantity);
      order.remainingQuantity =
        order.quantity - order.filledQuantity < BigInt(0)
          ? BigInt(0)
          : order.quantity - order.filledQuantity;
    }

    if (dto.pricePerTokenUsd !== undefined) {
      order.pricePerTokenUsd = dto.pricePerTokenUsd;
    }

    if (dto.expiresAt !== undefined) {
      order.expiresAt = new Date(dto.expiresAt);
    }

    // Recalculate total value
    order.totalValueUsd =
      Number(order.remainingQuantity) * order.pricePerTokenUsd;

    const updatedOrder = await this.orderRepository.save(order);

    // If price or quantity changed, re-process for matching
    if (order.orderStatus === 'open') {
      await this.orderMatchingService.processOrder(updatedOrder);
    }

    return updatedOrder;
  }

  /**
   * Find orders by user with filters
   */
  async findByUser(
    userId: string,
    filters?: {
      status?: string;
      orderType?: 'buy' | 'sell';
      assetId?: string;
      page?: number;
      limit?: number;
    },
  ) {
    const {
      status,
      orderType,
      assetId,
      page = 1,
      limit = 20,
    } = filters || {};
    const skip = (page - 1) * limit;

    const queryBuilder = this.orderRepository
      .createQueryBuilder('order')
      .where('order.userId = :userId', { userId })
      .leftJoinAndSelect('order.asset', 'asset')
      .orderBy('order.createdAt', 'DESC');

    if (status) {
      queryBuilder.andWhere('order.orderStatus = :status', { status });
    }

    if (orderType) {
      queryBuilder.andWhere('order.orderType = :orderType', { orderType });
    }

    if (assetId) {
      queryBuilder.andWhere('order.assetId = :assetId', { assetId });
    }

    queryBuilder.skip(skip).take(limit);

    const [items, total] = await queryBuilder.getManyAndCount();

    return {
      items,
      total,
      page,
      limit,
      totalPages: Math.ceil(total / limit),
    };
  }

  /**
   * Find one order by orderId
   */
  async findOne(orderId: string, userId: string): Promise<Order> {
    const order = await this.orderRepository.findOne({
      where: { orderId, userId },
      relations: ['asset', 'user'],
    });

    if (!order) {
      throw new NotFoundException('Order not found');
    }

    return order;
  }

  /**
   * Get open orders for a user
   */
  async getOpenOrders(userId: string) {
    return this.findByUser(userId, { status: 'open' });
  }

  /**
   * Get order history for a user
   */
  async getHistory(
    userId: string,
    filters?: {
      status?: string;
      orderType?: 'buy' | 'sell';
      assetId?: string;
      page?: number;
      limit?: number;
    },
  ) {
    // History includes filled, cancelled, expired orders
    const historyStatuses = ['filled', 'cancelled', 'expired'];
    return this.findByUser(userId, {
      ...filters,
      status: filters?.status || undefined,
    });
  }

  /**
   * Get orders for a specific asset
   */
  async findByAsset(assetId: string, filters?: { page?: number; limit?: number }) {
    const { page = 1, limit = 20 } = filters || {};
    const skip = (page - 1) * limit;

    const queryBuilder = this.orderRepository
      .createQueryBuilder('order')
      .where('order.assetId = :assetId', { assetId })
      .andWhere('order.orderStatus = :status', { status: 'open' })
      .leftJoinAndSelect('order.user', 'user')
      .orderBy('order.pricePerTokenUsd', 'DESC')
      .addOrderBy('order.createdAt', 'ASC');

    queryBuilder.skip(skip).take(limit);

    const [items, total] = await queryBuilder.getManyAndCount();

    return {
      items,
      total,
      page,
      limit,
      totalPages: Math.ceil(total / limit),
    };
  }
}












