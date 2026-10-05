import {
  Injectable,
  Logger,
  NotFoundException,
  BadRequestException,
} from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository, FindOptionsWhere, Between } from 'typeorm';
import { Transaction } from '../entities/transaction.entity';
import { User } from '../../users/entities/user.entity';
import { TokenizedAsset } from '../../assets/entities/tokenized-asset.entity';
import { BalanceService } from '../../orders/services/balance.service';
import { WebSocketService } from '../../orders/services/websocket.service';
import { BlockchainService } from '../../blockchain/services/blockchain.service';
import { VaultService } from './vault.service';
import { OasisWalletService } from '../../services/oasis-wallet.service';
import { DepositDto } from '../dto/deposit.dto';
import { WithdrawalDto } from '../dto/withdrawal.dto';
import { TransactionFilters } from '../dto/transaction-filters.dto';

@Injectable()
export class TransactionsService {
  private readonly logger = new Logger(TransactionsService.name);

  constructor(
    @InjectRepository(Transaction)
    private transactionRepository: Repository<Transaction>,
    @InjectRepository(User)
    private userRepository: Repository<User>,
    private balanceService: BalanceService,
    private webSocketService: WebSocketService,
    private blockchainService: BlockchainService,
    private vaultService: VaultService,
    private oasisWalletService: OasisWalletService,
  ) {}

  /**
   * Generate unique transaction ID
   */
  private generateTransactionId(): string {
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 1000);
    return `TXN-${timestamp}-${random}`;
  }

  /**
   * Get user's wallet address for a blockchain
   */
  private async getUserWalletAddress(
    userId: string,
    blockchain: string,
  ): Promise<string> {
    // 1. Try to get from user entity first
    const user = await this.userRepository.findOne({
      where: { id: userId },
    });

    if (!user) {
      throw new NotFoundException(`User ${userId} not found`);
    }

    // 2. Check user entity wallet addresses
    if (blockchain === 'solana' && user.walletAddressSolana) {
      return user.walletAddressSolana;
    }
    if (blockchain === 'ethereum' && user.walletAddressEthereum) {
      return user.walletAddressEthereum;
    }

    // 3. Try to get from OASIS wallet service if avatarId exists
    if (user.avatarId) {
      try {
        const providerType =
          blockchain === 'solana' ? 'SolanaOASIS' : 'EthereumOASIS';
        const defaultWallet =
          await this.oasisWalletService.getDefaultWallet(user.avatarId);

        if (defaultWallet && defaultWallet.providerType === providerType) {
          return defaultWallet.walletAddress;
        }

        // Try to get any wallet of the correct type
        const wallets = await this.oasisWalletService.getWallets(
          user.avatarId,
          providerType,
        );
        if (wallets && wallets.length > 0) {
          return wallets[0].walletAddress;
        }
      } catch (error) {
        this.logger.warn(
          `Failed to get wallet from OASIS for user ${userId}: ${error.message}`,
        );
      }
    }

    // 4. If no wallet found, throw error
    throw new BadRequestException(
      `No wallet address found for user ${userId} on ${blockchain}. Please connect a wallet first.`,
    );
  }

  /**
   * Initiate a deposit
   */
  async initiateDeposit(dto: DepositDto, userId: string): Promise<Transaction> {
    this.logger.log(
      `Initiating deposit: user ${userId}, asset ${dto.assetId}, amount ${dto.amount}`,
    );

    // 1. Get vault address for asset
    const vaultAddress = await this.vaultService.getVaultAddress(
      dto.assetId,
      dto.blockchain,
    );

    // 2. Get user wallet address (if not provided)
    const fromAddress =
      dto.fromAddress || (await this.getUserWalletAddress(userId, dto.blockchain));

    // 3. Convert amount to bigint (assuming amount is in token units)
    const amountBigInt = BigInt(Math.floor(dto.amount * 1e9)); // Adjust decimals as needed

    // 4. Create transaction record
    const transaction = this.transactionRepository.create({
      transactionId: this.generateTransactionId(),
      userId,
      assetId: dto.assetId,
      transactionType: 'deposit',
      status: 'pending',
      amount: amountBigInt,
      blockchain: dto.blockchain,
      toAddress: vaultAddress,
      fromAddress,
      metadata: {
        vaultAddress,
        expectedAmount: dto.amount,
        initiatedAt: new Date().toISOString(),
      },
    });

    const savedTransaction = await this.transactionRepository.save(transaction);

    this.logger.log(
      `✅ Deposit initiated: ${savedTransaction.transactionId}. User should send tokens to: ${vaultAddress}`,
    );

    return savedTransaction;
  }

  /**
   * Initiate a withdrawal
   */
  async initiateWithdrawal(
    dto: WithdrawalDto,
    userId: string,
  ): Promise<Transaction> {
    this.logger.log(
      `Initiating withdrawal: user ${userId}, asset ${dto.assetId}, amount ${dto.amount}, to ${dto.toAddress}`,
    );

    // 1. Validate balance
    const balance = await this.balanceService.getBalance(userId, dto.assetId);
    const amountBigInt = BigInt(Math.floor(dto.amount * 1e9)); // Adjust decimals as needed

    if (balance.availableBalance < amountBigInt) {
      throw new BadRequestException('Insufficient balance');
    }

    // 2. Lock balance
    await this.balanceService.lockBalance(userId, dto.assetId, amountBigInt);

    try {
      // 3. Execute withdrawal on blockchain
      const transactionHash = await this.blockchainService.withdraw({
        user: userId,
        asset: dto.assetId,
        amount: amountBigInt,
        toAddress: dto.toAddress,
        blockchain: dto.blockchain,
      });

      // 4. Get user wallet address
      const fromAddress = await this.getUserWalletAddress(
        userId,
        dto.blockchain,
      );

      // 5. Create transaction record
      const transaction = this.transactionRepository.create({
        transactionId: this.generateTransactionId(),
        userId,
        assetId: dto.assetId,
        transactionType: 'withdrawal',
        status: 'processing',
        amount: amountBigInt,
        blockchain: dto.blockchain,
        transactionHash,
        fromAddress,
        toAddress: dto.toAddress,
        metadata: {
          initiatedAt: new Date().toISOString(),
        },
      });

      const savedTransaction =
        await this.transactionRepository.save(transaction);

      this.logger.log(
        `✅ Withdrawal initiated: ${savedTransaction.transactionId}, tx hash: ${transactionHash}`,
      );

      return savedTransaction;
    } catch (error) {
      // Unlock balance if withdrawal fails
      await this.balanceService.unlockBalance(
        userId,
        dto.assetId,
        amountBigInt,
      );
      throw error;
    }
  }

  /**
   * Confirm a transaction (admin or automatic)
   */
  async confirmTransaction(transactionId: string): Promise<Transaction> {
    this.logger.log(`Confirming transaction: ${transactionId}`);

    const transaction = await this.transactionRepository.findOne({
      where: { transactionId },
      relations: ['user', 'asset'],
    });

    if (!transaction) {
      throw new NotFoundException('Transaction not found');
    }

    if (transaction.status === 'completed') {
      this.logger.warn(`Transaction ${transactionId} already completed`);
      return transaction;
    }

    // 1. Verify on blockchain
    if (!transaction.transactionHash) {
      throw new BadRequestException(
        'Transaction hash not found. Cannot confirm.',
      );
    }

    const blockchainTx = await this.blockchainService.getTransaction(
      transaction.transactionHash,
      transaction.blockchain,
    );

    if (blockchainTx.status !== 'confirmed') {
      throw new BadRequestException(
        'Transaction not confirmed on blockchain',
      );
    }

    // 2. Update transaction
    transaction.status = 'completed';
    transaction.confirmedAt = new Date();
    if (blockchainTx.blockNumber) {
      transaction.blockNumber = blockchainTx.blockNumber;
    }

    await this.transactionRepository.save(transaction);

    // 3. Update balance
    if (transaction.transactionType === 'deposit') {
      await this.balanceService.addBalance(
        transaction.userId,
        transaction.assetId,
        transaction.amount,
      );
      this.logger.log(
        `✅ Deposit confirmed: ${transaction.amount} added to user ${transaction.userId}`,
      );
    } else if (transaction.transactionType === 'withdrawal') {
      await this.balanceService.subtractBalance(
        transaction.userId,
        transaction.assetId,
        transaction.amount,
      );
      await this.balanceService.unlockBalance(
        transaction.userId,
        transaction.assetId,
        transaction.amount,
      );
      this.logger.log(
        `✅ Withdrawal confirmed: ${transaction.amount} subtracted from user ${transaction.userId}`,
      );
    }

    // 4. Emit balance update via WebSocket
    try {
      const balance = await this.balanceService.getBalance(
        transaction.userId,
        transaction.assetId,
      );
      this.webSocketService.emitBalanceUpdate(transaction.userId, {
        assetId: transaction.assetId,
        balance: balance.balance.toString(),
        availableBalance: balance.availableBalance.toString(),
        lockedBalance: balance.lockedBalance.toString(),
        transactionType: transaction.transactionType,
        transactionId: transaction.transactionId,
      });
    } catch (error) {
      this.logger.warn(
        `Failed to emit balance update: ${error.message}`,
      );
    }

    return transaction;
  }

  /**
   * Find transactions by user with filters
   */
  async findByUser(
    userId: string,
    filters: TransactionFilters,
  ): Promise<{ transactions: Transaction[]; total: number; page: number; limit: number }> {
    const where: FindOptionsWhere<Transaction> = { userId };

    if (filters.assetId) {
      where.assetId = filters.assetId;
    }

    if (filters.transactionType) {
      where.transactionType = filters.transactionType;
    }

    if (filters.status) {
      where.status = filters.status;
    }

    if (filters.startDate || filters.endDate) {
      where.createdAt = Between(
        filters.startDate || new Date(0),
        filters.endDate || new Date(),
      );
    }

    const page = filters.page || 1;
    const limit = filters.limit || 20;
    const skip = (page - 1) * limit;

    const [transactions, total] = await this.transactionRepository.findAndCount(
      {
        where,
        relations: ['asset'],
        order: { createdAt: 'DESC' },
        skip,
        take: limit,
      },
    );

    return {
      transactions,
      total,
      page,
      limit,
    };
  }

  /**
   * Find a single transaction by ID
   */
  async findOne(txId: string, userId: string): Promise<Transaction> {
    const transaction = await this.transactionRepository.findOne({
      where: { transactionId: txId, userId },
      relations: ['asset'],
    });

    if (!transaction) {
      throw new NotFoundException('Transaction not found');
    }

    return transaction;
  }

  /**
   * Find pending transactions for a user
   */
  async findPending(userId: string): Promise<Transaction[]> {
    return this.transactionRepository.find({
      where: {
        userId,
        status: 'pending',
      },
      relations: ['asset'],
      order: { createdAt: 'DESC' },
    });
  }

  /**
   * Monitor deposits (background job)
   * This should be called periodically to check for new deposits
   */
  async monitorDeposits(): Promise<void> {
    this.logger.log('Monitoring deposits on blockchain...');

    // Get all pending deposit transactions
    const pendingDeposits = await this.transactionRepository.find({
      where: {
        transactionType: 'deposit',
        status: 'pending',
      },
      relations: ['asset'],
    });

    this.logger.log(`Found ${pendingDeposits.length} pending deposits`);

    for (const deposit of pendingDeposits) {
      try {
        const vaultAddress = deposit.metadata?.vaultAddress;

        if (!vaultAddress) {
          this.logger.warn(
            `Deposit ${deposit.transactionId} has no vault address`,
          );
          continue;
        }

        // Monitor vault for new transactions
        const newTransactions =
          await this.blockchainService.monitorVaultDeposits(
            vaultAddress,
            deposit.blockchain,
          );

        // Check if any transaction matches this deposit
        for (const tx of newTransactions) {
          // Match by amount and toAddress
          if (
            tx.toAddress === vaultAddress &&
            tx.amount === deposit.amount
          ) {
            // Update transaction with hash
            deposit.transactionHash = tx.transactionHash;
            deposit.fromAddress = tx.fromAddress;
            deposit.blockNumber = tx.blockNumber;
            deposit.status = 'processing';
            await this.transactionRepository.save(deposit);

            // Try to confirm
            await this.confirmTransaction(deposit.transactionId);

            this.logger.log(
              `✅ Deposit ${deposit.transactionId} detected and confirmed`,
            );
            break;
          }
        }
      } catch (error) {
        this.logger.error(
          `Error monitoring deposit ${deposit.transactionId}: ${error.message}`,
          error.stack,
        );
      }
    }
  }
}











