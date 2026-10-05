import {
  Controller,
  Post,
  Get,
  Body,
  UseGuards,
  HttpCode,
  HttpStatus,
} from '@nestjs/common';
import { JwtAuthGuard } from '../../auth/guards/jwt-auth.guard';
import { AdminGuard } from '../../auth/guards/admin.guard';
import { SmartContractService } from '../services/smart-contract.service';
import { DeployRwaTokenDto } from '../dto/deploy-rwa-token.dto';

@Controller('smart-contracts')
@UseGuards(JwtAuthGuard, AdminGuard)
export class SmartContractsController {
  constructor(private readonly smartContractService: SmartContractService) {}

  @Post('deploy-rwa-token')
  @HttpCode(HttpStatus.OK)
  async deployRwaToken(@Body() dto: DeployRwaTokenDto) {
    const result = await this.smartContractService.generateRwaToken({
      name: dto.name,
      symbol: dto.symbol,
      totalSupply: dto.totalSupply,
      metadataUri: dto.metadataUri || '',
      issuerWallet: dto.issuerWallet,
      decimals: dto.decimals,
    });

    return {
      success: true,
      contractAddress: result.contractAddress,
      transactionHash: result.transactionHash,
      programId: result.programId,
      message: 'RWA Token contract deployed successfully',
    };
  }

  @Post('deploy-order-book')
  @HttpCode(HttpStatus.OK)
  async deployOrderBook() {
    const result = await this.smartContractService.deployOrderBook();

    return {
      success: true,
      contractAddress: result.contractAddress,
      transactionHash: result.transactionHash,
      programId: result.programId,
      message: 'Order Book contract deployed successfully',
    };
  }

  @Post('deploy-trade-execution')
  @HttpCode(HttpStatus.OK)
  async deployTradeExecution() {
    const result = await this.smartContractService.deployTradeExecution();

    return {
      success: true,
      contractAddress: result.contractAddress,
      transactionHash: result.transactionHash,
      programId: result.programId,
      message: 'Trade Execution contract deployed successfully',
    };
  }

  @Post('deploy-vault')
  @HttpCode(HttpStatus.OK)
  async deployVault() {
    const result = await this.smartContractService.deployVault();

    return {
      success: true,
      contractAddress: result.contractAddress,
      transactionHash: result.transactionHash,
      programId: result.programId,
      message: 'Vault contract deployed successfully',
    };
  }

  @Get('cache-stats')
  async getCacheStats() {
    const stats = await this.smartContractService.getCacheStats();
    return stats;
  }
}












