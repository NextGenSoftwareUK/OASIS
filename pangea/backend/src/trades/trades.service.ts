import { Injectable, NotFoundException } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { Trade } from './entities/trade.entity';
import { CreateTradeDto } from './dto/create-trade.dto';
import { TradeFilters, TradeListResponseDto, TradeStatisticsDto } from './dto';

@Injectable()
export class TradesService {
  constructor(
    @InjectRepository(Trade)
    private tradeRepository: Repository<Trade>,
  ) {}

  /**
   * Generate unique trade ID
   * Format: TRD-YYYY-XXX (e.g., TRD-2025-001)
   */
  private generateTradeId(): string {
    const year = new Date().getFullYear();
    const random = Math.floor(Math.random() * 1000)
      .toString()
      .padStart(3, '0');
    return `TRD-${year}-${random}`;
  }

  /**
   * Create a new trade record
   */
  async create(dto: CreateTradeDto): Promise<Trade> {
    const trade = this.tradeRepository.create({
      ...dto,
      tradeId: this.generateTradeId(),
      quantity: BigInt(dto.quantity),
      blockNumber: dto.blockNumber ? BigInt(dto.blockNumber) : null,
    });

    return this.tradeRepository.save(trade);
  }

  /**
   * Find a trade by ID and verify user access
   */
  async findOne(tradeId: string, userId: string): Promise<Trade> {
    const trade = await this.tradeRepository.findOne({
      where: { tradeId },
      relations: ['buyer', 'seller'],
    });

    if (!trade) {
      throw new NotFoundException(`Trade with ID ${tradeId} not found`);
    }

    // Verify user is either buyer or seller
    if (trade.buyer.id !== userId && trade.seller.id !== userId) {
      throw new NotFoundException(`Trade with ID ${tradeId} not found`);
    }

    return trade;
  }

  /**
   * Get all trades for a user with optional filters
   */
  async findByUser(
    userId: string,
    filters?: TradeFilters,
  ): Promise<TradeListResponseDto> {
    const queryBuilder = this.tradeRepository
      .createQueryBuilder('trade')
      .leftJoinAndSelect('trade.buyer', 'buyer')
      .leftJoinAndSelect('trade.seller', 'seller')
      .where(
        '(trade.buyer.id = :userId OR trade.seller.id = :userId)',
        { userId },
      );

    // Apply filters
    if (filters?.assetId) {
      queryBuilder.andWhere('trade.assetId = :assetId', {
        assetId: filters.assetId,
      });
    }

    if (filters?.status) {
      queryBuilder.andWhere('trade.status = :status', {
        status: filters.status,
      });
    }

    if (filters?.settlementStatus) {
      queryBuilder.andWhere('trade.settlementStatus = :settlementStatus', {
        settlementStatus: filters.settlementStatus,
      });
    }

    if (filters?.startDate) {
      queryBuilder.andWhere('trade.executedAt >= :startDate', {
        startDate: filters.startDate,
      });
    }

    if (filters?.endDate) {
      queryBuilder.andWhere('trade.executedAt <= :endDate', {
        endDate: filters.endDate,
      });
    }

    // Get total count before pagination
    const total = await queryBuilder.getCount();

    // Apply pagination
    const page = filters?.page || 1;
    const limit = filters?.limit || 20;
    const skip = (page - 1) * limit;

    queryBuilder
      .orderBy('trade.executedAt', 'DESC')
      .skip(skip)
      .take(limit);

    const items = await queryBuilder.getMany();

    return {
      items: items.map((trade) => this.mapTradeToResponse(trade)),
      total,
      page,
      limit,
      totalPages: Math.ceil(total / limit),
    };
  }

  /**
   * Get trades for a specific asset
   */
  async findByAsset(
    assetId: string,
    filters?: TradeFilters,
  ): Promise<TradeListResponseDto> {
    const queryBuilder = this.tradeRepository
      .createQueryBuilder('trade')
      .leftJoinAndSelect('trade.buyer', 'buyer')
      .leftJoinAndSelect('trade.seller', 'seller')
      .where('trade.assetId = :assetId', { assetId });

    // Apply filters
    if (filters?.status) {
      queryBuilder.andWhere('trade.status = :status', {
        status: filters.status,
      });
    }

    if (filters?.settlementStatus) {
      queryBuilder.andWhere('trade.settlementStatus = :settlementStatus', {
        settlementStatus: filters.settlementStatus,
      });
    }

    if (filters?.startDate) {
      queryBuilder.andWhere('trade.executedAt >= :startDate', {
        startDate: filters.startDate,
      });
    }

    if (filters?.endDate) {
      queryBuilder.andWhere('trade.executedAt <= :endDate', {
        endDate: filters.endDate,
      });
    }

    // Get total count before pagination
    const total = await queryBuilder.getCount();

    // Apply pagination
    const page = filters?.page || 1;
    const limit = filters?.limit || 20;
    const skip = (page - 1) * limit;

    queryBuilder
      .orderBy('trade.executedAt', 'DESC')
      .skip(skip)
      .take(limit);

    const items = await queryBuilder.getMany();

    return {
      items: items.map((trade) => this.mapTradeToResponse(trade)),
      total,
      page,
      limit,
      totalPages: Math.ceil(total / limit),
    };
  }

  /**
   * Get trade statistics for a user
   */
  async getStatistics(
    userId: string,
    assetId?: string,
  ): Promise<TradeStatisticsDto> {
    const queryBuilder = this.tradeRepository
      .createQueryBuilder('trade')
      .select('COUNT(trade.id)', 'totalTrades')
      .addSelect('COALESCE(SUM(trade.totalValueUsd), 0)', 'totalVolume')
      .addSelect(
        'COALESCE(AVG(trade.totalValueUsd), 0)',
        'averageTradeSize',
      )
      .addSelect('COALESCE(MIN(trade.pricePerTokenUsd), 0)', 'minPrice')
      .addSelect('COALESCE(MAX(trade.pricePerTokenUsd), 0)', 'maxPrice')
      .addSelect(
        'COALESCE(AVG(trade.pricePerTokenUsd), 0)',
        'averagePrice',
      )
      .where(
        '(trade.buyer.id = :userId OR trade.seller.id = :userId)',
        { userId },
      );

    if (assetId) {
      queryBuilder.andWhere('trade.assetId = :assetId', { assetId });
    }

    const result = await queryBuilder.getRawOne();

    return {
      totalTrades: parseInt(result.totalTrades) || 0,
      totalVolume: result.totalVolume || '0',
      averageTradeSize: result.averageTradeSize || '0',
      minPrice: result.minPrice || '0',
      maxPrice: result.maxPrice || '0',
      averagePrice: result.averagePrice || '0',
    };
  }

  /**
   * Map Trade entity to response DTO
   */
  mapTradeToResponse(trade: Trade): any {
    return {
      id: trade.id,
      tradeId: trade.tradeId,
      buyer: {
        id: trade.buyer.id,
        email: trade.buyer.email,
        username: trade.buyer.username,
      },
      seller: {
        id: trade.seller.id,
        email: trade.seller.email,
        username: trade.seller.username,
      },
      assetId: trade.assetId,
      buyOrderId: trade.buyOrderId,
      sellOrderId: trade.sellOrderId,
      quantity: trade.quantity.toString(),
      pricePerTokenUsd: trade.pricePerTokenUsd.toString(),
      totalValueUsd: trade.totalValueUsd.toString(),
      platformFeeUsd: trade.platformFeeUsd.toString(),
      platformFeePercentage: trade.platformFeePercentage.toString(),
      blockchain: trade.blockchain,
      transactionHash: trade.transactionHash,
      blockNumber: trade.blockNumber?.toString(),
      executedAt: trade.executedAt,
      confirmedAt: trade.confirmedAt,
      status: trade.status,
      settlementStatus: trade.settlementStatus,
    };
  }
}












