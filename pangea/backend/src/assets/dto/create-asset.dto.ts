import {
  IsString,
  IsNotEmpty,
  IsOptional,
  IsNumber,
  IsEnum,
  IsBoolean,
  IsUrl,
  Min,
  Max,
  ValidateNested,
} from 'class-validator';
import { Type } from 'class-transformer';

export enum AssetClass {
  REAL_ESTATE = 'real_estate',
  ART = 'art',
  COMMODITIES = 'commodities',
  SECURITIES = 'securities',
  OTHER = 'other',
}

export enum Blockchain {
  SOLANA = 'solana',
  ETHEREUM = 'ethereum',
  RADIX = 'radix',
}

export enum TokenStandard {
  SPL = 'SPL',
  ERC_721 = 'ERC-721',
  ERC_1155 = 'ERC-1155',
  UAT = 'UAT',
}

export class CreateAssetDto {
  @IsString()
  @IsNotEmpty()
  name: string;

  @IsString()
  @IsNotEmpty()
  symbol: string;

  @IsString()
  @IsOptional()
  description?: string;

  @IsEnum(AssetClass)
  assetClass: AssetClass;

  @IsString()
  @IsOptional()
  assetType?: string;

  @IsNumber()
  @Min(1)
  totalSupply: number;

  @IsNumber()
  @Min(0)
  @Max(18)
  @IsOptional()
  decimals?: number;

  @IsEnum(TokenStandard)
  @IsOptional()
  tokenStandard?: TokenStandard;

  @IsEnum(Blockchain)
  blockchain: Blockchain;

  @IsString()
  @IsOptional()
  network?: string;

  @IsNumber()
  @IsOptional()
  @Min(0)
  totalValueUsd?: number;

  @IsUrl()
  @IsOptional()
  metadataUri?: string;

  @IsUrl()
  @IsOptional()
  imageUri?: string;

  @IsUrl()
  @IsOptional()
  legalDocumentsUri?: string;

  @IsString()
  @IsOptional()
  status?: string;

  @IsBoolean()
  @IsOptional()
  deployContract?: boolean;

  @IsString()
  @IsOptional()
  issuerWallet?: string;

  @IsOptional()
  metadata?: Record<string, any>;
}












