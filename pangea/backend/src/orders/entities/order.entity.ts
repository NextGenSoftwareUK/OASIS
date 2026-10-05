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
import { TokenizedAsset } from '../../assets/entities/tokenized-asset.entity';

@Entity('orders')
export class Order {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Column({ unique: true, name: 'order_id' })
  orderId: string;

  @ManyToOne(() => User)
  @JoinColumn({ name: 'user_id' })
  @Index('idx_orders_user_id')
  user: User;

  @Column({ name: 'user_id', type: 'uuid' })
  userId: string;

  @ManyToOne(() => TokenizedAsset)
  @JoinColumn({ name: 'asset_id' })
  @Index('idx_orders_asset_id')
  asset: TokenizedAsset;

  @Column({ name: 'asset_id', type: 'uuid' })
  assetId: string;

  @Column({ name: 'order_type' })
  orderType: 'buy' | 'sell';

  @Column({ name: 'order_status', default: 'pending' })
  @Index('idx_orders_status')
  @Index('idx_orders_type_status')
  orderStatus: string;

  @Column('decimal', {
    precision: 18,
    scale: 2,
    name: 'price_per_token_usd',
  })
  pricePerTokenUsd: number;

  @Column('bigint')
  quantity: bigint;

  @Column('bigint', { default: 0, name: 'filled_quantity' })
  filledQuantity: bigint;

  @Column('bigint', { name: 'remaining_quantity' })
  remainingQuantity: bigint;

  @Column('decimal', {
    precision: 18,
    scale: 2,
    name: 'total_value_usd',
  })
  totalValueUsd: number;

  @Column({ nullable: true, name: 'expires_at' })
  expiresAt: Date;

  @Column()
  blockchain: string;

  @Column({ nullable: true, name: 'transaction_hash' })
  transactionHash: string;

  @Column({ default: false, name: 'is_market_order' })
  isMarketOrder: boolean;

  @Column({ default: true, name: 'is_limit_order' })
  isLimitOrder: boolean;

  @CreateDateColumn({ name: 'created_at' })
  @Index('idx_orders_created_at')
  createdAt: Date;

  @UpdateDateColumn({ name: 'updated_at' })
  updatedAt: Date;

  @Column({ nullable: true, name: 'filled_at' })
  filledAt: Date;
}












