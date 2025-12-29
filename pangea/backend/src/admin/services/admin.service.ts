import {
  Injectable,
  NotFoundException,
  BadRequestException,
  Logger,
} from '@nestjs/common';
import { InjectRepository, InjectDataSource } from '@nestjs/typeorm';
import { Repository, DataSource } from 'typeorm';
import { User } from '../../users/entities/user.entity';
import { TokenizedAsset } from '../../assets/entities/tokenized-asset.entity';
import { Order } from '../../orders/entities/order.entity';
import { Trade } from '../../trades/entities/trade.entity';
import { Transaction } from '../../transactions/entities/transaction.entity';
import {
  AdminUserFiltersDto,
  UpdateUserDto,
  UpdateKycStatusDto,
  AdminAssetFiltersDto,
  AdminOrderFiltersDto,
  AdminTradeFiltersDto,
  AdminTransactionFiltersDto,
  AnalyticsFiltersDto,
  AnalyticsPeriod,
} from '../dto';
import { CreateAssetDto } from '../../assets/dto/create-asset.dto';
import { UpdateAssetDto } from '../../assets/dto/update-asset.dto';

@Injectable()
export class AdminService {
  private readonly logger = new Logger(AdminService.name);

  constructor(
    @InjectRepository(User)
    private userRepository: Repository<User>,
    @InjectRepository(TokenizedAsset)
    private assetRepository: Repository<TokenizedAsset>,
    @InjectRepository(Order)
    private orderRepository: Repository<Order>,
    @InjectRepository(Trade)
    private tradeRepository: Repository<Trade>,
    @InjectRepository(Transaction)
    private transactionRepository: Repository<Transaction>,
    @InjectDataSource()
    private dataSource: DataSource,
  ) {}

  // ==================== USER MANAGEMENT ====================

  /**
   * Get all users with filters and pagination
   */
  async getUsers(filters: AdminUserFiltersDto) {
    const { page = 1, limit = 20, kycStatus, role, search } = filters;
    const skip = (page - 1) * limit;

    const queryBuilder = this.userRepository.createQueryBuilder('user');

    if (kycStatus) {
      queryBuilder.andWhere('user.kycStatus = :kycStatus', { kycStatus });
    }

    if (role) {
      queryBuilder.andWhere('user.role = :role', { role });
    }

    if (search) {
      queryBuilder.andWhere(
        '(user.email ILIKE :search OR user.username ILIKE :search OR user.firstName ILIKE :search OR user.lastName ILIKE :search)',
        { search: `%${search}%` },
      );
    }

    queryBuilder.orderBy('user.createdAt', 'DESC').skip(skip).take(limit);

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
   * Get user by ID
   */
  async getUser(userId: string): Promise<User> {
    const user = await this.userRepository.findOne({
      where: { id: userId },
    });

    if (!user) {
      throw new NotFoundException(`User with ID ${userId} not found`);
    }

    return user;
  }

  /**
   * Update user information
   */
  async updateUser(userId: string, dto: UpdateUserDto): Promise<User> {
    const user = await this.getUser(userId);

    Object.assign(user, dto);

    this.logger.log(`Admin updated user ${userId}: ${JSON.stringify(dto)}`);

    return this.userRepository.save(user);
  }

  /**
   * Update user KYC status
   */
  async updateUserKycStatus(
    userId: string,
    dto: UpdateKycStatusDto,
  ): Promise<User> {
    const user = await this.getUser(userId);

    user.kycStatus = dto.status;

    // Note: Rejection reason could be stored in a separate audit log table
    // For now, we just update the status
    this.logger.log(
      `Admin updated KYC status for user ${userId}: ${dto.status}${dto.reason ? ` - Reason: ${dto.reason}` : ''}`,
    );

    return this.userRepository.save(user);
  }

  /**
   * Get user activity logs (placeholder - can be extended with actual logging)
   */
  async getUserActivityLogs(userId: string) {
    // In a real implementation, this would query an activity log table
    // For now, return basic user info with timestamps
    const user = await this.getUser(userId);

    return {
      userId: user.id,
      email: user.email,
      lastLogin: user.lastLogin,
      createdAt: user.createdAt,
      updatedAt: user.updatedAt,
      // Additional activity can be added here
      orders: await this.orderRepository.count({ where: { userId } }),
      trades: await this.tradeRepository.count({
        where: [{ buyerId: userId }, { sellerId: userId }],
      }),
      transactions: await this.transactionRepository.count({
        where: { userId },
      }),
    };
  }

  // ==================== ASSET MANAGEMENT ====================

  /**
   * Get all assets with filters and pagination
   */
  async getAssets(filters: AdminAssetFiltersDto) {
    const { page = 1, limit = 20, status, assetClass, blockchain, search } =
      filters;
    const skip = (page - 1) * limit;

    const queryBuilder =
      this.assetRepository.createQueryBuilder('asset');

    if (status) {
      queryBuilder.andWhere('asset.status = :status', { status });
    }

    if (assetClass) {
      queryBuilder.andWhere('asset.assetClass = :assetClass', { assetClass });
    }

    if (blockchain) {
      queryBuilder.andWhere('asset.blockchain = :blockchain', { blockchain });
    }

    if (search) {
      queryBuilder.andWhere(
        '(asset.name ILIKE :search OR asset.symbol ILIKE :search OR asset.assetId ILIKE :search)',
        { search: `%${search}%` },
      );
    }

    queryBuilder
      .leftJoinAndSelect('asset.issuer', 'issuer')
      .orderBy('asset.createdAt', 'DESC')
      .skip(skip)
      .take(limit);

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
   * Create asset (admin)
   */
  async createAsset(dto: CreateAssetDto, issuerId?: string): Promise<TokenizedAsset> {
    this.logger.log(`Admin creating asset: ${dto.name}`);

    // Generate asset ID
    let assetId = this.generateAssetId(dto.symbol);
    let attempts = 0;
    const maxAttempts = 10;

    // Check if assetId already exists and regenerate if needed
    while (attempts < maxAttempts) {
      const existing = await this.assetRepository.findOne({
        where: { assetId },
      });
      if (!existing) {
        break;
      }
      assetId = this.generateAssetId(dto.symbol + Date.now() + attempts);
      attempts++;
    }

    if (attempts >= maxAttempts) {
      throw new BadRequestException('Failed to generate unique asset ID');
    }

    const asset = this.assetRepository.create({
      ...dto,
      assetId,
      totalSupply: BigInt(dto.totalSupply),
      issuerId: issuerId || (dto as any).issuerId,
      network: dto.network || 'devnet',
      decimals: dto.decimals ?? 0,
      status: dto.status || 'draft',
    });

    // Calculate price per token if total value is provided
    if (dto.totalValueUsd && dto.totalSupply) {
      asset.pricePerTokenUsd = Number(
        (dto.totalValueUsd / Number(dto.totalSupply)).toFixed(2),
      );
    }

    return this.assetRepository.save(asset);
  }

  /**
   * Update asset (admin)
   */
  async updateAsset(
    assetId: string,
    dto: UpdateAssetDto,
  ): Promise<TokenizedAsset> {
    const asset = await this.assetRepository.findOne({
      where: { id: assetId },
    });

    if (!asset) {
      throw new NotFoundException(`Asset with ID ${assetId} not found`);
    }

    Object.assign(asset, dto);

    // Recalculate price if total value or supply changed
    if (dto.totalValueUsd && asset.totalSupply) {
      asset.pricePerTokenUsd = Number(
        (dto.totalValueUsd / Number(asset.totalSupply)).toFixed(2),
      );
    }

    // Update listed_at if status changes to 'listed'
    if (dto.status === 'listed' && asset.status !== 'listed') {
      asset.listedAt = new Date();
    }

    this.logger.log(`Admin updated asset ${assetId}`);

    return this.assetRepository.save(asset);
  }

  /**
   * Delete asset (soft delete by setting status to 'closed')
   */
  async deleteAsset(assetId: string): Promise<void> {
    const asset = await this.assetRepository.findOne({
      where: { id: assetId },
    });

    if (!asset) {
      throw new NotFoundException(`Asset with ID ${assetId} not found`);
    }

    asset.status = 'closed';
    await this.assetRepository.save(asset);

    this.logger.log(`Admin deleted asset ${assetId}`);
  }

  /**
   * Approve asset listing
   */
  async approveAssetListing(assetId: string): Promise<TokenizedAsset> {
    const asset = await this.assetRepository.findOne({
      where: { id: assetId },
    });

    if (!asset) {
      throw new NotFoundException(`Asset with ID ${assetId} not found`);
    }

    asset.status = 'listed';
    if (!asset.listedAt) {
      asset.listedAt = new Date();
    }

    this.logger.log(`Admin approved asset listing ${assetId}`);

    return this.assetRepository.save(asset);
  }

  /**
   * Get asset statistics
   */
  async getAssetStatistics(assetId: string) {
    const asset = await this.assetRepository.findOne({
      where: { id: assetId },
    });

    if (!asset) {
      throw new NotFoundException(`Asset with ID ${assetId} not found`);
    }

    const [totalTrades, totalVolumeResult, averagePriceResult] =
      await Promise.all([
        this.tradeRepository.count({ where: { assetId } }),
        this.tradeRepository
          .createQueryBuilder('trade')
          .where('trade.assetId = :assetId', { assetId })
          .select('SUM(trade.totalValueUsd)', 'total')
          .getRawOne(),
        this.tradeRepository
          .createQueryBuilder('trade')
          .where('trade.assetId = :assetId', { assetId })
          .select('AVG(trade.pricePerTokenUsd)', 'avg')
          .getRawOne(),
      ]);

    return {
      asset,
      totalTrades,
      totalVolume: totalVolumeResult?.total
        ? Number(totalVolumeResult.total)
        : 0,
      averagePrice: averagePriceResult?.avg
        ? Number(averagePriceResult.avg)
        : 0,
    };
  }

  // ==================== ORDER MANAGEMENT ====================

  /**
   * Get all orders with filters and pagination
   */
  async getOrders(filters: AdminOrderFiltersDto) {
    const {
      page = 1,
      limit = 20,
      orderStatus,
      orderType,
      userId,
      assetId,
    } = filters;
    const skip = (page - 1) * limit;

    const queryBuilder = this.orderRepository.createQueryBuilder('order');

    if (orderStatus) {
      queryBuilder.andWhere('order.orderStatus = :orderStatus', {
        orderStatus,
      });
    }

    if (orderType) {
      queryBuilder.andWhere('order.orderType = :orderType', { orderType });
    }

    if (userId) {
      queryBuilder.andWhere('order.userId = :userId', { userId });
    }

    if (assetId) {
      queryBuilder.andWhere('order.assetId = :assetId', { assetId });
    }

    queryBuilder
      .leftJoinAndSelect('order.user', 'user')
      .leftJoinAndSelect('order.asset', 'asset')
      .orderBy('order.createdAt', 'DESC')
      .skip(skip)
      .take(limit);

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
   * Cancel order (emergency admin action)
   */
  async cancelOrder(orderId: string): Promise<Order> {
    const order = await this.orderRepository.findOne({
      where: { id: orderId },
    });

    if (!order) {
      throw new NotFoundException(`Order with ID ${orderId} not found`);
    }

    if (order.orderStatus === 'filled' || order.orderStatus === 'cancelled') {
      throw new BadRequestException(
        `Cannot cancel order with status: ${order.orderStatus}`,
      );
    }

    order.orderStatus = 'cancelled';
    order.remainingQuantity = BigInt(0);

    this.logger.log(`Admin cancelled order ${orderId}`);

    return this.orderRepository.save(order);
  }

  /**
   * Get order statistics
   */
  async getOrderStatistics() {
    const [
      totalOrders,
      openOrders,
      filledOrders,
      cancelledOrders,
      totalVolume,
    ] = await Promise.all([
      this.orderRepository.count(),
      this.orderRepository.count({ where: { orderStatus: 'open' } }),
      this.orderRepository.count({ where: { orderStatus: 'filled' } }),
      this.orderRepository.count({ where: { orderStatus: 'cancelled' } }),
      this.orderRepository
        .createQueryBuilder('order')
        .select('SUM(order.totalValueUsd)', 'total')
        .getRawOne(),
    ]);

    return {
      totalOrders,
      openOrders,
      filledOrders,
      cancelledOrders,
      totalVolume: totalVolume?.total ? Number(totalVolume.total) : 0,
    };
  }

  // ==================== TRADE MANAGEMENT ====================

  /**
   * Get all trades with filters and pagination
   */
  async getTrades(filters: AdminTradeFiltersDto) {
    const {
      page = 1,
      limit = 20,
      status,
      assetId,
      buyerId,
      sellerId,
      startDate,
      endDate,
    } = filters;
    const skip = (page - 1) * limit;

    const queryBuilder = this.tradeRepository.createQueryBuilder('trade');

    if (status) {
      queryBuilder.andWhere('trade.status = :status', { status });
    }

    if (assetId) {
      queryBuilder.andWhere('trade.assetId = :assetId', { assetId });
    }

    if (buyerId) {
      queryBuilder.andWhere('trade.buyerId = :buyerId', { buyerId });
    }

    if (sellerId) {
      queryBuilder.andWhere('trade.sellerId = :sellerId', { sellerId });
    }

    if (startDate) {
      queryBuilder.andWhere('trade.executedAt >= :startDate', {
        startDate: new Date(startDate),
      });
    }

    if (endDate) {
      queryBuilder.andWhere('trade.executedAt <= :endDate', {
        endDate: new Date(endDate),
      });
    }

    queryBuilder
      .leftJoinAndSelect('trade.buyer', 'buyer')
      .leftJoinAndSelect('trade.seller', 'seller')
      .leftJoinAndSelect('trade.asset', 'asset')
      .orderBy('trade.executedAt', 'DESC')
      .skip(skip)
      .take(limit);

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
   * Get trade statistics
   */
  async getTradeStatistics() {
    const [totalTrades, totalVolume, totalFees, averageTradeSize] =
      await Promise.all([
        this.tradeRepository.count(),
        this.tradeRepository
          .createQueryBuilder('trade')
          .select('SUM(trade.totalValueUsd)', 'total')
          .getRawOne(),
        this.tradeRepository
          .createQueryBuilder('trade')
          .select('SUM(trade.platformFeeUsd)', 'total')
          .getRawOne(),
        this.tradeRepository
          .createQueryBuilder('trade')
          .select('AVG(trade.totalValueUsd)', 'avg')
          .getRawOne(),
      ]);

    return {
      totalTrades,
      totalVolume: totalVolume?.total ? Number(totalVolume.total) : 0,
      totalFees: totalFees?.total ? Number(totalFees.total) : 0,
      averageTradeSize: averageTradeSize?.avg
        ? Number(averageTradeSize.avg)
        : 0,
    };
  }

  // ==================== TRANSACTION MANAGEMENT ====================

  /**
   * Get all transactions with filters and pagination
   */
  async getTransactions(filters: AdminTransactionFiltersDto) {
    const {
      page = 1,
      limit = 20,
      transactionType,
      status,
      userId,
      assetId,
      startDate,
      endDate,
    } = filters;
    const skip = (page - 1) * limit;

    const queryBuilder =
      this.transactionRepository.createQueryBuilder('transaction');

    if (transactionType) {
      queryBuilder.andWhere('transaction.transactionType = :transactionType', {
        transactionType,
      });
    }

    if (status) {
      queryBuilder.andWhere('transaction.status = :status', { status });
    }

    if (userId) {
      queryBuilder.andWhere('transaction.userId = :userId', { userId });
    }

    if (assetId) {
      queryBuilder.andWhere('transaction.assetId = :assetId', { assetId });
    }

    if (startDate) {
      queryBuilder.andWhere('transaction.createdAt >= :startDate', {
        startDate: new Date(startDate),
      });
    }

    if (endDate) {
      queryBuilder.andWhere('transaction.createdAt <= :endDate', {
        endDate: new Date(endDate),
      });
    }

    queryBuilder
      .leftJoinAndSelect('transaction.user', 'user')
      .leftJoinAndSelect('transaction.asset', 'asset')
      .orderBy('transaction.createdAt', 'DESC')
      .skip(skip)
      .take(limit);

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
   * Approve withdrawal (if manual approval is required)
   */
  async approveWithdrawal(transactionId: string): Promise<Transaction> {
    const transaction = await this.transactionRepository.findOne({
      where: { id: transactionId },
    });

    if (!transaction) {
      throw new NotFoundException(
        `Transaction with ID ${transactionId} not found`,
      );
    }

    if (transaction.transactionType !== 'withdrawal') {
      throw new BadRequestException('Only withdrawals can be approved');
    }

    if (transaction.status !== 'pending') {
      throw new BadRequestException(
        `Cannot approve transaction with status: ${transaction.status}`,
      );
    }

    transaction.status = 'processing';

    this.logger.log(`Admin approved withdrawal ${transactionId}`);

    return this.transactionRepository.save(transaction);
  }

  // ==================== PLATFORM STATISTICS ====================

  /**
   * Get platform statistics
   */
  async getPlatformStatistics() {
    const [
      totalUsers,
      activeUsers,
      totalAssets,
      totalOrders,
      totalTrades,
      totalVolume,
      totalRevenue,
    ] = await Promise.all([
      this.userRepository.count(),
      this.userRepository.count({ where: { isActive: true } }),
      this.assetRepository.count(),
      this.orderRepository.count(),
      this.tradeRepository.count(),
      this.tradeRepository
        .createQueryBuilder('trade')
        .select('SUM(trade.totalValueUsd)', 'total')
        .getRawOne(),
      this.tradeRepository
        .createQueryBuilder('trade')
        .select('SUM(trade.platformFeeUsd)', 'total')
        .getRawOne(),
    ]);

    return {
      totalUsers,
      activeUsers,
      totalAssets,
      totalOrders,
      totalTrades,
      totalVolume: totalVolume?.total ? Number(totalVolume.total) : 0,
      totalRevenue: totalRevenue?.total ? Number(totalRevenue.total) : 0,
    };
  }

  // ==================== ANALYTICS ====================

  /**
   * Get analytics data
   */
  async getAnalytics(filters: AnalyticsFiltersDto) {
    const { period = AnalyticsPeriod.MONTH, startDate, endDate, assetId } =
      filters;

    // Calculate date range based on period
    let dateStart: Date;
    let dateEnd: Date = new Date();

    if (startDate && endDate) {
      dateStart = new Date(startDate);
      dateEnd = new Date(endDate);
    } else {
      switch (period) {
        case AnalyticsPeriod.DAY:
          dateStart = new Date();
          dateStart.setDate(dateStart.getDate() - 1);
          break;
        case AnalyticsPeriod.WEEK:
          dateStart = new Date();
          dateStart.setDate(dateStart.getDate() - 7);
          break;
        case AnalyticsPeriod.MONTH:
          dateStart = new Date();
          dateStart.setMonth(dateStart.getMonth() - 1);
          break;
        case AnalyticsPeriod.YEAR:
          dateStart = new Date();
          dateStart.setFullYear(dateStart.getFullYear() - 1);
          break;
        default:
          dateStart = new Date();
          dateStart.setMonth(dateStart.getMonth() - 1);
      }
    }

    // Build query builder for trades
    const tradeQueryBuilder = this.tradeRepository
      .createQueryBuilder('trade')
      .where('trade.executedAt >= :startDate', { startDate })
      .andWhere('trade.executedAt <= :endDate', { endDate });

    if (assetId) {
      tradeQueryBuilder.andWhere('trade.assetId = :assetId', { assetId });
    }

    // Get trade analytics
    const [tradeCount, tradeVolume, tradeFees] = await Promise.all([
      tradeQueryBuilder.getCount(),
      tradeQueryBuilder
        .clone()
        .select('SUM(trade.totalValueUsd)', 'total')
        .getRawOne(),
      tradeQueryBuilder
        .clone()
        .select('SUM(trade.platformFeeUsd)', 'total')
        .getRawOne(),
    ]);

    // Get user analytics
    const newUsers = await this.userRepository
      .createQueryBuilder('user')
      .where('user.createdAt >= :startDate', { startDate })
      .andWhere('user.createdAt <= :endDate', { endDate })
      .getCount();

    // Get order analytics
    const [newOrders, orderVolume] = await Promise.all([
      this.orderRepository
        .createQueryBuilder('order')
        .where('order.createdAt >= :startDate', { startDate })
        .andWhere('order.createdAt <= :endDate', { endDate })
        .getCount(),
      this.orderRepository
        .createQueryBuilder('order')
        .where('order.createdAt >= :startDate', { startDate })
        .andWhere('order.createdAt <= :endDate', { endDate })
        .select('SUM(order.totalValueUsd)', 'total')
        .getRawOne(),
    ]);

    return {
      period,
      dateRange: {
        start: dateStart,
        end: dateEnd,
      },
      trades: {
        count: tradeCount,
        volume: tradeVolume?.total ? Number(tradeVolume.total) : 0,
        fees: tradeFees?.total ? Number(tradeFees.total) : 0,
      },
      users: {
        newUsers,
      },
      orders: {
        count: newOrders,
        volume: orderVolume?.total ? Number(orderVolume.total) : 0,
      },
    };
  }

  // ==================== HELPER METHODS ====================

  /**
   * Generate unique asset ID
   */
  private generateAssetId(symbol: string): string {
    const year = new Date().getFullYear();
    const prefix = symbol.toUpperCase().substring(0, 3);
    const sequence = Math.floor(Math.random() * 1000)
      .toString()
      .padStart(3, '0');
    return `${prefix}-${year}-${sequence}`;
  }
}












