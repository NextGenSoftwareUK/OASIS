import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AssetsController } from './controllers/assets.controller';
import { AssetsService } from './services/assets.service';
import { TokenizedAsset } from './entities/tokenized-asset.entity';
import { SmartContractsModule } from '../smart-contracts/smart-contracts.module';

@Module({
  imports: [
    TypeOrmModule.forFeature([TokenizedAsset]),
    SmartContractsModule,
  ],
  controllers: [AssetsController],
  providers: [AssetsService],
  exports: [AssetsService],
})
export class AssetsModule {}












