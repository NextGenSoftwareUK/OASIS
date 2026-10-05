import {
  Injectable,
  Logger,
  UnauthorizedException,
  OnModuleInit,
} from '@nestjs/common';
import {
  WebSocketGateway,
  WebSocketServer,
  SubscribeMessage,
  OnGatewayConnection,
  OnGatewayDisconnect,
  ConnectedSocket,
  MessageBody,
} from '@nestjs/websockets';
import { Server, Socket } from 'socket.io';
import { JwtService } from '@nestjs/jwt';
import { Trade } from '../../trades/entities/trade.entity';
import { Order } from '../entities/order.entity';
import { AuthService } from '../../auth/services/auth.service';

@Injectable()
@WebSocketGateway({
  cors: {
    origin: '*', // Configure appropriately for production
  },
  namespace: '/trading',
})
export class WebSocketService
  implements OnGatewayConnection, OnGatewayDisconnect, OnModuleInit
{
  @WebSocketServer()
  server: Server;

  private readonly logger = new Logger(WebSocketService.name);

  constructor(
    private jwtService: JwtService,
    private authService: AuthService,
  ) {}

  onModuleInit() {
    this.logger.log('WebSocket Gateway initialized on /trading namespace');
  }

  /**
   * Handle client connection with JWT authentication
   */
  async handleConnection(client: Socket) {
    try {
      // Extract token from handshake auth or query params
      const token =
        client.handshake.auth?.token ||
        client.handshake.headers?.authorization?.replace('Bearer ', '') ||
        client.handshake.query?.token;

      if (!token) {
        this.logger.warn(
          `Client ${client.id} attempted to connect without token`,
        );
        client.emit('error', { message: 'Authentication required' });
        client.disconnect();
        return;
      }

      // Verify JWT token
      const payload = this.jwtService.verify(token);

      // Validate user exists and is active
      const user = await this.authService.validateUser(payload.sub);
      if (!user || !user.isActive) {
        this.logger.warn(
          `Client ${client.id} connected with invalid or inactive user`,
        );
        client.emit('error', { message: 'Invalid or inactive user' });
        client.disconnect();
        return;
      }

      // Attach user info to socket
      (client as any).user = {
        id: user.id,
        email: user.email,
        username: user.username,
        avatarId: user.avatarId,
        role: user.role,
      };

      // Join user-specific room
      client.join(`user:${user.id}`);

      this.logger.log(
        `Client ${client.id} connected as user ${user.id} (${user.email})`,
      );

      // Send connection confirmation
      client.emit('connected', {
        userId: user.id,
        message: 'Successfully connected to trading WebSocket',
      });
    } catch (error) {
      this.logger.error(
        `Connection error for client ${client.id}: ${error.message}`,
      );
      client.emit('error', {
        message: 'Authentication failed',
        details: error.message,
      });
      client.disconnect();
    }
  }

  /**
   * Handle client disconnection
   */
  handleDisconnect(client: Socket) {
    const user = (client as any).user;
    if (user) {
      this.logger.log(
        `Client ${client.id} disconnected (user ${user.id}) - automatically unsubscribed from all rooms`,
      );
    } else {
      this.logger.log(`Client ${client.id} disconnected`);
    }
  }

  /**
   * Subscribe to order book updates for a specific asset
   */
  @SubscribeMessage('subscribe:orderbook')
  handleOrderBookSubscribe(
    @ConnectedSocket() client: Socket,
    @MessageBody() data: { assetId: string },
  ) {
    const user = (client as any).user;
    if (!user) {
      client.emit('error', { message: 'Not authenticated' });
      return { success: false, error: 'Not authenticated' };
    }

    if (!data || !data.assetId) {
      client.emit('error', { message: 'assetId is required' });
      return { success: false, error: 'assetId is required' };
    }

    const room = `orderbook:${data.assetId}`;
    client.join(room);

    this.logger.log(
      `User ${user.id} subscribed to orderbook for asset ${data.assetId}`,
    );

    client.emit('subscribed', {
      subscription: 'orderbook',
      assetId: data.assetId,
      room,
    });

    return { success: true, subscription: 'orderbook', assetId: data.assetId };
  }

  /**
   * Subscribe to trade feed for a specific asset
   */
  @SubscribeMessage('subscribe:trades')
  handleTradesSubscribe(
    @ConnectedSocket() client: Socket,
    @MessageBody() data: { assetId: string },
  ) {
    const user = (client as any).user;
    if (!user) {
      client.emit('error', { message: 'Not authenticated' });
      return { success: false, error: 'Not authenticated' };
    }

    if (!data || !data.assetId) {
      client.emit('error', { message: 'assetId is required' });
      return { success: false, error: 'assetId is required' };
    }

    const room = `trades:${data.assetId}`;
    client.join(room);

    this.logger.log(
      `User ${user.id} subscribed to trades for asset ${data.assetId}`,
    );

    client.emit('subscribed', {
      subscription: 'trades',
      assetId: data.assetId,
      room,
    });

    return { success: true, subscription: 'trades', assetId: data.assetId };
  }

  /**
   * Subscribe to user-specific events (orders, balance updates)
   */
  @SubscribeMessage('subscribe:user')
  handleUserSubscribe(@ConnectedSocket() client: Socket) {
    const user = (client as any).user;
    if (!user) {
      client.emit('error', { message: 'Not authenticated' });
      return { success: false, error: 'Not authenticated' };
    }

    // User is already in their user room from handleConnection
    const room = `user:${user.id}`;

    this.logger.log(`User ${user.id} subscribed to user events`);

    client.emit('subscribed', {
      subscription: 'user',
      userId: user.id,
      room,
    });

    return { success: true, subscription: 'user', userId: user.id };
  }

  /**
   * Unsubscribe from order book
   */
  @SubscribeMessage('unsubscribe:orderbook')
  handleOrderBookUnsubscribe(
    @ConnectedSocket() client: Socket,
    @MessageBody() data: { assetId: string },
  ) {
    const user = (client as any).user;
    if (!user) {
      return { success: false, error: 'Not authenticated' };
    }

    if (!data || !data.assetId) {
      return { success: false, error: 'assetId is required' };
    }

    const room = `orderbook:${data.assetId}`;
    client.leave(room);

    this.logger.log(
      `User ${user.id} unsubscribed from orderbook for asset ${data.assetId}`,
    );

    return { success: true, unsubscribed: 'orderbook', assetId: data.assetId };
  }

  /**
   * Unsubscribe from trades
   */
  @SubscribeMessage('unsubscribe:trades')
  handleTradesUnsubscribe(
    @ConnectedSocket() client: Socket,
    @MessageBody() data: { assetId: string },
  ) {
    const user = (client as any).user;
    if (!user) {
      return { success: false, error: 'Not authenticated' };
    }

    if (!data || !data.assetId) {
      return { success: false, error: 'assetId is required' };
    }

    const room = `trades:${data.assetId}`;
    client.leave(room);

    this.logger.log(
      `User ${user.id} unsubscribed from trades for asset ${data.assetId}`,
    );

    return { success: true, unsubscribed: 'trades', assetId: data.assetId };
  }

  /**
   * Emit trade execution event
   */
  emitTradeExecution(trade: Trade): void {
    this.logger.log(`Emitting trade event for trade ${trade.tradeId}`);

    const tradeData = {
      tradeId: trade.tradeId,
      assetId: trade.assetId,
      buyerId: trade.buyer.id,
      sellerId: trade.seller.id,
      quantity: trade.quantity.toString(),
      pricePerTokenUsd: trade.pricePerTokenUsd.toString(),
      totalValueUsd: trade.totalValueUsd.toString(),
      transactionHash: trade.transactionHash,
      executedAt: trade.executedAt,
    };

    // Emit to asset-specific trade room
    this.server.to(`trades:${trade.assetId}`).emit('trade:executed', tradeData);

    // Also emit to specific user rooms with side information
    this.server.to(`user:${trade.buyer.id}`).emit('trade:executed', {
      ...tradeData,
      side: 'buy',
    });

    this.server.to(`user:${trade.seller.id}`).emit('trade:executed', {
      ...tradeData,
      side: 'sell',
    });
  }

  /**
   * Alias for backwards compatibility
   */
  emitTradeEvent(trade: Trade): void {
    this.emitTradeExecution(trade);
  }

  /**
   * Emit order update event
   */
  emitOrderUpdate(order: Order): void {
    this.logger.log(`Emitting order update for order ${order.orderId}`);
    this.server.emit('order:updated', {
      orderId: order.orderId,
      assetId: order.assetId,
      orderType: order.orderType,
      orderStatus: order.orderStatus,
      filledQuantity: order.filledQuantity.toString(),
      remainingQuantity: order.remainingQuantity.toString(),
      pricePerTokenUsd: order.pricePerTokenUsd.toString(),
    });

    // Also emit to specific user room
    this.server.to(`user:${order.user.id}`).emit('order:updated', {
      orderId: order.orderId,
      assetId: order.assetId,
      orderType: order.orderType,
      orderStatus: order.orderStatus,
      filledQuantity: order.filledQuantity.toString(),
      remainingQuantity: order.remainingQuantity.toString(),
      pricePerTokenUsd: order.pricePerTokenUsd.toString(),
    });
  }

  /**
   * Emit order book update event
   */
  emitOrderBookUpdate(assetId: string, orderBook: any): void {
    this.logger.log(`Emitting order book update for asset ${assetId}`);
    this.server.to(`orderbook:${assetId}`).emit('orderbook:update', {
      assetId,
      orderBook,
      timestamp: new Date().toISOString(),
    });
  }

  /**
   * Emit balance update event for a user
   */
  emitBalanceUpdate(userId: string, balance: any): void {
    this.logger.log(`Emitting balance update for user ${userId}`);
    this.server.to(`user:${userId}`).emit('balance:update', {
      userId,
      ...balance,
      timestamp: new Date().toISOString(),
    });
  }

  /**
   * Emit price update event for an asset
   */
  emitPriceUpdate(assetId: string, priceData: any): void {
    this.logger.log(`Emitting price update for asset ${assetId}`);
    // Emit to orderbook subscribers (they also care about prices)
    this.server.to(`orderbook:${assetId}`).emit('price:update', {
      assetId,
      ...priceData,
      timestamp: new Date().toISOString(),
    });
  }
}












