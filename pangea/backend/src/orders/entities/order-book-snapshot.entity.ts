import {
  Entity,
  Column,
  PrimaryGeneratedColumn,
  ManyToOne,
  JoinColumn,
  CreateDateColumn,
  Index,
} from 'typeorm';
import { TokenizedAsset } from '../../assets/entities/tokenized-asset.entity';

@Entity('order_book_snapshots')
export class OrderBookSnapshot {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @ManyToOne(() => TokenizedAsset)
  @JoinColumn({ name: 'asset_id' })
  @Index('idx_orderbook_asset_id')
  asset: TokenizedAsset;

  @Column({ name: 'asset_id', type: 'uuid' })
  assetId: string;

  // Best Bid/Ask
  @Column('decimal', {
    precision: 18,
    scale: 2,
    nullable: true,
    name: 'best_bid_price',
  })
  bestBidPrice: number;

  @Column({ name: 'best_bid_quantity', type: 'bigint', nullable: true })
  bestBidQuantity: bigint;

  @Column('decimal', {
    precision: 18,
    scale: 2,
    nullable: true,
    name: 'best_ask_price',
  })
  bestAskPrice: number;

  @Column({ name: 'best_ask_quantity', type: 'bigint', nullable: true })
  bestAskQuantity: bigint;

  // Market Stats
  @Column('decimal', {
    precision: 18,
    scale: 2,
    nullable: true,
    name: 'last_trade_price',
  })
  lastTradePrice: number;

  @Column({ name: 'volume_24h', type: 'bigint', nullable: true })
  volume24h: bigint;

  @Column('decimal', {
    precision: 18,
    scale: 2,
    nullable: true,
    name: 'high_24h',
  })
  high24h: number;

  @Column('decimal', {
    precision: 18,
    scale: 2,
    nullable: true,
    name: 'low_24h',
  })
  low24h: number;

  // Timestamp
  @CreateDateColumn({ name: 'snapshot_at' })
  @Index('idx_orderbook_snapshot_at')
  snapshotAt: Date;
}












