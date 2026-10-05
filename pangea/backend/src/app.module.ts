import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { TypeOrmModule } from '@nestjs/typeorm';
import { ScheduleModule } from '@nestjs/schedule';
import { AppController } from './app.controller';
import { AppService } from './app.service';
import { DatabaseConfig } from './config/database.config';
import { RedisModule } from './config/redis.module';
import { MigrationRunnerService } from './config/migration-runner.service';
import { TradesModule } from './trades/trades.module';
import { OrdersModule } from './orders/orders.module';
import { AssetsModule } from './assets/assets.module';
import { SmartContractsModule } from './smart-contracts/smart-contracts.module';
import { TransactionsModule } from './transactions/transactions.module';
import { AdminModule } from './admin/admin.module';
import { WalletModule } from './wallet/wallet.module';
import { OasisModule } from './services/oasis.module';

@Module({
  imports: [
    // Configuration module
    ConfigModule.forRoot({
      isGlobal: true,
      envFilePath: '.env',
    }),

    // Database module
    TypeOrmModule.forRootAsync({
      useClass: DatabaseConfig,
    }),

    // Redis module
    RedisModule,

    // Schedule module for cron jobs
    ScheduleModule.forRoot(),

    // OASIS services module (global)
    OasisModule,

    // Feature modules
    TradesModule,
    OrdersModule,
    AssetsModule,
    SmartContractsModule,
    TransactionsModule,
    AdminModule,
    WalletModule,
  ],
  controllers: [AppController],
  providers: [AppService, MigrationRunnerService],
})
export class AppModule {}












