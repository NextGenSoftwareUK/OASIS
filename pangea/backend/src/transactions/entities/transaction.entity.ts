import {
  Entity,
  Column,
  PrimaryGeneratedColumn,
  ManyToOne,
  JoinColumn,
  CreateDateColumn,
  Index,
} from 'typeorm';
import { User } from '../../users/entities/user.entity';
import { TokenizedAsset } from '../../assets/entities/tokenized-asset.entity';

@Entity('transactions')
export class Transaction {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Column({ unique: true, name: 'transaction_id', length: 100 })
  transactionId: string;

  @ManyToOne(() => User)
  @JoinColumn({ name: 'user_id' })
  @Index('idx_transactions_user_id')
  user: User;

  @Column({ name: 'user_id', type: 'uuid' })
  userId: string;

  @ManyToOne(() => TokenizedAsset)
  @JoinColumn({ name: 'asset_id' })
  asset: TokenizedAsset;

  @Column({ name: 'asset_id', type: 'uuid' })
  assetId: string;

  // Transaction Details
  @Column({ name: 'transaction_type', length: 20 })
  @Index('idx_transactions_type')
  transactionType: string; // 'deposit', 'withdrawal'

  @Column({ length: 20, default: 'pending' })
  @Index('idx_transactions_status')
  status: string; // 'pending', 'processing', 'completed', 'failed'

  // Amounts
  @Column({ type: 'bigint' })
  amount: bigint;

  @Column('decimal', {
    precision: 18,
    scale: 2,
    nullable: true,
    name: 'amount_usd',
  })
  amountUsd: number;

  // Blockchain
  @Column({ length: 20 })
  blockchain: string;

  @Column({ name: 'transaction_hash', length: 255, nullable: true })
  @Index('idx_transactions_transaction_hash')
  transactionHash: string;

  @Column({ name: 'from_address', length: 255, nullable: true })
  fromAddress: string;

  @Column({ name: 'to_address', length: 255, nullable: true })
  toAddress: string;

  @Column({ name: 'block_number', type: 'bigint', nullable: true })
  blockNumber: bigint;

  // Timestamps
  @CreateDateColumn({ name: 'created_at' })
  createdAt: Date;

  @Column({ name: 'confirmed_at', nullable: true })
  confirmedAt: Date;

  // Metadata
  @Column('jsonb', { nullable: true })
  metadata: Record<string, any>;
}












