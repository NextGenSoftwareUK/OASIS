import { IsString, IsUUID, IsNumber, IsOptional, IsDateString } from 'class-validator';

export class CreateTradeDto {
  @IsUUID()
  buyerId: string;

  @IsUUID()
  sellerId: string;

  @IsUUID()
  assetId: string;

  @IsUUID()
  @IsOptional()
  buyOrderId?: string;

  @IsUUID()
  @IsOptional()
  sellOrderId?: string;

  @IsNumber()
  quantity: number;

  @IsNumber()
  pricePerTokenUsd: number;

  @IsNumber()
  totalValueUsd: number;

  @IsNumber()
  @IsOptional()
  platformFeeUsd?: number;

  @IsNumber()
  @IsOptional()
  platformFeePercentage?: number;

  @IsString()
  blockchain: string;

  @IsString()
  transactionHash: string;

  @IsNumber()
  @IsOptional()
  blockNumber?: number;

  @IsDateString()
  @IsOptional()
  confirmedAt?: string;

  @IsString()
  @IsOptional()
  status?: string;

  @IsString()
  @IsOptional()
  settlementStatus?: string;
}












