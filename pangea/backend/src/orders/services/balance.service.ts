import { Injectable, BadRequestException, Logger } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { UserBalance } from '../../users/entities/user-balance.entity';

@Injectable()
export class BalanceService {
  private readonly logger = new Logger(BalanceService.name);

  constructor(
    @InjectRepository(UserBalance)
    private balanceRepository: Repository<UserBalance>,
  ) {}

  /**
   * Get balance for a user and asset
   */
  async getBalance(
    userId: string,
    assetId: string,
  ): Promise<UserBalance> {
    const balance = await this.balanceRepository.findOne({
      where: { userId, assetId },
    });

    if (!balance) {
      // Create a new balance record if it doesn't exist
      const newBalance = this.balanceRepository.create({
        userId,
        assetId,
        balance: BigInt(0),
        availableBalance: BigInt(0),
        lockedBalance: BigInt(0),
        blockchain: 'solana',
      });
      return await this.balanceRepository.save(newBalance);
    }

    return balance;
  }

  /**
   * Lock balance for an order (for sell orders)
   */
  async lockBalance(
    userId: string,
    assetId: string,
    quantity: bigint,
  ): Promise<void> {
    this.logger.log(
      `Locking balance: user ${userId}, asset ${assetId}, quantity ${quantity}`,
    );

    // TODO: Implement actual database update when UserBalance entity is available
    // const balance = await this.getBalance(userId, assetId);
    //
    // if (balance.availableBalance < quantity) {
    //   throw new BadRequestException('Insufficient available balance');
    // }
    //
    // balance.availableBalance -= quantity;
    // balance.lockedBalance += quantity;
    //
    // await this.balanceRepository.save(balance);
  }

  /**
   * Unlock balance (when order is cancelled or filled)
   */
  async unlockBalance(
    userId: string,
    assetId: string,
    quantity: bigint,
  ): Promise<void> {
    this.logger.log(
      `Unlocking balance: user ${userId}, asset ${assetId}, quantity ${quantity}`,
    );

    const balance = await this.getBalance(userId, assetId);

    if (balance.lockedBalance < quantity) {
      this.logger.warn(
        `Attempted to unlock ${quantity} but only ${balance.lockedBalance} is locked. Unlocking available amount.`,
      );
      quantity = balance.lockedBalance;
    }

    balance.availableBalance += quantity;
    balance.lockedBalance -= quantity;

    await this.balanceRepository.save(balance);
  }

  /**
   * Transfer tokens from seller to buyer
   */
  async transfer(
    fromUserId: string,
    toUserId: string,
    assetId: string,
    quantity: bigint,
  ): Promise<void> {
    this.logger.log(
      `Transferring ${quantity} of asset ${assetId} from ${fromUserId} to ${toUserId}`,
    );

    const fromBalance = await this.getBalance(fromUserId, assetId);
    const toBalance = await this.getBalance(toUserId, assetId);

    // Use lockedBalance for transfer (it's coming from an order)
    if (fromBalance.lockedBalance < quantity) {
      throw new BadRequestException('Insufficient locked balance for transfer');
    }

    // Deduct from seller (unlock and remove from balance)
    fromBalance.balance -= quantity;
    fromBalance.lockedBalance -= quantity;

    // Add to buyer
    toBalance.balance += quantity;
    toBalance.availableBalance += quantity;

    await this.balanceRepository.save([fromBalance, toBalance]);
  }

  /**
   * Get payment token balance (USDC, SOL, etc.) for a user
   */
  async getPaymentTokenBalance(
    userId: string,
    blockchain: string,
  ): Promise<bigint> {
    this.logger.log(
      `Getting payment token balance for user ${userId} on ${blockchain}`,
    );

    // TODO: Implement actual query for payment token balance
    // This would typically query for USDC (Solana/Ethereum) or native token (SOL, ETH)
    return BigInt(0);
  }

  /**
   * Add balance to user (for deposits)
   */
  async addBalance(
    userId: string,
    assetId: string,
    amount: bigint,
  ): Promise<void> {
    this.logger.log(
      `Adding balance: user ${userId}, asset ${assetId}, amount ${amount}`,
    );

    const balance = await this.getBalance(userId, assetId);
    balance.balance += amount;
    balance.availableBalance += amount;

    await this.balanceRepository.save(balance);
  }

  /**
   * Subtract balance from user (for withdrawals)
   */
  async subtractBalance(
    userId: string,
    assetId: string,
    amount: bigint,
  ): Promise<void> {
    this.logger.log(
      `Subtracting balance: user ${userId}, asset ${assetId}, amount ${amount}`,
    );

    const balance = await this.getBalance(userId, assetId);

    if (balance.availableBalance < amount) {
      throw new BadRequestException('Insufficient available balance');
    }

    balance.balance -= amount;
    balance.availableBalance -= amount;

    await this.balanceRepository.save(balance);
  }
}












