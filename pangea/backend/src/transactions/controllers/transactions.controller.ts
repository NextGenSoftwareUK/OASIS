import {
  Controller,
  Get,
  Post,
  Body,
  Param,
  Query,
  UseGuards,
  Request,
} from '@nestjs/common';
import { TransactionsService } from '../services/transactions.service';
import { JwtAuthGuard } from '../../auth/guards/jwt-auth.guard';
import { AdminGuard } from '../../auth/guards/admin.guard';
import { DepositDto } from '../dto/deposit.dto';
import { WithdrawalDto } from '../dto/withdrawal.dto';
import { TransactionFiltersDto } from '../dto/transaction-filters.dto';

@Controller('transactions')
@UseGuards(JwtAuthGuard)
export class TransactionsController {
  constructor(private readonly transactionsService: TransactionsService) {}

  /**
   * GET /api/transactions
   * Get transaction history for the authenticated user
   */
  @Get()
  async findAll(
    @Request() req,
    @Query() filters: TransactionFiltersDto,
  ) {
    // Convert DTO to service interface (convert string dates to Date objects)
    const serviceFilters = {
      ...filters,
      startDate: filters.startDate ? new Date(filters.startDate) : undefined,
      endDate: filters.endDate ? new Date(filters.endDate) : undefined,
    };
    return this.transactionsService.findByUser(req.user.id, serviceFilters);
  }

  /**
   * GET /api/transactions/:txId
   * Get transaction details
   */
  @Get(':txId')
  async findOne(@Param('txId') txId: string, @Request() req) {
    return this.transactionsService.findOne(txId, req.user.id);
  }

  /**
   * POST /api/transactions/deposit
   * Initiate a deposit
   */
  @Post('deposit')
  async deposit(@Request() req, @Body() dto: DepositDto) {
    return this.transactionsService.initiateDeposit(dto, req.user.id);
  }

  /**
   * POST /api/transactions/withdraw
   * Initiate a withdrawal
   */
  @Post('withdraw')
  async withdraw(@Request() req, @Body() dto: WithdrawalDto) {
    return this.transactionsService.initiateWithdrawal(dto, req.user.id);
  }

  /**
   * GET /api/transactions/pending
   * Get pending transactions for the authenticated user
   */
  @Get('pending')
  async getPending(@Request() req) {
    return this.transactionsService.findPending(req.user.id);
  }

  /**
   * POST /api/transactions/:txId/confirm
   * Confirm a transaction (admin only)
   */
  @Post(':txId/confirm')
  @UseGuards(AdminGuard)
  async confirm(@Param('txId') txId: string) {
    return this.transactionsService.confirmTransaction(txId);
  }
}











