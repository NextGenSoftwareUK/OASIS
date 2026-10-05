import {
  IsUUID,
  IsString,
  IsNumber,
  Min,
} from 'class-validator';
import { Type } from 'class-transformer';

export class WithdrawalDto {
  @IsUUID()
  assetId: string;

  @IsNumber()
  @Min(0.00000001) // Minimum amount
  @Type(() => Number)
  amount: number;

  @IsString()
  toAddress: string; // Destination wallet address

  @IsString()
  blockchain: string; // 'solana' | 'ethereum'
}











