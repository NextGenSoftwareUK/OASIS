import { Test, TestingModule } from '@nestjs/testing';
import { JwtService } from '@nestjs/jwt';
import { UnauthorizedException } from '@nestjs/common';
import { WebSocketService } from './websocket.service';
import { AuthService } from '../../auth/services/auth.service';
import { Trade } from '../../trades/entities/trade.entity';
import { Order } from '../entities/order.entity';
import { User } from '../../users/entities/user.entity';
import { Socket, Server } from 'socket.io';

describe('WebSocketService', () => {
  let service: WebSocketService;
  let jwtService: JwtService;
  let authService: AuthService;
  let mockServer: Partial<Server>;
  let mockSocket: Partial<Socket>;

  const mockJwtService = {
    verify: jest.fn(),
  };

  const mockAuthService = {
    validateUser: jest.fn(),
  };

  const mockUser: Partial<User> = {
    id: 'user-123',
    email: 'test@example.com',
    username: 'testuser',
    avatarId: 'avatar-123',
    role: 'user',
    isActive: true,
  };

  beforeEach(async () => {
    mockSocket = {
      id: 'socket-123',
      handshake: {
        auth: {},
        headers: {},
        query: {},
      } as any,
      join: jest.fn(),
      leave: jest.fn(),
      emit: jest.fn(),
      disconnect: jest.fn(),
      to: jest.fn().mockReturnThis(),
    };

    mockServer = {
      emit: jest.fn(),
      to: jest.fn().mockReturnThis(),
    };

    const module: TestingModule = await Test.createTestingModule({
      providers: [
        WebSocketService,
        {
          provide: JwtService,
          useValue: mockJwtService,
        },
        {
          provide: AuthService,
          useValue: mockAuthService,
        },
      ],
    }).compile();

    service = module.get<WebSocketService>(WebSocketService);
    jwtService = module.get<JwtService>(JwtService);
    authService = module.get<AuthService>(AuthService);

    // Attach mock server
    (service as any).server = mockServer;
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should be defined', () => {
    expect(service).toBeDefined();
  });

  describe('handleConnection', () => {
    it('should authenticate user and join user room on valid token', async () => {
      const token = 'valid-token';
      mockSocket.handshake.auth = { token };
      mockJwtService.verify.mockReturnValue({ sub: 'user-123' });
      mockAuthService.validateUser.mockResolvedValue(mockUser);

      await service.handleConnection(mockSocket as Socket);

      expect(jwtService.verify).toHaveBeenCalledWith(token);
      expect(authService.validateUser).toHaveBeenCalledWith('user-123');
      expect(mockSocket.join).toHaveBeenCalledWith('user:user-123');
      expect(mockSocket.emit).toHaveBeenCalledWith('connected', {
        userId: 'user-123',
        message: 'Successfully connected to trading WebSocket',
      });
      expect((mockSocket as any).user).toEqual({
        id: 'user-123',
        email: 'test@example.com',
        username: 'testuser',
        avatarId: 'avatar-123',
        role: 'user',
      });
    });

    it('should authenticate with token from Authorization header', async () => {
      const token = 'valid-token';
      mockSocket.handshake.headers = {
        authorization: `Bearer ${token}`,
      } as any;
      mockJwtService.verify.mockReturnValue({ sub: 'user-123' });
      mockAuthService.validateUser.mockResolvedValue(mockUser);

      await service.handleConnection(mockSocket as Socket);

      expect(jwtService.verify).toHaveBeenCalledWith(token);
      expect(authService.validateUser).toHaveBeenCalledWith('user-123');
    });

    it('should authenticate with token from query params', async () => {
      const token = 'valid-token';
      mockSocket.handshake.query = { token } as any;
      mockJwtService.verify.mockReturnValue({ sub: 'user-123' });
      mockAuthService.validateUser.mockResolvedValue(mockUser);

      await service.handleConnection(mockSocket as Socket);

      expect(jwtService.verify).toHaveBeenCalledWith(token);
      expect(authService.validateUser).toHaveBeenCalledWith('user-123');
    });

    it('should disconnect client without token', async () => {
      mockSocket.handshake.auth = {};
      mockSocket.handshake.headers = {};
      mockSocket.handshake.query = {};

      await service.handleConnection(mockSocket as Socket);

      expect(mockSocket.emit).toHaveBeenCalledWith('error', {
        message: 'Authentication required',
      });
      expect(mockSocket.disconnect).toHaveBeenCalled();
      expect(authService.validateUser).not.toHaveBeenCalled();
    });

    it('should disconnect client with invalid token', async () => {
      const token = 'invalid-token';
      mockSocket.handshake.auth = { token };
      mockJwtService.verify.mockImplementation(() => {
        throw new Error('Invalid token');
      });

      await service.handleConnection(mockSocket as Socket);

      expect(mockSocket.emit).toHaveBeenCalledWith('error', {
        message: 'Authentication failed',
        details: 'Invalid token',
      });
      expect(mockSocket.disconnect).toHaveBeenCalled();
      expect(authService.validateUser).not.toHaveBeenCalled();
    });

    it('should disconnect client with inactive user', async () => {
      const token = 'valid-token';
      mockSocket.handshake.auth = { token };
      mockJwtService.verify.mockReturnValue({ sub: 'user-123' });
      mockAuthService.validateUser.mockResolvedValue(null);

      await service.handleConnection(mockSocket as Socket);

      expect(mockSocket.emit).toHaveBeenCalledWith('error', {
        message: 'Invalid or inactive user',
      });
      expect(mockSocket.disconnect).toHaveBeenCalled();
      expect(mockSocket.join).not.toHaveBeenCalled();
    });
  });

  describe('handleDisconnect', () => {
    it('should log disconnect for authenticated user', () => {
      (mockSocket as any).user = mockUser;

      service.handleDisconnect(mockSocket as Socket);

      // Should not throw, just log
      expect(service).toBeDefined();
    });

    it('should log disconnect for unauthenticated user', () => {
      service.handleDisconnect(mockSocket as Socket);

      // Should not throw, just log
      expect(service).toBeDefined();
    });
  });

  describe('handleOrderBookSubscribe', () => {
    it('should subscribe authenticated user to orderbook room', () => {
      (mockSocket as any).user = mockUser;
      const data = { assetId: 'asset-123' };

      const result = service.handleOrderBookSubscribe(
        mockSocket as Socket,
        data,
      );

      expect(mockSocket.join).toHaveBeenCalledWith('orderbook:asset-123');
      expect(mockSocket.emit).toHaveBeenCalledWith('subscribed', {
        subscription: 'orderbook',
        assetId: 'asset-123',
        room: 'orderbook:asset-123',
      });
      expect(result).toEqual({
        success: true,
        subscription: 'orderbook',
        assetId: 'asset-123',
      });
    });

    it('should reject subscription for unauthenticated user', () => {
      const data = { assetId: 'asset-123' };

      const result = service.handleOrderBookSubscribe(
        mockSocket as Socket,
        data,
      );

      expect(mockSocket.emit).toHaveBeenCalledWith('error', {
        message: 'Not authenticated',
      });
      expect(result).toEqual({
        success: false,
        error: 'Not authenticated',
      });
      expect(mockSocket.join).not.toHaveBeenCalled();
    });

    it('should reject subscription without assetId', () => {
      (mockSocket as any).user = mockUser;
      const data = {} as any;

      const result = service.handleOrderBookSubscribe(
        mockSocket as Socket,
        data,
      );

      expect(mockSocket.emit).toHaveBeenCalledWith('error', {
        message: 'assetId is required',
      });
      expect(result).toEqual({
        success: false,
        error: 'assetId is required',
      });
    });
  });

  describe('handleTradesSubscribe', () => {
    it('should subscribe authenticated user to trades room', () => {
      (mockSocket as any).user = mockUser;
      const data = { assetId: 'asset-123' };

      const result = service.handleTradesSubscribe(mockSocket as Socket, data);

      expect(mockSocket.join).toHaveBeenCalledWith('trades:asset-123');
      expect(mockSocket.emit).toHaveBeenCalledWith('subscribed', {
        subscription: 'trades',
        assetId: 'asset-123',
        room: 'trades:asset-123',
      });
      expect(result).toEqual({
        success: true,
        subscription: 'trades',
        assetId: 'asset-123',
      });
    });

    it('should reject subscription for unauthenticated user', () => {
      const data = { assetId: 'asset-123' };

      const result = service.handleTradesSubscribe(mockSocket as Socket, data);

      expect(mockSocket.emit).toHaveBeenCalledWith('error', {
        message: 'Not authenticated',
      });
      expect(result).toEqual({
        success: false,
        error: 'Not authenticated',
      });
    });
  });

  describe('handleUserSubscribe', () => {
    it('should confirm user subscription', () => {
      (mockSocket as any).user = mockUser;

      const result = service.handleUserSubscribe(mockSocket as Socket);

      expect(mockSocket.emit).toHaveBeenCalledWith('subscribed', {
        subscription: 'user',
        userId: 'user-123',
        room: 'user:user-123',
      });
      expect(result).toEqual({
        success: true,
        subscription: 'user',
        userId: 'user-123',
      });
    });

    it('should reject subscription for unauthenticated user', () => {
      const result = service.handleUserSubscribe(mockSocket as Socket);

      expect(mockSocket.emit).toHaveBeenCalledWith('error', {
        message: 'Not authenticated',
      });
      expect(result).toEqual({
        success: false,
        error: 'Not authenticated',
      });
    });
  });

  describe('emitTradeExecution', () => {
    it('should emit trade execution to asset trades room and user rooms', () => {
      const trade: Partial<Trade> = {
        tradeId: 'trade-123',
        assetId: 'asset-123',
        buyer: { id: 'buyer-123' } as any,
        seller: { id: 'seller-123' } as any,
        quantity: BigInt(100),
        pricePerTokenUsd: 10.5,
        totalValueUsd: 1050,
        transactionHash: 'tx-hash-123',
        executedAt: new Date(),
      };

      const mockToChain = {
        emit: jest.fn(),
        to: jest.fn().mockReturnThis(),
      };
      (mockServer.to as jest.Mock).mockReturnValue(mockToChain);

      service.emitTradeExecution(trade as Trade);

      expect(mockServer.to).toHaveBeenCalledWith('trades:asset-123');
      expect(mockServer.to).toHaveBeenCalledWith('user:buyer-123');
      expect(mockServer.to).toHaveBeenCalledWith('user:seller-123');
      expect(mockToChain.emit).toHaveBeenCalled();
    });
  });

  describe('emitOrderUpdate', () => {
    it('should emit order update to user room', () => {
      const order: Partial<Order> = {
        orderId: 'order-123',
        assetId: 'asset-123',
        orderType: 'buy',
        orderStatus: 'filled',
        filledQuantity: BigInt(50),
        remainingQuantity: BigInt(0),
        pricePerTokenUsd: 10.5,
        user: { id: 'user-123' } as any,
      };

      const mockToChain = {
        emit: jest.fn(),
        to: jest.fn().mockReturnThis(),
      };
      (mockServer.to as jest.Mock).mockReturnValue(mockToChain);

      service.emitOrderUpdate(order as Order);

      expect(mockServer.to).toHaveBeenCalledWith('user:user-123');
      expect(mockToChain.emit).toHaveBeenCalledWith('order:updated', {
        orderId: 'order-123',
        assetId: 'asset-123',
        orderType: 'buy',
        orderStatus: 'filled',
        filledQuantity: '50',
        remainingQuantity: '0',
        pricePerTokenUsd: '10.5',
      });
    });
  });

  describe('emitOrderBookUpdate', () => {
    it('should emit order book update to asset room', () => {
      const assetId = 'asset-123';
      const orderBook = {
        bids: [{ price: 10, quantity: 100 }],
        asks: [{ price: 11, quantity: 200 }],
      };

      const mockToChain = {
        emit: jest.fn(),
        to: jest.fn().mockReturnThis(),
      };
      (mockServer.to as jest.Mock).mockReturnValue(mockToChain);

      service.emitOrderBookUpdate(assetId, orderBook);

      expect(mockServer.to).toHaveBeenCalledWith('orderbook:asset-123');
      expect(mockToChain.emit).toHaveBeenCalledWith('orderbook:update', {
        assetId,
        orderBook,
        timestamp: expect.any(String),
      });
    });
  });

  describe('emitBalanceUpdate', () => {
    it('should emit balance update to user room', () => {
      const userId = 'user-123';
      const balance = {
        assetId: 'asset-123',
        balance: '1000',
        availableBalance: '800',
        lockedBalance: '200',
      };

      const mockToChain = {
        emit: jest.fn(),
        to: jest.fn().mockReturnThis(),
      };
      (mockServer.to as jest.Mock).mockReturnValue(mockToChain);

      service.emitBalanceUpdate(userId, balance);

      expect(mockServer.to).toHaveBeenCalledWith('user:user-123');
      expect(mockToChain.emit).toHaveBeenCalledWith('balance:update', {
        userId,
        ...balance,
        timestamp: expect.any(String),
      });
    });
  });

  describe('handleOrderBookUnsubscribe', () => {
    it('should unsubscribe authenticated user from orderbook room', () => {
      (mockSocket as any).user = mockUser;
      const data = { assetId: 'asset-123' };

      const result = service.handleOrderBookUnsubscribe(
        mockSocket as Socket,
        data,
      );

      expect(mockSocket.leave).toHaveBeenCalledWith('orderbook:asset-123');
      expect(result).toEqual({
        success: true,
        unsubscribed: 'orderbook',
        assetId: 'asset-123',
      });
    });
  });

  describe('handleTradesUnsubscribe', () => {
    it('should unsubscribe authenticated user from trades room', () => {
      (mockSocket as any).user = mockUser;
      const data = { assetId: 'asset-123' };

      const result = service.handleTradesUnsubscribe(mockSocket as Socket, data);

      expect(mockSocket.leave).toHaveBeenCalledWith('trades:asset-123');
      expect(result).toEqual({
        success: true,
        unsubscribed: 'trades',
        assetId: 'asset-123',
      });
    });
  });
});











