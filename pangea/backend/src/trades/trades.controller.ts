import {
  Controller,
  Get,
  Param,
  Query,
  UseGuards,
  Request,
  ParseUUIDPipe,
} from '@nestjs/common';
import { TradesService } from './trades.service';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { TradeFiltersDto } from './dto/trade-filters.dto';
import { TradeResponseDto, TradeListResponseDto, TradeStatisticsDto } from './dto';

@Controller('trades')
@UseGuards(JwtAuthGuard)
export class TradesController {
  constructor(private readonly tradesService: TradesService) {}

  /**
   * GET /api/trades
   * Get all trades for the authenticated user
   */
  @Get()
  async findAll(
    @Request() req,
    @Query() filters: TradeFiltersDto,
  ): Promise<TradeListResponseDto> {
    const tradeFilters = this.mapFiltersDtoToFilters(filters);
    return this.tradesService.findByUser(req.user.id, tradeFilters);
  }

  /**
   * GET /api/trades/history
   * Get trade history with filters (alias for GET /api/trades)
   */
  @Get('history')
  async getHistory(
    @Request() req,
    @Query() filters: TradeFiltersDto,
  ): Promise<TradeListResponseDto> {
    const tradeFilters = this.mapFiltersDtoToFilters(filters);
    return this.tradesService.findByUser(req.user.id, tradeFilters);
  }

  /**
   * GET /api/trades/statistics
   * Get trade statistics for the authenticated user
   */
  @Get('statistics')
  async getStatistics(
    @Request() req,
    @Query('assetId') assetId?: string,
  ): Promise<TradeStatisticsDto> {
    return this.tradesService.getStatistics(req.user.id, assetId);
  }

  /**
   * GET /api/trades/asset/:assetId
   * Get trades for a specific asset
   */
  @Get('asset/:assetId')
  async findByAsset(
    @Param('assetId', ParseUUIDPipe) assetId: string,
    @Query() filters: TradeFiltersDto,
  ): Promise<TradeListResponseDto> {
    const tradeFilters = this.mapFiltersDtoToFilters(filters);
    return this.tradesService.findByAsset(assetId, tradeFilters);
  }

  /**
   * GET /api/trades/:tradeId
   * Get trade details by trade ID
   * Note: This must be last to avoid conflicts with routes like 'history', 'statistics', etc.
   */
  @Get(':tradeId')
  async findOne(
    @Param('tradeId') tradeId: string,
    @Request() req,
  ): Promise<TradeResponseDto> {
    const trade = await this.tradesService.findOne(tradeId, req.user.id);
    return this.tradesService.mapTradeToResponse(trade);
  }

  /**
   * Map TradeFiltersDto to TradeFilters interface
   */
  private mapFiltersDtoToFilters(filters: TradeFiltersDto) {
    return {
      assetId: filters.assetId,
      status: filters.status,
      settlementStatus: filters.settlementStatus,
      startDate: filters.startDate ? new Date(filters.startDate) : undefined,
      endDate: filters.endDate ? new Date(filters.endDate) : undefined,
      page: filters.page,
      limit: filters.limit,
    };
  }
}












