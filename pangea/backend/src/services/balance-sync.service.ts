import { Injectable, Logger } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { OasisWalletService } from './oasis-wallet.service';

// Note: This assumes user_balances entity exists from Task 02
// For now, we'll use a placeholder interface
export interface UserBalance {
  id?: string;
  userId: string;
  assetId: string;
  walletId: string;
  balance: number;
  providerType: string;
  updatedAt: Date;
}

@Injectable()
export class BalanceSyncService {
  private readonly logger = new Logger(BalanceSyncService.name);

  constructor(
    private readonly oasisWalletService: OasisWalletService,
    // @InjectRepository(UserBalance)
    // private readonly userBalanceRepository: Repository<UserBalance>,
  ) {}

  /**
   * Sync all balances for a user
   * Fetches all wallets from OASIS and updates local database
   */
  async syncUserBalances(userId: string, avatarId: string): Promise<UserBalance[]> {
    try {
      this.logger.log(`Syncing balances for user ${userId} (avatar ${avatarId})`);

      // 1. Get all wallets from OASIS
      const wallets = await this.oasisWalletService.getWallets(avatarId);
      
      this.logger.log(`Found ${wallets.length} wallets for avatar ${avatarId}`);

      const balances: UserBalance[] = [];

      // 2. Get balances for each wallet
      for (const wallet of wallets) {
        try {
          const balance = await this.oasisWalletService.getBalance(
            wallet.walletId,
            wallet.providerType,
          );

          const userBalance: UserBalance = {
            userId,
            assetId: wallet.providerType, // Using providerType as assetId for now
            walletId: wallet.walletId,
            balance: balance.balance,
            providerType: wallet.providerType,
            updatedAt: new Date(),
          };

          balances.push(userBalance);

          // 3. Update local database
          // await this.updateOrCreateBalance(userBalance);

        } catch (error) {
          this.logger.error(
            `Failed to sync balance for wallet ${wallet.walletId}: ${error.message}`,
          );
          // Continue with other wallets
        }
      }

      this.logger.log(`Successfully synced ${balances.length} balances for user ${userId}`);
      return balances;
    } catch (error) {
      this.logger.error(`Failed to sync user balances: ${error.message}`, error.stack);
      throw error;
    }
  }

  /**
   * Sync balance for a specific asset
   */
  async syncAssetBalance(
    userId: string,
    avatarId: string,
    assetId: string,
    providerType: string,
  ): Promise<UserBalance | null> {
    try {
      this.logger.log(`Syncing balance for asset ${assetId} (${providerType})`);

      // 1. Get wallet for the asset/provider
      const wallets = await this.oasisWalletService.getWallets(avatarId, providerType);
      
      if (wallets.length === 0) {
        this.logger.warn(`No wallet found for provider ${providerType}`);
        return null;
      }

      const wallet = wallets[0]; // Use first wallet for the provider

      // 2. Get balance from OASIS
      const balance = await this.oasisWalletService.getBalance(
        wallet.walletId,
        wallet.providerType,
      );

      const userBalance: UserBalance = {
        userId,
        assetId,
        walletId: wallet.walletId,
        balance: balance.balance,
        providerType: wallet.providerType,
        updatedAt: new Date(),
      };

      // 3. Update local database
      // await this.updateOrCreateBalance(userBalance);

      return userBalance;
    } catch (error) {
      this.logger.error(`Failed to sync asset balance: ${error.message}`, error.stack);
      throw error;
    }
  }

  /**
   * Refresh balance for a specific wallet
   */
  async refreshWalletBalance(walletId: string, providerType: string): Promise<number> {
    try {
      const balance = await this.oasisWalletService.refreshBalance(walletId, providerType);
      return balance.balance;
    } catch (error) {
      this.logger.error(`Failed to refresh wallet balance: ${error.message}`, error.stack);
      throw error;
    }
  }

  /**
   * Update or create balance in local database
   * Note: This will be implemented once the UserBalance entity is available
   */
  // private async updateOrCreateBalance(balance: UserBalance): Promise<UserBalance> {
  //   const existing = await this.userBalanceRepository.findOne({
  //     where: {
  //       userId: balance.userId,
  //       assetId: balance.assetId,
  //       walletId: balance.walletId,
  //     },
  //   });

  //   if (existing) {
  //     existing.balance = balance.balance;
  //     existing.updatedAt = new Date();
  //     return await this.userBalanceRepository.save(existing);
  //   } else {
  //     return await this.userBalanceRepository.save(balance);
  //   }
  // }
}












