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
import { Order } from '../../orders/entities/order.entity';

@Entity('trades')
export class Trade {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Column({ unique: true, name: 'trade_id' })
  tradeId: string;

  @ManyToOne(() => User)
  @JoinColumn({ name: 'buyer_id' })
  @Index('idx_trades_buyer_id')
  buyer: User;

  @Column({ name: 'buyer_id', type: 'uuid' })
  buyerId: string;

  @ManyToOne(() => User)
  @JoinColumn({ name: 'seller_id' })
  @Index('idx_trades_seller_id')
  seller: User;

  @Column({ name: 'seller_id', type: 'uuid' })
  sellerId: string;

  @ManyToOne(() => TokenizedAsset)
  @JoinColumn({ name: 'asset_id' })
  @Index('idx_trades_asset_id')
  asset: TokenizedAsset;

  @Column({ name: 'asset_id', type: 'uuid' })
  assetId: string;

  @ManyToOne(() => Order, { nullable: true })
  @JoinColumn({ name: 'buy_order_id' })
  buyOrder: Order;

  @Column({ name: 'buy_order_id', type: 'uuid', nullable: true })
  buyOrderId: string;

  @ManyToOne(() => Order, { nullable: true })
  @JoinColumn({ name: 'sell_order_id' })
  sellOrder: Order;

  @Column({ name: 'sell_order_id', type: 'uuid', nullable: true })
  sellOrderId: string;

  @Column('bigint')
  quantity: bigint;

  @Column('decimal', { precision: 18, scale: 2, name: 'price_per_token_usd' })
  pricePerTokenUsd: number;

  @Column('decimal', { precision: 18, scale: 2, name: 'total_value_usd' })
  totalValueUsd: number;

  @Column('decimal', {
    precision: 18,
    scale: 2,
    default: 0,
    name: 'platform_fee_usd',
  })
  platformFeeUsd: number;

  @Column('decimal', {
    precision: 5,
    scale: 2,
    default: 0,
    name: 'platform_fee_percentage',
  })
  platformFeePercentage: number;

  @Column()
  blockchain: string;

  @Column({ name: 'transaction_hash' })
  @Index('idx_trades_transaction_hash')
  transactionHash: string;

  @Column('bigint', { nullable: true, name: 'block_number' })
  blockNumber: bigint;

  @CreateDateColumn({ name: 'executed_at' })
  @Index('idx_trades_executed_at')
  executedAt: Date;

  @Column({ nullable: true, name: 'confirmed_at' })
  confirmedAt: Date;

  @Column({ default: 'pending' })
  status: string;

  @Column({ default: 'pending', name: 'settlement_status' })
  settlementStatus: string;
}












