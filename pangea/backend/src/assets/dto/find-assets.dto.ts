import { IsOptional, IsString, IsInt, Min, IsEnum } from 'class-validator';
import { Type } from 'class-transformer';
import { AssetClass } from './create-asset.dto';
import { Blockchain } from './create-asset.dto';

export class FindAssetsDto {
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

  @IsOptional()
  @IsString()
  status?: string;

  @IsOptional()
  @IsEnum(AssetClass)
  assetClass?: AssetClass;

  @IsOptional()
  @IsEnum(Blockchain)
  blockchain?: Blockchain;
}












