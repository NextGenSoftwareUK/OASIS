import {
  Controller,
  Get,
  Post,
  Put,
  Delete,
  Param,
  Query,
  Body,
  UseGuards,
  Logger,
} from '@nestjs/common';
import { AdminService } from '../services/admin.service';
import { JwtAuthGuard } from '../../auth/guards/jwt-auth.guard';
import { AdminGuard } from '../../auth/guards/admin.guard';
import {
  AdminUserFiltersDto,
  UpdateUserDto,
  UpdateKycStatusDto,
  AdminAssetFiltersDto,
  AdminOrderFiltersDto,
  AdminTradeFiltersDto,
  AdminTransactionFiltersDto,
  AnalyticsFiltersDto,
} from '../dto';
import { CreateAssetDto } from '../../assets/dto/create-asset.dto';
import { UpdateAssetDto } from '../../assets/dto/update-asset.dto';

@Controller('admin')
@UseGuards(JwtAuthGuard, AdminGuard)
export class AdminController {
  private readonly logger = new Logger(AdminController.name);

  constructor(private readonly adminService: AdminService) {}

  // ==================== USER MANAGEMENT ====================

  /**
   * Get all users with filters
   * GET /admin/users
   */
  @Get('users')
  async getUsers(@Query() filters: AdminUserFiltersDto) {
    return this.adminService.getUsers(filters);
  }

  /**
   * Get user by ID
   * GET /admin/users/:userId
   */
  @Get('users/:userId')
  async getUser(@Param('userId') userId: string) {
    return this.adminService.getUser(userId);
  }

  /**
   * Update user information
   * PUT /admin/users/:userId
   */
  @Put('users/:userId')
  async updateUser(
    @Param('userId') userId: string,
    @Body() dto: UpdateUserDto,
  ) {
    return this.adminService.updateUser(userId, dto);
  }

  /**
   * Update user KYC status
   * PUT /admin/users/:userId/kyc
   */
  @Put('users/:userId/kyc')
  async updateKycStatus(
    @Param('userId') userId: string,
    @Body() dto: UpdateKycStatusDto,
  ) {
    return this.adminService.updateUserKycStatus(userId, dto);
  }

  /**
   * Get user activity logs
   * GET /admin/users/:userId/activity
   */
  @Get('users/:userId/activity')
  async getUserActivityLogs(@Param('userId') userId: string) {
    return this.adminService.getUserActivityLogs(userId);
  }

  // ==================== ASSET MANAGEMENT ====================

  /**
   * Get all assets with filters
   * GET /admin/assets
   */
  @Get('assets')
  async getAssets(@Query() filters: AdminAssetFiltersDto) {
    return this.adminService.getAssets(filters);
  }

  /**
   * Create new asset (admin)
   * POST /admin/assets
   */
  @Post('assets')
  async createAsset(
    @Body() dto: CreateAssetDto,
    @Query('issuerId') issuerId?: string,
  ) {
    return this.adminService.createAsset(dto, issuerId);
  }

  /**
   * Update asset (admin)
   * PUT /admin/assets/:assetId
   */
  @Put('assets/:assetId')
  async updateAsset(
    @Param('assetId') assetId: string,
    @Body() dto: UpdateAssetDto,
  ) {
    return this.adminService.updateAsset(assetId, dto);
  }

  /**
   * Delete asset (admin - soft delete)
   * DELETE /admin/assets/:assetId
   */
  @Delete('assets/:assetId')
  async deleteAsset(@Param('assetId') assetId: string) {
    await this.adminService.deleteAsset(assetId);
    return { message: 'Asset deleted successfully' };
  }

  /**
   * Approve asset listing
   * POST /admin/assets/:assetId/approve
   */
  @Post('assets/:assetId/approve')
  async approveAssetListing(@Param('assetId') assetId: string) {
    return this.adminService.approveAssetListing(assetId);
  }

  /**
   * Get asset statistics
   * GET /admin/assets/:assetId/statistics
   */
  @Get('assets/:assetId/statistics')
  async getAssetStatistics(@Param('assetId') assetId: string) {
    return this.adminService.getAssetStatistics(assetId);
  }

  // ==================== ORDER MANAGEMENT ====================

  /**
   * Get all orders with filters
   * GET /admin/orders
   */
  @Get('orders')
  async getOrders(@Query() filters: AdminOrderFiltersDto) {
    return this.adminService.getOrders(filters);
  }

  /**
   * Cancel order (emergency admin action)
   * DELETE /admin/orders/:orderId
   */
  @Delete('orders/:orderId')
  async cancelOrder(@Param('orderId') orderId: string) {
    return this.adminService.cancelOrder(orderId);
  }

  /**
   * Get order statistics
   * GET /admin/orders/statistics
   */
  @Get('orders/statistics')
  async getOrderStatistics() {
    return this.adminService.getOrderStatistics();
  }

  // ==================== TRADE MANAGEMENT ====================

  /**
   * Get all trades with filters
   * GET /admin/trades
   */
  @Get('trades')
  async getTrades(@Query() filters: AdminTradeFiltersDto) {
    return this.adminService.getTrades(filters);
  }

  /**
   * Get trade statistics
   * GET /admin/trades/statistics
   */
  @Get('trades/statistics')
  async getTradeStatistics() {
    return this.adminService.getTradeStatistics();
  }

  // ==================== TRANSACTION MANAGEMENT ====================

  /**
   * Get all transactions with filters
   * GET /admin/transactions
   */
  @Get('transactions')
  async getTransactions(@Query() filters: AdminTransactionFiltersDto) {
    return this.adminService.getTransactions(filters);
  }

  /**
   * Approve withdrawal (if manual approval is required)
   * POST /admin/transactions/:transactionId/approve
   */
  @Post('transactions/:transactionId/approve')
  async approveWithdrawal(@Param('transactionId') transactionId: string) {
    return this.adminService.approveWithdrawal(transactionId);
  }

  // ==================== PLATFORM STATISTICS ====================

  /**
   * Get platform statistics
   * GET /admin/stats
   */
  @Get('stats')
  async getPlatformStatistics() {
    return this.adminService.getPlatformStatistics();
  }

  // ==================== ANALYTICS ====================

  /**
   * Get analytics data
   * GET /admin/analytics
   */
  @Get('analytics')
  async getAnalytics(@Query() filters: AnalyticsFiltersDto) {
    return this.adminService.getAnalytics(filters);
  }
}












