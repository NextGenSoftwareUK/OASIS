import { Test, TestingModule } from '@nestjs/testing';
import { getRepositoryToken } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { TradesService } from './trades.service';
import { Trade } from './entities/trade.entity';
import { CreateTradeDto } from './dto/create-trade.dto';
import { NotFoundException } from '@nestjs/common';

describe('TradesService', () => {
  let service: TradesService;
  let repository: Repository<Trade>;

  const mockRepository = {
    create: jest.fn(),
    save: jest.fn(),
    findOne: jest.fn(),
    createQueryBuilder: jest.fn(),
  };

  const mockQueryBuilder = {
    leftJoinAndSelect: jest.fn().mockReturnThis(),
    where: jest.fn().mockReturnThis(),
    andWhere: jest.fn().mockReturnThis(),
    orderBy: jest.fn().mockReturnThis(),
    skip: jest.fn().mockReturnThis(),
    take: jest.fn().mockReturnThis(),
    getCount: jest.fn(),
    getMany: jest.fn(),
    getRawOne: jest.fn(),
    select: jest.fn().mockReturnThis(),
    addSelect: jest.fn().mockReturnThis(),
  };

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      providers: [
        TradesService,
        {
          provide: getRepositoryToken(Trade),
          useValue: mockRepository,
        },
      ],
    }).compile();

    service = module.get<TradesService>(TradesService);
    repository = module.get<Repository<Trade>>(getRepositoryToken(Trade));

    // Reset mocks
    jest.clearAllMocks();
    mockRepository.createQueryBuilder.mockReturnValue(mockQueryBuilder);
  });

  it('should be defined', () => {
    expect(service).toBeDefined();
  });

  describe('create', () => {
    it('should create a new trade with generated trade ID', async () => {
      const createDto: CreateTradeDto = {
        buyerId: 'buyer-uuid',
        sellerId: 'seller-uuid',
        assetId: 'asset-uuid',
        quantity: 100,
        pricePerTokenUsd: 10.5,
        totalValueUsd: 1050,
        blockchain: 'solana',
        transactionHash: 'tx-hash-123',
      };

      const mockTrade = {
        id: 'trade-uuid',
        tradeId: 'TRD-2025-001',
        ...createDto,
        quantity: BigInt(100),
      };

      mockRepository.create.mockReturnValue(mockTrade);
      mockRepository.save.mockResolvedValue(mockTrade);

      const result = await service.create(createDto);

      expect(mockRepository.create).toHaveBeenCalledWith(
        expect.objectContaining({
          ...createDto,
          tradeId: expect.stringMatching(/^TRD-\d{4}-\d{3}$/),
          quantity: BigInt(100),
        }),
      );
      expect(mockRepository.save).toHaveBeenCalled();
      expect(result).toEqual(mockTrade);
    });
  });

  describe('findOne', () => {
    it('should return a trade when user is the buyer', async () => {
      const tradeId = 'TRD-2025-001';
      const userId = 'buyer-uuid';

      const mockTrade = {
        id: 'trade-uuid',
        tradeId,
        buyer: { id: userId, email: 'buyer@test.com' },
        seller: { id: 'seller-uuid', email: 'seller@test.com' },
      };

      mockRepository.findOne.mockResolvedValue(mockTrade);

      const result = await service.findOne(tradeId, userId);

      expect(mockRepository.findOne).toHaveBeenCalledWith({
        where: { tradeId },
        relations: ['buyer', 'seller'],
      });
      expect(result).toEqual(mockTrade);
    });

    it('should return a trade when user is the seller', async () => {
      const tradeId = 'TRD-2025-001';
      const userId = 'seller-uuid';

      const mockTrade = {
        id: 'trade-uuid',
        tradeId,
        buyer: { id: 'buyer-uuid', email: 'buyer@test.com' },
        seller: { id: userId, email: 'seller@test.com' },
      };

      mockRepository.findOne.mockResolvedValue(mockTrade);

      const result = await service.findOne(tradeId, userId);

      expect(result).toEqual(mockTrade);
    });

    it('should throw NotFoundException when trade does not exist', async () => {
      const tradeId = 'TRD-2025-999';
      const userId = 'user-uuid';

      mockRepository.findOne.mockResolvedValue(null);

      await expect(service.findOne(tradeId, userId)).rejects.toThrow(
        NotFoundException,
      );
    });

    it('should throw NotFoundException when user is not buyer or seller', async () => {
      const tradeId = 'TRD-2025-001';
      const userId = 'unauthorized-uuid';

      const mockTrade = {
        id: 'trade-uuid',
        tradeId,
        buyer: { id: 'buyer-uuid', email: 'buyer@test.com' },
        seller: { id: 'seller-uuid', email: 'seller@test.com' },
      };

      mockRepository.findOne.mockResolvedValue(mockTrade);

      await expect(service.findOne(tradeId, userId)).rejects.toThrow(
        NotFoundException,
      );
    });
  });

  describe('findByUser', () => {
    it('should return paginated trades for a user', async () => {
      const userId = 'user-uuid';
      const filters = { page: 1, limit: 20 };

      const mockTrades = [
        {
          id: 'trade-1',
          tradeId: 'TRD-2025-001',
          buyer: { id: userId, email: 'user@test.com' },
          seller: { id: 'seller-uuid', email: 'seller@test.com' },
          assetId: 'asset-uuid',
          quantity: BigInt(100),
          pricePerTokenUsd: 10.5,
          totalValueUsd: 1050,
        },
      ];

      mockQueryBuilder.getCount.mockResolvedValue(1);
      mockQueryBuilder.getMany.mockResolvedValue(mockTrades);

      const result = await service.findByUser(userId, filters);

      expect(mockQueryBuilder.where).toHaveBeenCalled();
      expect(mockQueryBuilder.skip).toHaveBeenCalledWith(0);
      expect(mockQueryBuilder.take).toHaveBeenCalledWith(20);
      expect(result.total).toBe(1);
      expect(result.items).toHaveLength(1);
    });

    it('should apply asset filter', async () => {
      const userId = 'user-uuid';
      const filters = { assetId: 'asset-uuid' };

      mockQueryBuilder.getCount.mockResolvedValue(0);
      mockQueryBuilder.getMany.mockResolvedValue([]);

      await service.findByUser(userId, filters);

      expect(mockQueryBuilder.andWhere).toHaveBeenCalledWith(
        'trade.assetId = :assetId',
        { assetId: 'asset-uuid' },
      );
    });

    it('should apply date range filters', async () => {
      const userId = 'user-uuid';
      const startDate = new Date('2025-01-01');
      const endDate = new Date('2025-01-31');
      const filters = { startDate, endDate };

      mockQueryBuilder.getCount.mockResolvedValue(0);
      mockQueryBuilder.getMany.mockResolvedValue([]);

      await service.findByUser(userId, filters);

      expect(mockQueryBuilder.andWhere).toHaveBeenCalledWith(
        'trade.executedAt >= :startDate',
        { startDate },
      );
      expect(mockQueryBuilder.andWhere).toHaveBeenCalledWith(
        'trade.executedAt <= :endDate',
        { endDate },
      );
    });
  });

  describe('findByAsset', () => {
    it('should return trades for a specific asset', async () => {
      const assetId = 'asset-uuid';
      const filters = { page: 1, limit: 20 };

      const mockTrades = [
        {
          id: 'trade-1',
          tradeId: 'TRD-2025-001',
          assetId,
          buyer: { id: 'buyer-uuid', email: 'buyer@test.com' },
          seller: { id: 'seller-uuid', email: 'seller@test.com' },
        },
      ];

      mockQueryBuilder.getCount.mockResolvedValue(1);
      mockQueryBuilder.getMany.mockResolvedValue(mockTrades);

      const result = await service.findByAsset(assetId, filters);

      expect(mockQueryBuilder.where).toHaveBeenCalledWith(
        'trade.assetId = :assetId',
        { assetId },
      );
      expect(result.total).toBe(1);
      expect(result.items).toHaveLength(1);
    });
  });

  describe('getStatistics', () => {
    it('should return trade statistics for a user', async () => {
      const userId = 'user-uuid';

      const mockStats = {
        totalTrades: '5',
        totalVolume: '10000.00',
        averageTradeSize: '2000.00',
        minPrice: '9.50',
        maxPrice: '11.50',
        averagePrice: '10.50',
      };

      mockQueryBuilder.getRawOne.mockResolvedValue(mockStats);

      const result = await service.getStatistics(userId);

      expect(mockQueryBuilder.select).toHaveBeenCalled();
      expect(mockQueryBuilder.where).toHaveBeenCalled();
      expect(result.totalTrades).toBe(5);
      expect(result.totalVolume).toBe('10000.00');
    });

    it('should return statistics filtered by asset', async () => {
      const userId = 'user-uuid';
      const assetId = 'asset-uuid';

      const mockStats = {
        totalTrades: '2',
        totalVolume: '5000.00',
        averageTradeSize: '2500.00',
        minPrice: '10.00',
        maxPrice: '11.00',
        averagePrice: '10.50',
      };

      mockQueryBuilder.getRawOne.mockResolvedValue(mockStats);

      const result = await service.getStatistics(userId, assetId);

      expect(mockQueryBuilder.andWhere).toHaveBeenCalledWith(
        'trade.assetId = :assetId',
        { assetId },
      );
      expect(result.totalTrades).toBe(2);
    });
  });
});












