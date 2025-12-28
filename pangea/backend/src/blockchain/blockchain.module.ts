import { Module } from '@nestjs/common';
import { BlockchainService } from './services/blockchain.service';

@Module({
  providers: [BlockchainService],
  exports: [BlockchainService], // Export for use in other modules (e.g., Order Matching, Transactions)
})
export class BlockchainModule {}












