import {
  IsUUID,
  IsString,
  IsNumber,
  Min,
  IsOptional,
} from 'class-validator';
import { Type } from 'class-transformer';

export class DepositDto {
  @IsUUID()
  assetId: string;

  @IsNumber()
  @Min(0.00000001) // Minimum amount
  @Type(() => Number)
  amount: number;

  @IsString()
  blockchain: string; // 'solana' | 'ethereum'

  @IsOptional()
  @IsString()
  fromAddress?: string; // User's wallet address (optional, can be inferred from user)
}











