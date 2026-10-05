import {
  Controller,
  Get,
  Post,
  Put,
  Delete,
  Body,
  Param,
  Query,
  UseGuards,
  Request,
  HttpCode,
  HttpStatus,
} from '@nestjs/common';
import { AssetsService } from '../services/assets.service';
import { CreateAssetDto, UpdateAssetDto, FindAssetsDto } from '../dto';
import { JwtAuthGuard } from '../../auth/guards/jwt-auth.guard';
import { AdminGuard } from '../../auth/guards/admin.guard';
import { Public } from '../../auth/decorators/public.decorator';

@Controller('assets')
export class AssetsController {
  constructor(private readonly assetsService: AssetsService) {}

  /**
   * GET /api/assets
   * List all assets (paginated)
   * Public endpoint
   */
  @Get()
  @Public()
  async findAll(@Query() query: FindAssetsDto) {
    return this.assetsService.findAll(query);
  }

  /**
   * GET /api/assets/:assetId
   * Get asset details
   * Public endpoint
   */
  @Get(':assetId')
  @Public()
  async findOne(@Param('assetId') assetId: string) {
    return this.assetsService.findOne(assetId);
  }

  /**
   * GET /api/assets/:assetId/orders
   * Get order book for asset
   * Public endpoint
   */
  @Get(':assetId/orders')
  @Public()
  async getOrderBook(@Param('assetId') assetId: string) {
    return this.assetsService.getOrderBook(assetId);
  }

  /**
   * GET /api/assets/:assetId/trades
   * Get trade history for asset
   * Public endpoint
   */
  @Get(':assetId/trades')
  @Public()
  async getTradeHistory(
    @Param('assetId') assetId: string,
    @Query('limit') limit?: number,
  ) {
    return this.assetsService.getTradeHistory(assetId, limit);
  }

  /**
   * GET /api/assets/:assetId/price
   * Get current price for asset
   * Public endpoint
   */
  @Get(':assetId/price')
  @Public()
  async getCurrentPrice(@Param('assetId') assetId: string) {
    return this.assetsService.getCurrentPrice(assetId);
  }

  /**
   * GET /api/assets/search
   * Search assets by name, symbol, or description
   * Public endpoint
   */
  @Get('search')
  @Public()
  async search(@Query('q') query: string) {
    return this.assetsService.search(query);
  }

  /**
   * POST /api/assets
   * Create new asset (admin only)
   */
  @Post()
  @UseGuards(JwtAuthGuard, AdminGuard)
  async create(@Request() req, @Body() dto: CreateAssetDto) {
    return this.assetsService.create(dto, req.user.id);
  }

  /**
   * PUT /api/assets/:assetId
   * Update asset (admin only)
   */
  @Put(':assetId')
  @UseGuards(JwtAuthGuard, AdminGuard)
  async update(
    @Param('assetId') assetId: string,
    @Body() dto: UpdateAssetDto,
  ) {
    return this.assetsService.update(assetId, dto);
  }

  /**
   * DELETE /api/assets/:assetId
   * Delete asset (admin only)
   * Note: Soft delete - marks asset as 'closed'
   */
  @Delete(':assetId')
  @UseGuards(JwtAuthGuard, AdminGuard)
  @HttpCode(HttpStatus.NO_CONTENT)
  async delete(@Param('assetId') assetId: string) {
    await this.assetsService.delete(assetId);
  }
}












