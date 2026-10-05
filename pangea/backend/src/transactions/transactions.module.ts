import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { Transaction } from './entities/transaction.entity';
import { TokenizedAsset } from '../assets/entities/tokenized-asset.entity';
import { User } from '../users/entities/user.entity';
import { TransactionsController } from './controllers/transactions.controller';
import { TransactionsService } from './services/transactions.service';
import { VaultService } from './services/vault.service';
import { DepositMonitoringJob } from './jobs/deposit-monitoring.job';
import { OrdersModule } from '../orders/orders.module';
import { BlockchainModule } from '../blockchain/blockchain.module';
import { AssetsModule } from '../assets/assets.module';
import { SmartContractsModule } from '../smart-contracts/smart-contracts.module';

@Module({
  imports: [
    TypeOrmModule.forFeature([Transaction, TokenizedAsset, User]),
    OrdersModule, // For BalanceService
    BlockchainModule, // For BlockchainService
    AssetsModule, // For TokenizedAsset repository access
    SmartContractsModule, // For SmartContractService (used by VaultService)
  ],
  controllers: [TransactionsController],
  providers: [
    TransactionsService,
    VaultService,
    DepositMonitoringJob,
    // OasisWalletService is provided by OasisModule (global)
  ],
  exports: [TransactionsService, VaultService],
})
export class TransactionsModule {}












