import { IsOptional, IsString, IsUUID, IsDateString, IsInt, Min } from 'class-validator';
import { Type } from 'class-transformer';

export class TradeFiltersDto {
  @IsOptional()
  @IsUUID()
  assetId?: string;

  @IsOptional()
  @IsString()
  status?: string;

  @IsOptional()
  @IsString()
  settlementStatus?: string;

  @IsOptional()
  @IsDateString()
  startDate?: string;

  @IsOptional()
  @IsDateString()
  endDate?: string;

  @IsOptional()
  @Type(() => Number)
  @IsInt()
  @Min(1)
  page?: number = 1;

  @IsOptional()
  @Type(() => Number)
  @IsInt()
  @Min(1)
  limit?: number = 20;
}

export interface TradeFilters {
  assetId?: string;
  status?: string;
  settlementStatus?: string;
  startDate?: Date;
  endDate?: Date;
  page?: number;
  limit?: number;
}












