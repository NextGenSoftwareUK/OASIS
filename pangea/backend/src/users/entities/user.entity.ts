import {
  Entity,
  Column,
  PrimaryGeneratedColumn,
  CreateDateColumn,
  UpdateDateColumn,
  Index,
} from 'typeorm';

@Entity('users')
export class User {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Column({ unique: true })
  @Index('idx_users_email')
  email: string;

  @Column({ name: 'password_hash', nullable: true })
  passwordHash: string; // Nullable if using OASIS auth only

  @Column({ nullable: true })
  username: string;

  @Column({ nullable: true })
  firstName: string;

  @Column({ nullable: true })
  lastName: string;

  // Link to OASIS Avatar
  @Column({ name: 'avatar_id', unique: true, nullable: true })
  avatarId: string;

  // Wallet addresses (from OASIS Wallet API)
  @Column({ name: 'wallet_address_solana', nullable: true })
  walletAddressSolana: string;

  @Column({ name: 'wallet_address_ethereum', nullable: true })
  walletAddressEthereum: string;

  // User role
  @Column({ default: 'user' })
  role: string;

  // KYC status
  @Column({ name: 'kyc_status', default: 'pending' })
  kycStatus: string;

  // Timestamps
  @CreateDateColumn({ name: 'created_at' })
  createdAt: Date;

  @UpdateDateColumn({ name: 'updated_at' })
  updatedAt: Date;

  @Column({ name: 'last_login', nullable: true })
  lastLogin: Date;

  @Column({ name: 'is_active', default: true })
  isActive: boolean;
}












