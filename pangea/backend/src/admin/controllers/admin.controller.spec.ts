import { Test, TestingModule } from '@nestjs/testing';
import { getRepositoryToken } from '@nestjs/typeorm';
import { Repository, DataSource } from 'typeorm';
import { AdminController } from './admin.controller';
import { AdminService } from '../services/admin.service';
import { User } from '../../users/entities/user.entity';
import { TokenizedAsset } from '../../assets/entities/tokenized-asset.entity';
import { Order } from '../../orders/entities/order.entity';
import { Trade } from '../../trades/entities/trade.entity';
import { Transaction } from '../../transactions/entities/transaction.entity';
import { JwtAuthGuard } from '../../auth/guards/jwt-auth.guard';
import { AdminGuard } from '../../auth/guards/admin.guard';

describe('AdminController', () => {
  let controller: AdminController;
  let service: AdminService;

  const mockAdminService = {
    getUsers: jest.fn(),
    getUser: jest.fn(),
    updateUser: jest.fn(),
    updateUserKycStatus: jest.fn(),
    getUserActivityLogs: jest.fn(),
    getAssets: jest.fn(),
    createAsset: jest.fn(),
    updateAsset: jest.fn(),
    deleteAsset: jest.fn(),
    approveAssetListing: jest.fn(),
    getAssetStatistics: jest.fn(),
    getOrders: jest.fn(),
    cancelOrder: jest.fn(),
    getOrderStatistics: jest.fn(),
    getTrades: jest.fn(),
    getTradeStatistics: jest.fn(),
    getTransactions: jest.fn(),
    approveWithdrawal: jest.fn(),
    getPlatformStatistics: jest.fn(),
    getAnalytics: jest.fn(),
  };

  const mockUserRepository = {};
  const mockAssetRepository = {};
  const mockOrderRepository = {};
  const mockTradeRepository = {};
  const mockTransactionRepository = {};
  const mockDataSource = {};

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      controllers: [AdminController],
      providers: [
        {
          provide: AdminService,
          useValue: mockAdminService,
        },
        {
          provide: getRepositoryToken(User),
          useValue: mockUserRepository,
        },
        {
          provide: getRepositoryToken(TokenizedAsset),
          useValue: mockAssetRepository,
        },
        {
          provide: getRepositoryToken(Order),
          useValue: mockOrderRepository,
        },
        {
          provide: getRepositoryToken(Trade),
          useValue: mockTradeRepository,
        },
        {
          provide: getRepositoryToken(Transaction),
          useValue: mockTransactionRepository,
        },
        {
          provide: DataSource,
          useValue: mockDataSource,
        },
      ],
    })
      .overrideGuard(JwtAuthGuard)
      .useValue({ canActivate: () => true })
      .overrideGuard(AdminGuard)
      .useValue({ canActivate: () => true })
      .compile();

    controller = module.get<AdminController>(AdminController);
    service = module.get<AdminService>(AdminService);

    jest.clearAllMocks();
  });

  it('should be defined', () => {
    expect(controller).toBeDefined();
  });

  describe('getUsers', () => {
    it('should return users from service', async () => {
      const filters = { page: 1, limit: 20 };
      const expectedResult = {
        items: [],
        total: 0,
        page: 1,
        limit: 20,
        totalPages: 0,
      };
      mockAdminService.getUsers.mockResolvedValue(expectedResult);

      const result = await controller.getUsers(filters);

      expect(result).toEqual(expectedResult);
      expect(mockAdminService.getUsers).toHaveBeenCalledWith(filters);
    });
  });

  describe('getUser', () => {
    it('should return user by id', async () => {
      const userId = '123';
      const expectedUser = { id: userId, email: 'test@test.com' };
      mockAdminService.getUser.mockResolvedValue(expectedUser);

      const result = await controller.getUser(userId);

      expect(result).toEqual(expectedUser);
      expect(mockAdminService.getUser).toHaveBeenCalledWith(userId);
    });
  });

  describe('updateUser', () => {
    it('should update user', async () => {
      const userId = '123';
      const updateDto = { firstName: 'New Name' };
      const updatedUser = { id: userId, ...updateDto };
      mockAdminService.updateUser.mockResolvedValue(updatedUser);

      const result = await controller.updateUser(userId, updateDto);

      expect(result).toEqual(updatedUser);
      expect(mockAdminService.updateUser).toHaveBeenCalledWith(userId, updateDto);
    });
  });

  describe('updateKycStatus', () => {
    it('should update KYC status', async () => {
      const userId = '123';
      const dto = { status: 'approved' as any };
      const updatedUser = { id: userId, kycStatus: 'approved' };
      mockAdminService.updateUserKycStatus.mockResolvedValue(updatedUser);

      const result = await controller.updateKycStatus(userId, dto);

      expect(result).toEqual(updatedUser);
      expect(mockAdminService.updateUserKycStatus).toHaveBeenCalledWith(
        userId,
        dto,
      );
    });
  });

  describe('getAssets', () => {
    it('should return assets from service', async () => {
      const filters = { page: 1, limit: 20 };
      const expectedResult = {
        items: [],
        total: 0,
        page: 1,
        limit: 20,
        totalPages: 0,
      };
      mockAdminService.getAssets.mockResolvedValue(expectedResult);

      const result = await controller.getAssets(filters);

      expect(result).toEqual(expectedResult);
      expect(mockAdminService.getAssets).toHaveBeenCalledWith(filters);
    });
  });

  describe('createAsset', () => {
    it('should create asset', async () => {
      const dto = { name: 'Test Asset', symbol: 'TEST' } as any;
      const createdAsset = { id: '123', ...dto };
      mockAdminService.createAsset.mockResolvedValue(createdAsset);

      const result = await controller.createAsset(dto);

      expect(result).toEqual(createdAsset);
      expect(mockAdminService.createAsset).toHaveBeenCalledWith(dto, undefined);
    });
  });

  describe('cancelOrder', () => {
    it('should cancel order', async () => {
      const orderId = '123';
      const cancelledOrder = { id: orderId, orderStatus: 'cancelled' };
      mockAdminService.cancelOrder.mockResolvedValue(cancelledOrder);

      const result = await controller.cancelOrder(orderId);

      expect(result).toEqual(cancelledOrder);
      expect(mockAdminService.cancelOrder).toHaveBeenCalledWith(orderId);
    });
  });

  describe('getPlatformStatistics', () => {
    it('should return platform statistics', async () => {
      const expectedStats = {
        totalUsers: 100,
        activeUsers: 80,
        totalAssets: 50,
        totalOrders: 200,
        totalTrades: 150,
        totalVolume: 50000,
        totalRevenue: 500,
      };
      mockAdminService.getPlatformStatistics.mockResolvedValue(expectedStats);

      const result = await controller.getPlatformStatistics();

      expect(result).toEqual(expectedStats);
      expect(mockAdminService.getPlatformStatistics).toHaveBeenCalled();
    });
  });

  describe('getAnalytics', () => {
    it('should return analytics data', async () => {
      const filters = { period: 'month' as any };
      const expectedAnalytics = {
        period: 'month',
        dateRange: { start: new Date(), end: new Date() },
        trades: { count: 10, volume: 1000, fees: 10 },
      };
      mockAdminService.getAnalytics.mockResolvedValue(expectedAnalytics);

      const result = await controller.getAnalytics(filters);

      expect(result).toEqual(expectedAnalytics);
      expect(mockAdminService.getAnalytics).toHaveBeenCalledWith(filters);
    });
  });
});












