import {
  Entity,
  Column,
  PrimaryGeneratedColumn,
  ManyToOne,
  JoinColumn,
  CreateDateColumn,
  UpdateDateColumn,
  Index,
} from 'typeorm';
import { User } from '../../users/entities/user.entity';

@Entity('tokenized_assets')
export class TokenizedAsset {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Column({ unique: true, name: 'asset_id', length: 100 })
  @Index('idx_assets_asset_id')
  assetId: string;

  @Column({ length: 255 })
  name: string;

  @Column({ length: 20 })
  symbol: string;

  @Column('text', { nullable: true })
  description: string;

  // Asset Classification
  @Column({ name: 'asset_class', length: 50 })
  assetClass: string; // 'real_estate', 'art', 'commodities', etc.

  @Column({ name: 'asset_type', length: 50, nullable: true })
  assetType: string; // 'residential', 'commercial', etc.

  // Tokenization Details
  @Column({ name: 'total_supply', type: 'bigint' })
  totalSupply: bigint;

  @Column({ type: 'int', default: 0 })
  decimals: number;

  @Column({ name: 'token_standard', length: 20, nullable: true })
  tokenStandard: string; // 'SPL', 'ERC-721', 'ERC-1155', 'UAT'

  // Blockchain Details
  @Column({ length: 20 })
  @Index('idx_assets_blockchain')
  blockchain: string; // 'solana', 'ethereum', 'radix'

  @Column({ length: 20, default: 'devnet' })
  network: string; // 'devnet', 'testnet', 'mainnet'

  @Column({ name: 'contract_address', length: 255, nullable: true })
  @Index('idx_assets_contract_address')
  contractAddress: string;

  @Column({ name: 'mint_address', length: 255, nullable: true })
  mintAddress: string; // Solana mint address

  // Valuation
  @Column('decimal', {
    precision: 18,
    scale: 2,
    nullable: true,
    name: 'total_value_usd',
  })
  totalValueUsd: number;

  @Column('decimal', {
    precision: 18,
    scale: 2,
    nullable: true,
    name: 'price_per_token_usd',
  })
  pricePerTokenUsd: number;

  // Metadata
  @Column('text', { name: 'metadata_uri', nullable: true })
  metadataUri: string; // IPFS URI

  @Column('text', { name: 'image_uri', nullable: true })
  imageUri: string;

  @Column('text', { name: 'legal_documents_uri', nullable: true })
  legalDocumentsUri: string;

  // Status
  @Column({ length: 20, default: 'draft' })
  @Index('idx_assets_status')
  status: string; // 'draft', 'listed', 'trading', 'closed'

  // Issuer relationship
  @ManyToOne(() => User)
  @JoinColumn({ name: 'issuer_id' })
  issuer: User;

  @Column({ name: 'issuer_id', type: 'uuid' })
  issuerId: string;

  // Timestamps
  @CreateDateColumn({ name: 'created_at' })
  createdAt: Date;

  @UpdateDateColumn({ name: 'updated_at' })
  updatedAt: Date;

  @Column({ name: 'listed_at', nullable: true })
  listedAt: Date;

  // Additional JSON metadata
  @Column('jsonb', { nullable: true })
  metadata: Record<string, any>;
}












