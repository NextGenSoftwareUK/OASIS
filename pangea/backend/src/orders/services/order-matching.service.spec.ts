import { Test, TestingModule } from '@nestjs/testing';
import { getRepositoryToken } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { OrderMatchingService } from './order-matching.service';
import { Order } from '../entities/order.entity';
import { TradesService } from '../../trades/trades.service';
import { BalanceService } from './balance.service';
import { BlockchainService } from '../../blockchain/services/blockchain.service';
import { WebSocketService } from './websocket.service';
import { User } from '../../users/entities/user.entity';

describe('OrderMatchingService', () => {
  let service: OrderMatchingService;
  let orderRepository: Repository<Order>;
  let tradesService: TradesService;
  let balanceService: BalanceService;
  let blockchainService: BlockchainService;
  let webSocketService: WebSocketService;

  const mockOrderRepository = {
    create: jest.fn(),
    save: jest.fn(),
    find: jest.fn(),
    findOne: jest.fn(),
    createQueryBuilder: jest.fn(),
  };

  const mockTradesService = {
    create: jest.fn(),
  };

  const mockBalanceService = {
    getBalance: jest.fn(),
    lockBalance: jest.fn(),
    unlockBalance: jest.fn(),
    transfer: jest.fn(),
    getPaymentTokenBalance: jest.fn(),
  };

  const mockBlockchainService = {
    executeTrade: jest.fn(),
  };

  const mockWebSocketService = {
    emitTradeEvent: jest.fn(),
    emitOrderUpdate: jest.fn(),
  };

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      providers: [
        OrderMatchingService,
        {
          provide: getRepositoryToken(Order),
          useValue: mockOrderRepository,
        },
        {
          provide: TradesService,
          useValue: mockTradesService,
        },
        {
          provide: BalanceService,
          useValue: mockBalanceService,
        },
        {
          provide: BlockchainService,
          useValue: mockBlockchainService,
        },
        {
          provide: WebSocketService,
          useValue: mockWebSocketService,
        },
      ],
    }).compile();

    service = module.get<OrderMatchingService>(OrderMatchingService);
    orderRepository = module.get<Repository<Order>>(
      getRepositoryToken(Order),
    );
    tradesService = module.get<TradesService>(TradesService);
    balanceService = module.get<BalanceService>(BalanceService);
    blockchainService = module.get<BlockchainService>(BlockchainService);
    webSocketService = module.get<WebSocketService>(WebSocketService);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should be defined', () => {
    expect(service).toBeDefined();
  });

  describe('findMatchingOrders', () => {
    it('should find matching sell orders for a buy order', async () => {
      const buyOrder: Partial<Order> = {
        id: 'buy-order-1',
        orderId: 'BUY-001',
        orderType: 'buy',
        assetId: 'asset-1',
        pricePerTokenUsd: 100,
        remainingQuantity: BigInt(10),
      };

      const mockQueryBuilder = {
        where: jest.fn().mockReturnThis(),
        andWhere: jest.fn().mockReturnThis(),
        orderBy: jest.fn().mockReturnThis(),
        addOrderBy: jest.fn().mockReturnThis(),
        getMany: jest.fn().mockResolvedValue([]),
      };

      mockOrderRepository.createQueryBuilder.mockReturnValue(mockQueryBuilder);

      await service.findMatchingOrders(buyOrder as Order);

      expect(mockOrderRepository.createQueryBuilder).toHaveBeenCalledWith(
        'order',
      );
      expect(mockQueryBuilder.where).toHaveBeenCalledWith(
        'order.assetId = :assetId',
        { assetId: 'asset-1' },
      );
      expect(mockQueryBuilder.andWhere).toHaveBeenCalledWith(
        'order.orderType = :type',
        { type: 'sell' },
      );
      expect(mockQueryBuilder.orderBy).toHaveBeenCalledWith(
        'order.pricePerTokenUsd',
        'ASC',
      );
    });

    it('should find matching buy orders for a sell order', async () => {
      const sellOrder: Partial<Order> = {
        id: 'sell-order-1',
        orderId: 'SELL-001',
        orderType: 'sell',
        assetId: 'asset-1',
        pricePerTokenUsd: 100,
        remainingQuantity: BigInt(10),
      };

      const mockQueryBuilder = {
        where: jest.fn().mockReturnThis(),
        andWhere: jest.fn().mockReturnThis(),
        orderBy: jest.fn().mockReturnThis(),
        addOrderBy: jest.fn().mockReturnThis(),
        getMany: jest.fn().mockResolvedValue([]),
      };

      mockOrderRepository.createQueryBuilder.mockReturnValue(mockQueryBuilder);

      await service.findMatchingOrders(sellOrder as Order);

      expect(mockQueryBuilder.andWhere).toHaveBeenCalledWith(
        'order.orderType = :type',
        { type: 'buy' },
      );
      expect(mockQueryBuilder.orderBy).toHaveBeenCalledWith(
        'order.pricePerTokenUsd',
        'DESC',
      );
    });
  });

  describe('processOrder', () => {
    it('should set order to open when no matches found', async () => {
      const order: Partial<Order> = {
        id: 'order-1',
        orderId: 'ORD-001',
        orderType: 'buy',
        assetId: 'asset-1',
        orderStatus: 'pending',
        remainingQuantity: BigInt(10),
        filledQuantity: BigInt(0),
        user: { id: 'user-1' } as User,
      };

      const mockQueryBuilder = {
        where: jest.fn().mockReturnThis(),
        andWhere: jest.fn().mockReturnThis(),
        orderBy: jest.fn().mockReturnThis(),
        addOrderBy: jest.fn().mockReturnThis(),
        getMany: jest.fn().mockResolvedValue([]),
      };

      mockOrderRepository.createQueryBuilder.mockReturnValue(mockQueryBuilder);
      mockOrderRepository.save.mockResolvedValue(order);

      await service.processOrder(order as Order);

      expect(order.orderStatus).toBe('open');
      expect(mockOrderRepository.save).toHaveBeenCalledWith(order);
      expect(mockWebSocketService.emitOrderUpdate).toHaveBeenCalledWith(order);
    });

    it('should process order and execute matches', async () => {
      const buyOrder: Partial<Order> = {
        id: 'buy-order-1',
        orderId: 'BUY-001',
        orderType: 'buy',
        assetId: 'asset-1',
        orderStatus: 'pending',
        pricePerTokenUsd: 100,
        remainingQuantity: BigInt(10),
        filledQuantity: BigInt(0),
        blockchain: 'solana',
        user: { id: 'buyer-1' } as User,
      };

      const sellOrder: Partial<Order> = {
        id: 'sell-order-1',
        orderId: 'SELL-001',
        orderType: 'sell',
        assetId: 'asset-1',
        orderStatus: 'open',
        pricePerTokenUsd: 95,
        remainingQuantity: BigInt(5),
        filledQuantity: BigInt(0),
        blockchain: 'solana',
        user: { id: 'seller-1' } as User,
      };

      const mockQueryBuilder = {
        where: jest.fn().mockReturnThis(),
        andWhere: jest.fn().mockReturnThis(),
        orderBy: jest.fn().mockReturnThis(),
        addOrderBy: jest.fn().mockReturnThis(),
        getMany: jest.fn().mockResolvedValue([sellOrder]),
      };

      mockOrderRepository.createQueryBuilder.mockReturnValue(mockQueryBuilder);
      mockOrderRepository.save.mockResolvedValue(buyOrder);
      mockBalanceService.getBalance.mockResolvedValue({
        availableBalance: BigInt(10),
      });
      mockBalanceService.getPaymentTokenBalance.mockResolvedValue(
        BigInt(1000000),
      );
      mockBlockchainService.executeTrade.mockResolvedValue('tx-hash-123');
      mockTradesService.create.mockResolvedValue({
        id: 'trade-1',
        tradeId: 'TRD-001',
      });

      await service.processOrder(buyOrder as Order);

      expect(mockBlockchainService.executeTrade).toHaveBeenCalled();
      expect(mockTradesService.create).toHaveBeenCalled();
      expect(mockBalanceService.transfer).toHaveBeenCalled();
    });
  });

  describe('validateBalances', () => {
    it('should throw error if seller has insufficient balance', async () => {
      const buyOrder: Partial<Order> = {
        id: 'buy-order-1',
        orderType: 'buy',
        assetId: 'asset-1',
        blockchain: 'solana',
        user: { id: 'buyer-1' } as User,
      };

      const sellOrder: Partial<Order> = {
        id: 'sell-order-1',
        orderType: 'sell',
        assetId: 'asset-1',
        pricePerTokenUsd: 100,
        user: { id: 'seller-1' } as User,
      };

      mockBalanceService.getBalance.mockResolvedValue({
        availableBalance: BigInt(3), // Less than required 5
      });

      await expect(
        service.validateBalances(
          buyOrder as Order,
          sellOrder as Order,
          5,
        ),
      ).rejects.toThrow('Seller has insufficient balance');
    });

    it('should throw error if buyer has insufficient payment balance', async () => {
      const buyOrder: Partial<Order> = {
        id: 'buy-order-1',
        orderType: 'buy',
        assetId: 'asset-1',
        blockchain: 'solana',
        user: { id: 'buyer-1' } as User,
      };

      const sellOrder: Partial<Order> = {
        id: 'sell-order-1',
        orderType: 'sell',
        assetId: 'asset-1',
        pricePerTokenUsd: 100,
        user: { id: 'seller-1' } as User,
      };

      mockBalanceService.getBalance.mockResolvedValue({
        availableBalance: BigInt(10),
      });
      mockBalanceService.getPaymentTokenBalance.mockResolvedValue(
        BigInt(100), // Insufficient for 5 * 100 = 500
      );

      await expect(
        service.validateBalances(
          buyOrder as Order,
          sellOrder as Order,
          5,
        ),
      ).rejects.toThrow('Buyer has insufficient payment balance');
    });
  });
});












