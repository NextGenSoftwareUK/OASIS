import { IsString, IsNumber, IsOptional, IsUrl } from 'class-validator';

export class DeployRwaTokenDto {
  @IsString()
  name: string;

  @IsString()
  symbol: string;

  @IsNumber()
  totalSupply: number;

  @IsOptional()
  @IsUrl()
  metadataUri?: string;

  @IsString()
  issuerWallet: string;

  @IsOptional()
  @IsNumber()
  decimals?: number;
}












