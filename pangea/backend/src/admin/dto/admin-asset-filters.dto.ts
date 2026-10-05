import { IsOptional, IsString, IsEnum, IsInt, Min } from 'class-validator';
import { Type } from 'class-transformer';

export enum AssetStatus {
  DRAFT = 'draft',
  LISTED = 'listed',
  TRADING = 'trading',
  CLOSED = 'closed',
}

export class AdminAssetFiltersDto {
  @IsOptional()
  @IsEnum(AssetStatus)
  status?: AssetStatus;

  @IsOptional()
  @IsString()
  assetClass?: string;

  @IsOptional()
  @IsString()
  blockchain?: string;

  @IsOptional()
  @IsString()
  search?: string; // Search by name, symbol, assetId

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












