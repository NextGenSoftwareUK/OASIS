import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { TradesController } from './trades.controller';
import { TradesService } from './trades.service';
import { Trade } from './entities/trade.entity';

@Module({
  imports: [TypeOrmModule.forFeature([Trade])],
  controllers: [TradesController],
  providers: [TradesService],
  exports: [TradesService], // Export for use in other modules (e.g., Order Matching)
})
export class TradesModule {}












