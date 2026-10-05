import { Module } from '@nestjs/common';
import { WalletController } from './wallet.controller';
import { WalletConnectionService } from '../services/wallet-connection.service';
import { BalanceSyncService } from '../services/balance-sync.service';
import { AuthModule } from '../auth/auth.module';
// OasisWalletService is provided by OasisModule (global)

@Module({
  imports: [AuthModule], // For JwtAuthGuard
  controllers: [WalletController],
  providers: [WalletConnectionService, BalanceSyncService],
  exports: [WalletConnectionService, BalanceSyncService],
})
export class WalletModule {}












