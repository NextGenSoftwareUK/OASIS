import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { JwtModule } from '@nestjs/jwt';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { Order } from './entities/order.entity';
import { OrderBookSnapshot } from './entities/order-book-snapshot.entity';
import { UserBalance } from '../users/entities/user-balance.entity';
import { OrderMatchingService } from './services/order-matching.service';
import { BalanceService } from './services/balance.service';
import { WebSocketService } from './services/websocket.service';
import { OrderMatchingJob } from './jobs/order-matching.job';
import { OrdersService } from './services/orders.service';
import { OrdersController } from './controllers/orders.controller';
import { TradesModule } from '../trades/trades.module';
import { BlockchainModule } from '../blockchain/blockchain.module';
import { AssetsModule } from '../assets/assets.module';
import { AuthModule } from '../auth/auth.module';

@Module({
  imports: [
    TypeOrmModule.forFeature([Order, OrderBookSnapshot, UserBalance]),
    TradesModule,
    BlockchainModule,
    AssetsModule,
    AuthModule,
    JwtModule.registerAsync({
      imports: [ConfigModule],
      inject: [ConfigService],
      useFactory: (configService: ConfigService) => ({
        secret:
          configService.get<string>('JWT_SECRET') ||
          'YourSuperSecretKeyForJWTTokenGenerationThatShouldBeAtLeast32Characters',
        signOptions: { expiresIn: '7d' },
      }),
    }),
  ],
  controllers: [OrdersController],
  providers: [
    OrdersService,
    OrderMatchingService,
    BalanceService,
    WebSocketService,
    OrderMatchingJob,
  ],
  exports: [
    OrdersService,
    OrderMatchingService,
    BalanceService,
    WebSocketService,
  ],
})
export class OrdersModule {}












