import { IsString, IsNotEmpty, IsEnum } from 'class-validator';

export enum BlockchainType {
  SOLANA = 'solana',
  ETHEREUM = 'ethereum',
}

export class ConnectWalletDto {
  @IsString()
  @IsNotEmpty()
  walletAddress: string;

  @IsString()
  @IsNotEmpty()
  signature: string;

  @IsString()
  @IsNotEmpty()
  message: string;

  @IsEnum(BlockchainType)
  @IsNotEmpty()
  blockchain: BlockchainType;
}












