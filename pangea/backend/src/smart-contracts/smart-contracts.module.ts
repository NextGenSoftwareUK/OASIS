import { Module } from '@nestjs/common';
import { SmartContractService } from './services/smart-contract.service';
import { SmartContractsController } from './controllers/smart-contracts.controller';

@Module({
  controllers: [SmartContractsController],
  providers: [SmartContractService],
  exports: [SmartContractService],
})
export class SmartContractsModule {}












