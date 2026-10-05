import { IsOptional, IsString, IsEnum, IsUUID } from 'class-validator';
import { Type } from 'class-transformer';

export class OrderFiltersDto {
  @IsOptional()
  @IsString()
  status?: string;

  @IsOptional()
  @IsEnum(['buy', 'sell'])
  orderType?: 'buy' | 'sell';

  @IsOptional()
  @IsUUID()
  assetId?: string;

  @IsOptional()
  @Type(() => Number)
  page?: number;

  @IsOptional()
  @Type(() => Number)
  limit?: number;
}












