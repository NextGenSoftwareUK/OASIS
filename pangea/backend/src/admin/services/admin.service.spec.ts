import { Test, TestingModule } from '@nestjs/testing';
import { getRepositoryToken } from '@nestjs/typeorm';
import { Repository, DataSource } from 'typeorm';
import { AdminService } from './admin.service';
import { User } from '../../users/entities/user.entity';
import { TokenizedAsset } from '../../assets/entities/tokenized-asset.entity';
import { Order } from '../../orders/entities/order.entity';
import { Trade } from '../../trades/entities/trade.entity';
import { Transaction } from '../../transactions/entities/transaction.entity';
import { NotFoundException, BadRequestException } from '@nestjs/common';

describe('AdminService', () => {
  let service: AdminService;
  let userRepository: Repository<User>;
  let assetRepository: Repository<TokenizedAsset>;
  let orderRepository: Repository<Order>;
  let tradeRepository: Repository<Trade>;
  let transactionRepository: Repository<Transaction>;

  const mockUserRepository = {
    createQueryBuilder: jest.fn(),
    findOne: jest.fn(),
    save: jest.fn(),
    count: jest.fn(),
  };

  const mockAssetRepository = {
    createQueryBuilder: jest.fn(),
    findOne: jest.fn(),
    create: jest.fn(),
    save: jest.fn(),
    count: jest.fn(),
  };

  const mockOrderRepository = {
    createQueryBuilder: jest.fn(),
    findOne: jest.fn(),
    save: jest.fn(),
    count: jest.fn(),
  };

  const mockTradeRepository = {
    createQueryBuilder: jest.fn(),
    count: jest.fn(),
  };

  const mockTransactionRepository = {
    createQueryBuilder: jest.fn(),
    findOne: jest.fn(),
    save: jest.fn(),
    count: jest.fn(),
  };

  const mockDataSource = {
    getRepository: jest.fn(),
  };

  const mockQueryBuilder = {
    leftJoinAndSelect: jest.fn().mockReturnThis(),
    where: jest.fn().mockReturnThis(),
    andWhere: jest.fn().mockReturnThis(),
    orderBy: jest.fn().mockReturnThis(),
    skip: jest.fn().mockReturnThis(),
    take: jest.fn().mockReturnThis(),
    getManyAndCount: jest.fn(),
    getCount: jest.fn(),
    select: jest.fn().mockReturnThis(),
    getRawOne: jest.fn(),
    clone: jest.fn().mockReturnThis(),
  };

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      providers: [
        AdminService,
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
    }).compile();

    service = module.get<AdminService>(AdminService);
    userRepository = module.get<Repository<User>>(getRepositoryToken(User));
    assetRepository = module.get<Repository<TokenizedAsset>>(
      getRepositoryToken(TokenizedAsset),
    );
    orderRepository = module.get<Repository<Order>>(getRepositoryToken(Order));
    tradeRepository = module.get<Repository<Trade>>(getRepositoryToken(Trade));
    transactionRepository = module.get<Repository<Transaction>>(
      getRepositoryToken(Transaction),
    );

    // Reset mocks
    jest.clearAllMocks();
    mockUserRepository.createQueryBuilder.mockReturnValue(mockQueryBuilder);
    mockAssetRepository.createQueryBuilder.mockReturnValue(mockQueryBuilder);
    mockOrderRepository.createQueryBuilder.mockReturnValue(mockQueryBuilder);
    mockTradeRepository.createQueryBuilder.mockReturnValue(mockQueryBuilder);
    mockTransactionRepository.createQueryBuilder.mockReturnValue(
      mockQueryBuilder,
    );
  });

  it('should be defined', () => {
    expect(service).toBeDefined();
  });

  describe('getUsers', () => {
    it('should return paginated users', async () => {
      const filters = { page: 1, limit: 20 };
      const mockUsers = [{ id: '1', email: 'test@test.com' }];
      mockQueryBuilder.getManyAndCount.mockResolvedValue([mockUsers, 1]);

      const result = await service.getUsers(filters);

      expect(result).toEqual({
        items: mockUsers,
        total: 1,
        page: 1,
        limit: 20,
        totalPages: 1,
      });
      expect(mockUserRepository.createQueryBuilder).toHaveBeenCalled();
    });

    it('should filter users by kycStatus', async () => {
      const filters = { kycStatus: 'pending' as any };
      mockQueryBuilder.getManyAndCount.mockResolvedValue([[], 0]);

      await service.getUsers(filters);

      expect(mockQueryBuilder.andWhere).toHaveBeenCalledWith(
        'user.kycStatus = :kycStatus',
        { kycStatus: 'pending' },
      );
    });
  });

  describe('getUser', () => {
    it('should return user by id', async () => {
      const userId = '123';
      const mockUser = { id: userId, email: 'test@test.com' };
      mockUserRepository.findOne.mockResolvedValue(mockUser);

      const result = await service.getUser(userId);

      expect(result).toEqual(mockUser);
      expect(mockUserRepository.findOne).toHaveBeenCalledWith({
        where: { id: userId },
      });
    });

    it('should throw NotFoundException if user not found', async () => {
      mockUserRepository.findOne.mockResolvedValue(null);

      await expect(service.getUser('123')).rejects.toThrow(NotFoundException);
    });
  });

  describe('updateUser', () => {
    it('should update user information', async () => {
      const userId = '123';
      const mockUser = { id: userId, email: 'test@test.com', firstName: 'Old' };
      const updateDto = { firstName: 'New' };
      const updatedUser = { ...mockUser, ...updateDto };

      mockUserRepository.findOne.mockResolvedValue(mockUser);
      mockUserRepository.save.mockResolvedValue(updatedUser);

      const result = await service.updateUser(userId, updateDto);

      expect(result).toEqual(updatedUser);
      expect(mockUserRepository.save).toHaveBeenCalled();
    });
  });

  describe('updateUserKycStatus', () => {
    it('should update user KYC status', async () => {
      const userId = '123';
      const mockUser = { id: userId, kycStatus: 'pending' };
      const dto = { status: 'approved' as any };
      const updatedUser = { ...mockUser, kycStatus: 'approved' };

      mockUserRepository.findOne.mockResolvedValue(mockUser);
      mockUserRepository.save.mockResolvedValue(updatedUser);

      const result = await service.updateUserKycStatus(userId, dto);

      expect(result.kycStatus).toBe('approved');
      expect(mockUserRepository.save).toHaveBeenCalled();
    });
  });

  describe('getAssets', () => {
    it('should return paginated assets', async () => {
      const filters = { page: 1, limit: 20 };
      const mockAssets = [{ id: '1', name: 'Test Asset' }];
      mockQueryBuilder.getManyAndCount.mockResolvedValue([mockAssets, 1]);

      const result = await service.getAssets(filters);

      expect(result.items).toEqual(mockAssets);
      expect(mockAssetRepository.createQueryBuilder).toHaveBeenCalled();
    });
  });

  describe('getAssetStatistics', () => {
    it('should return asset statistics', async () => {
      const assetId = '123';
      const mockAsset = { id: assetId, name: 'Test Asset' };
      const mockVolume = { total: '1000' };
      const mockAvgPrice = { avg: '10.5' };

      mockAssetRepository.findOne.mockResolvedValue(mockAsset);
      mockTradeRepository.count.mockResolvedValue(5);
      mockQueryBuilder.getRawOne
        .mockResolvedValueOnce(mockVolume)
        .mockResolvedValueOnce(mockAvgPrice);

      const result = await service.getAssetStatistics(assetId);

      expect(result.asset).toEqual(mockAsset);
      expect(result.totalTrades).toBe(5);
      expect(result.totalVolume).toBe(1000);
      expect(result.averagePrice).toBe(10.5);
    });

    it('should throw NotFoundException if asset not found', async () => {
      mockAssetRepository.findOne.mockResolvedValue(null);

      await expect(service.getAssetStatistics('123')).rejects.toThrow(
        NotFoundException,
      );
    });
  });

  describe('cancelOrder', () => {
    it('should cancel an order', async () => {
      const orderId = '123';
      const mockOrder = {
        id: orderId,
        orderStatus: 'open',
        remainingQuantity: BigInt(100),
      };
      const cancelledOrder = {
        ...mockOrder,
        orderStatus: 'cancelled',
        remainingQuantity: BigInt(0),
      };

      mockOrderRepository.findOne.mockResolvedValue(mockOrder);
      mockOrderRepository.save.mockResolvedValue(cancelledOrder);

      const result = await service.cancelOrder(orderId);

      expect(result.orderStatus).toBe('cancelled');
      expect(mockOrderRepository.save).toHaveBeenCalled();
    });

    it('should throw BadRequestException if order is already filled', async () => {
      const orderId = '123';
      const mockOrder = { id: orderId, orderStatus: 'filled' };

      mockOrderRepository.findOne.mockResolvedValue(mockOrder);

      await expect(service.cancelOrder(orderId)).rejects.toThrow(
        BadRequestException,
      );
    });
  });

  describe('getPlatformStatistics', () => {
    it('should return platform statistics', async () => {
      mockUserRepository.count
        .mockResolvedValueOnce(100) // totalUsers
        .mockResolvedValueOnce(80); // activeUsers
      mockAssetRepository.count.mockResolvedValue(50);
      mockOrderRepository.count.mockResolvedValue(200);
      mockTradeRepository.count.mockResolvedValue(150);
      mockQueryBuilder.getRawOne
        .mockResolvedValueOnce({ total: '50000' }) // totalVolume
        .mockResolvedValueOnce({ total: '500' }); // totalRevenue

      const result = await service.getPlatformStatistics();

      expect(result.totalUsers).toBe(100);
      expect(result.activeUsers).toBe(80);
      expect(result.totalAssets).toBe(50);
      expect(result.totalOrders).toBe(200);
      expect(result.totalTrades).toBe(150);
      expect(result.totalVolume).toBe(50000);
      expect(result.totalRevenue).toBe(500);
    });
  });

  describe('approveWithdrawal', () => {
    it('should approve a withdrawal transaction', async () => {
      const transactionId = '123';
      const mockTransaction = {
        id: transactionId,
        transactionType: 'withdrawal',
        status: 'pending',
      };
      const approvedTransaction = {
        ...mockTransaction,
        status: 'processing',
      };

      mockTransactionRepository.findOne.mockResolvedValue(mockTransaction);
      mockTransactionRepository.save.mockResolvedValue(approvedTransaction);

      const result = await service.approveWithdrawal(transactionId);

      expect(result.status).toBe('processing');
      expect(mockTransactionRepository.save).toHaveBeenCalled();
    });

    it('should throw BadRequestException if transaction is not a withdrawal', async () => {
      const transactionId = '123';
      const mockTransaction = {
        id: transactionId,
        transactionType: 'deposit',
        status: 'pending',
      };

      mockTransactionRepository.findOne.mockResolvedValue(mockTransaction);

      await expect(service.approveWithdrawal(transactionId)).rejects.toThrow(
        BadRequestException,
      );
    });
  });
});












