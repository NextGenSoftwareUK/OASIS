import { Test, TestingModule } from '@nestjs/testing';
import { TradesController } from './trades.controller';
import { TradesService } from './trades.service';
import { getRepositoryToken } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { Trade } from './entities/trade.entity';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { NotFoundException } from '@nestjs/common';

describe('TradesController', () => {
  let controller: TradesController;
  let service: TradesService;

  const mockUser = {
    id: 'user-uuid',
    email: 'user@test.com',
    username: 'testuser',
  };

  const mockTradesService = {
    findByUser: jest.fn(),
    findOne: jest.fn(),
    findByAsset: jest.fn(),
    getStatistics: jest.fn(),
  };

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      controllers: [TradesController],
      providers: [
        {
          provide: TradesService,
          useValue: mockTradesService,
        },
        {
          provide: getRepositoryToken(Trade),
          useValue: {},
        },
      ],
    })
      .overrideGuard(JwtAuthGuard)
      .useValue({ canActivate: () => true })
      .compile();

    controller = module.get<TradesController>(TradesController);
    service = module.get<TradesService>(TradesService);

    jest.clearAllMocks();
  });

  it('should be defined', () => {
    expect(controller).toBeDefined();
  });

  describe('findAll', () => {
    it('should return user trades with pagination', async () => {
      const mockResponse = {
        items: [],
        total: 0,
        page: 1,
        limit: 20,
        totalPages: 0,
      };

      mockTradesService.findByUser.mockResolvedValue(mockResponse);

      const req = { user: mockUser };
      const filters = { page: 1, limit: 20 };

      const result = await controller.findAll(req, filters);

      expect(service.findByUser).toHaveBeenCalledWith(
        mockUser.id,
        expect.objectContaining({ page: 1, limit: 20 }),
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('findOne', () => {
    it('should return a trade by ID', async () => {
      const tradeId = 'TRD-2025-001';
      const mockTrade = {
        id: 'trade-uuid',
        tradeId,
        buyer: mockUser,
        seller: { id: 'seller-uuid', email: 'seller@test.com' },
        assetId: 'asset-uuid',
        quantity: BigInt(100),
        pricePerTokenUsd: 10.5,
        totalValueUsd: 1050,
        blockchain: 'solana',
        transactionHash: 'tx-hash',
        status: 'completed',
        settlementStatus: 'settled',
      };

      mockTradesService.findOne.mockResolvedValue(mockTrade);

      const req = { user: mockUser };
      const result = await controller.findOne(tradeId, req);

      expect(service.findOne).toHaveBeenCalledWith(tradeId, mockUser.id);
      expect(result).toMatchObject({
        tradeId,
        buyer: expect.objectContaining({ id: mockUser.id }),
      });
    });

    it('should throw NotFoundException when trade not found', async () => {
      const tradeId = 'TRD-2025-999';
      mockTradesService.findOne.mockRejectedValue(
        new NotFoundException('Trade not found'),
      );

      const req = { user: mockUser };

      await expect(controller.findOne(tradeId, req)).rejects.toThrow(
        NotFoundException,
      );
    });
  });

  describe('findByAsset', () => {
    it('should return trades for a specific asset', async () => {
      const assetId = 'asset-uuid';
      const mockResponse = {
        items: [
          {
            id: 'trade-1',
            tradeId: 'TRD-2025-001',
            assetId,
            buyer: mockUser,
            seller: { id: 'seller-uuid', email: 'seller@test.com' },
          },
        ],
        total: 1,
        page: 1,
        limit: 20,
        totalPages: 1,
      };

      mockTradesService.findByAsset.mockResolvedValue(mockResponse);

      const filters = { page: 1, limit: 20 };
      const result = await controller.findByAsset(assetId, filters);

      expect(service.findByAsset).toHaveBeenCalledWith(
        assetId,
        expect.objectContaining({ page: 1, limit: 20 }),
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getHistory', () => {
    it('should return trade history (alias for findAll)', async () => {
      const mockResponse = {
        items: [],
        total: 0,
        page: 1,
        limit: 20,
        totalPages: 0,
      };

      mockTradesService.findByUser.mockResolvedValue(mockResponse);

      const req = { user: mockUser };
      const filters = { page: 1, limit: 20 };

      const result = await controller.getHistory(req, filters);

      expect(service.findByUser).toHaveBeenCalledWith(
        mockUser.id,
        expect.any(Object),
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getStatistics', () => {
    it('should return trade statistics for user', async () => {
      const mockStats = {
        totalTrades: 10,
        totalVolume: '50000.00',
        averageTradeSize: '5000.00',
        minPrice: '9.50',
        maxPrice: '11.50',
        averagePrice: '10.50',
      };

      mockTradesService.getStatistics.mockResolvedValue(mockStats);

      const req = { user: mockUser };
      const result = await controller.getStatistics(req);

      expect(service.getStatistics).toHaveBeenCalledWith(mockUser.id, undefined);
      expect(result).toEqual(mockStats);
    });

    it('should return trade statistics filtered by asset', async () => {
      const assetId = 'asset-uuid';
      const mockStats = {
        totalTrades: 5,
        totalVolume: '25000.00',
        averageTradeSize: '5000.00',
        minPrice: '10.00',
        maxPrice: '11.00',
        averagePrice: '10.50',
      };

      mockTradesService.getStatistics.mockResolvedValue(mockStats);

      const req = { user: mockUser };
      const result = await controller.getStatistics(req, assetId);

      expect(service.getStatistics).toHaveBeenCalledWith(mockUser.id, assetId);
      expect(result).toEqual(mockStats);
    });
  });
});












