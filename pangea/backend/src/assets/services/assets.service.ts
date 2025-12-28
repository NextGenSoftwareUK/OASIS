import {
  Injectable,
  NotFoundException,
  BadRequestException,
  Logger,
} from '@nestjs/common';
import { InjectRepository, InjectDataSource } from '@nestjs/typeorm';
import { Repository, DataSource } from 'typeorm';
import { TokenizedAsset } from '../entities/tokenized-asset.entity';
import { CreateAssetDto, UpdateAssetDto, FindAssetsDto } from '../dto';
import { SmartContractService } from '../../smart-contracts/services/smart-contract.service';
import { Order } from '../../orders/entities/order.entity';
import { Trade } from '../../trades/entities/trade.entity';

@Injectable()
export class AssetsService {
  private readonly logger = new Logger(AssetsService.name);

  constructor(
    @InjectRepository(TokenizedAsset)
    private assetRepository: Repository<TokenizedAsset>,
    @InjectDataSource()
    private dataSource: DataSource,
    private smartContractService: SmartContractService,
  ) {}

  private get orderRepository(): Repository<Order> {
    return this.dataSource.getRepository(Order);
  }

  private get tradeRepository(): Repository<Trade> {
    return this.dataSource.getRepository(Trade);
  }

  /**
   * Generate unique asset ID
   * Format: {SYMBOL}-{YEAR}-{SEQUENCE}
   * Example: BHE-2025-001
   */
  private generateAssetId(symbol: string): string {
    const year = new Date().getFullYear();
    const prefix = symbol.toUpperCase().substring(0, 3);
    // In production, get the next sequence from database
    const sequence = Math.floor(Math.random() * 1000)
      .toString()
      .padStart(3, '0');
    return `${prefix}-${year}-${sequence}`;
  }

  /**
   * Find all assets with pagination and filters
   */
  async findAll(query: FindAssetsDto) {
    const { page = 1, limit = 20, ...filters } = query;
    const skip = (page - 1) * limit;

    const queryBuilder = this.assetRepository.createQueryBuilder('asset');

    if (filters.status) {
      queryBuilder.where('asset.status = :status', { status: filters.status });
    }

    if (filters.assetClass) {
      queryBuilder.andWhere('asset.assetClass = :assetClass', {
        assetClass: filters.assetClass,
      });
    }

    if (filters.blockchain) {
      queryBuilder.andWhere('asset.blockchain = :blockchain', {
        blockchain: filters.blockchain,
      });
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
   * Find one asset by assetId
   */
  async findOne(assetId: string): Promise<TokenizedAsset> {
    const asset = await this.assetRepository.findOne({
      where: { assetId },
      relations: ['issuer'],
    });

    if (!asset) {
      throw new NotFoundException(`Asset with ID ${assetId} not found`);
    }

    return asset;
  }

  /**
   * Create a new asset
   */
  async create(dto: CreateAssetDto, issuerId: string): Promise<TokenizedAsset> {
    this.logger.log(`Creating asset: ${dto.name}`);

    // Generate unique asset ID
    const assetId = this.generateAssetId(dto.symbol);

    // Check if assetId already exists (unlikely but possible)
    const existing = await this.assetRepository.findOne({
      where: { assetId },
    });
    if (existing) {
      // Regenerate if collision
      const newAssetId = this.generateAssetId(dto.symbol + Date.now());
      return this.create({ ...dto }, issuerId);
    }

    // Create asset record
    const asset = this.assetRepository.create({
      ...dto,
      assetId,
      totalSupply: BigInt(dto.totalSupply || 0),
      issuer: { id: issuerId } as any,
      issuerId,
      network: dto.network || 'devnet',
      decimals: dto.decimals ?? 0,
      status: dto.status || 'draft',
    });

    // Deploy smart contract if requested
    if (dto.deployContract) {
      try {
        const deployResult = await this.smartContractService.generateRwaToken({
          name: dto.name,
          symbol: dto.symbol,
          totalSupply: dto.totalSupply,
          metadataUri: dto.metadataUri || '',
          issuerWallet: dto.issuerWallet || '',
          decimals: dto.decimals,
        });

        asset.contractAddress = deployResult.contractAddress;
        asset.mintAddress = deployResult.contractAddress; // For Solana, mint address is the contract address
      } catch (error) {
        this.logger.error(`Failed to deploy contract: ${error.message}`, error.stack);
        throw new BadRequestException(
          `Failed to deploy smart contract: ${error.message}`,
        );
      }
    }

    // Calculate price per token if total value is provided
    if (dto.totalValueUsd && dto.totalSupply) {
      asset.pricePerTokenUsd = Number(
        (dto.totalValueUsd / dto.totalSupply).toFixed(2),
      );
    }

    return this.assetRepository.save(asset);
  }

  /**
   * Update an asset
   */
  async update(
    assetId: string,
    dto: UpdateAssetDto,
  ): Promise<TokenizedAsset> {
    const asset = await this.findOne(assetId);

    // Update fields
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

    return this.assetRepository.save(asset);
  }

  /**
   * Delete an asset (soft delete by setting status to 'closed')
   */
  async delete(assetId: string): Promise<void> {
    const asset = await this.findOne(assetId);

    // Instead of hard delete, mark as closed
    asset.status = 'closed';
    await this.assetRepository.save(asset);

    this.logger.log(`Asset ${assetId} marked as closed`);
  }

  /**
   * Get current price for an asset from order book
   */
  async getCurrentPrice(assetId: string): Promise<{
    price: number | null;
    bestBid: number | null;
    bestAsk: number | null;
    lastTrade: number | null;
  }> {
    // Verify asset exists
    await this.findOne(assetId);

    // Get best bid (highest buy order price)
    const bestBid = await this.orderRepository
      .createQueryBuilder('order')
      .where('order.assetId = :assetId', { assetId })
      .andWhere('order.orderType = :type', { type: 'buy' })
      .andWhere('order.orderStatus = :status', { status: 'open' })
      .orderBy('order.pricePerTokenUsd', 'DESC')
      .limit(1)
      .getOne();

    // Get best ask (lowest sell order price)
    const bestAsk = await this.orderRepository
      .createQueryBuilder('order')
      .where('order.assetId = :assetId', { assetId })
      .andWhere('order.orderType = :type', { type: 'sell' })
      .andWhere('order.orderStatus = :status', { status: 'open' })
      .orderBy('order.pricePerTokenUsd', 'ASC')
      .limit(1)
      .getOne();

    // Get last trade price
    const lastTrade = await this.tradeRepository
      .createQueryBuilder('trade')
      .where('trade.assetId = :assetId', { assetId })
      .orderBy('trade.executedAt', 'DESC')
      .limit(1)
      .getOne();

    // Calculate current price (mid-point of bid/ask, or last trade, or null)
    let price: number | null = null;
    if (bestBid && bestAsk) {
      price = Number(
        ((bestBid.pricePerTokenUsd + bestAsk.pricePerTokenUsd) / 2).toFixed(2),
      );
    } else if (lastTrade) {
      price = Number(lastTrade.pricePerTokenUsd);
    }

    return {
      price,
      bestBid: bestBid ? Number(bestBid.pricePerTokenUsd) : null,
      bestAsk: bestAsk ? Number(bestAsk.pricePerTokenUsd) : null,
      lastTrade: lastTrade ? Number(lastTrade.pricePerTokenUsd) : null,
    };
  }

  /**
   * Search assets by name, symbol, or description
   */
  async search(query: string): Promise<TokenizedAsset[]> {
    if (!query || query.trim().length === 0) {
      return [];
    }

    const searchTerm = `%${query.trim()}%`;

    return this.assetRepository
      .createQueryBuilder('asset')
      .where('asset.name ILIKE :search', { search: searchTerm })
      .orWhere('asset.symbol ILIKE :search', { search: searchTerm })
      .orWhere('asset.description ILIKE :search', { search: searchTerm })
      .leftJoinAndSelect('asset.issuer', 'issuer')
      .orderBy('asset.createdAt', 'DESC')
      .limit(50)
      .getMany();
  }

  /**
   * Get order book for an asset
   */
  async getOrderBook(assetId: string): Promise<{
    bids: Order[];
    asks: Order[];
  }> {
    // Verify asset exists
    await this.findOne(assetId);

    // Get buy orders (bids) - sorted by price descending
    const bids = await this.orderRepository.find({
      where: {
        assetId,
        orderType: 'buy',
        orderStatus: 'open',
      },
      order: {
        pricePerTokenUsd: 'DESC',
      },
      take: 20, // Top 20 bids
    });

    // Get sell orders (asks) - sorted by price ascending
    const asks = await this.orderRepository.find({
      where: {
        assetId,
        orderType: 'sell',
        orderStatus: 'open',
      },
      order: {
        pricePerTokenUsd: 'ASC',
      },
      take: 20, // Top 20 asks
    });

    return { bids, asks };
  }

  /**
   * Get trade history for an asset
   */
  async getTradeHistory(
    assetId: string,
    limit: number = 50,
  ): Promise<Trade[]> {
    // Verify asset exists
    await this.findOne(assetId);

    return this.tradeRepository.find({
      where: { assetId },
      order: {
        executedAt: 'DESC',
      },
      take: limit,
      relations: ['buyer', 'seller'],
    });
  }
}












