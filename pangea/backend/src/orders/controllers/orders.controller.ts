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
} from '@nestjs/common';
import { OrdersService } from '../services/orders.service';
import { JwtAuthGuard } from '../../auth/guards/jwt-auth.guard';
import { CreateOrderDto, UpdateOrderDto, OrderFiltersDto } from '../dto';
import { CurrentUser } from '../../auth/decorators/current-user.decorator';

@Controller('orders')
@UseGuards(JwtAuthGuard)
export class OrdersController {
  constructor(private readonly ordersService: OrdersService) {}

  /**
   * Get open orders for the user
   * GET /api/orders/open
   */
  @Get('open')
  async getOpenOrders(@CurrentUser() user: { id: string }) {
    return this.ordersService.getOpenOrders(user.id);
  }

  /**
   * Get order history for the user
   * GET /api/orders/history
   */
  @Get('history')
  async getHistory(
    @CurrentUser() user: { id: string },
    @Query() filters: OrderFiltersDto,
  ) {
    return this.ordersService.getHistory(user.id, filters);
  }

  /**
   * Get orders for a specific asset
   * GET /api/orders/asset/:assetId
   */
  @Get('asset/:assetId')
  async getOrdersByAsset(
    @Param('assetId') assetId: string,
    @Query() filters: { page?: number; limit?: number },
  ) {
    return this.ordersService.findByAsset(assetId, filters);
  }

  /**
   * Get user's orders with optional filters
   * GET /api/orders
   */
  @Get()
  async findAll(
    @CurrentUser() user: { id: string },
    @Query() filters: OrderFiltersDto,
  ) {
    return this.ordersService.findByUser(user.id, filters);
  }

  /**
   * Create a new order
   * POST /api/orders
   */
  @Post()
  async create(
    @Body() createOrderDto: CreateOrderDto,
    @CurrentUser() user: { id: string },
  ) {
    return this.ordersService.create(createOrderDto, user.id);
  }

  /**
   * Get order details by orderId
   * GET /api/orders/:orderId
   */
  @Get(':orderId')
  async findOne(
    @Param('orderId') orderId: string,
    @CurrentUser() user: { id: string },
  ) {
    return this.ordersService.findOne(orderId, user.id);
  }

  /**
   * Update an order
   * PUT /api/orders/:orderId
   */
  @Put(':orderId')
  async update(
    @Param('orderId') orderId: string,
    @Body() updateOrderDto: UpdateOrderDto,
    @CurrentUser() user: { id: string },
  ) {
    return this.ordersService.update(orderId, updateOrderDto, user.id);
  }

  /**
   * Cancel an order
   * DELETE /api/orders/:orderId
   */
  @Delete(':orderId')
  async cancel(
    @Param('orderId') orderId: string,
    @CurrentUser() user: { id: string },
  ) {
    return this.ordersService.cancel(orderId, user.id);
  }
}












