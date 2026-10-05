import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AdminController } from './controllers/admin.controller';
import { AdminService } from './services/admin.service';
import { User } from '../users/entities/user.entity';
import { TokenizedAsset } from '../assets/entities/tokenized-asset.entity';
import { Order } from '../orders/entities/order.entity';
import { Trade } from '../trades/entities/trade.entity';
import { Transaction } from '../transactions/entities/transaction.entity';
@Module({
  imports: [
    TypeOrmModule.forFeature([
      User,
      TokenizedAsset,
      Order,
      Trade,
      Transaction,
    ]),
    // Guards are imported directly, no need for AuthModule import
  ],
  controllers: [AdminController],
  providers: [AdminService],
  exports: [AdminService],
})
export class AdminModule {}












