import { Test, TestingModule } from '@nestjs/testing';
import { getRepositoryToken } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { TransactionsService } from './transactions.service';
import { Transaction } from '../entities/transaction.entity';
import { User } from '../../users/entities/user.entity';
import { BalanceService } from '../../orders/services/balance.service';
import { BlockchainService } from '../../blockchain/services/blockchain.service';
import { VaultService } from './vault.service';
import { OasisWalletService } from '../../services/oasis-wallet.service';

describe('TransactionsService', () => {
  let service: TransactionsService;
  let transactionRepository: Repository<Transaction>;
  let userRepository: Repository<User>;
  let balanceService: BalanceService;
  let blockchainService: BlockchainService;
  let vaultService: VaultService;
  let oasisWalletService: OasisWalletService;

  const mockTransactionRepository = {
    create: jest.fn(),
    save: jest.fn(),
    findOne: jest.fn(),
    find: jest.fn(),
    findAndCount: jest.fn(),
  };

  const mockUserRepository = {
    findOne: jest.fn(),
  };

  const mockBalanceService = {
    getBalance: jest.fn(),
    lockBalance: jest.fn(),
    unlockBalance: jest.fn(),
    addBalance: jest.fn(),
    subtractBalance: jest.fn(),
  };

  const mockBlockchainService = {
    withdraw: jest.fn(),
    getTransaction: jest.fn(),
    monitorVaultDeposits: jest.fn(),
  };

  const mockVaultService = {
    getVaultAddress: jest.fn(),
  };

  const mockOasisWalletService = {
    getDefaultWallet: jest.fn(),
    getWallets: jest.fn(),
  };

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      providers: [
        TransactionsService,
        {
          provide: getRepositoryToken(Transaction),
          useValue: mockTransactionRepository,
        },
        {
          provide: getRepositoryToken(User),
          useValue: mockUserRepository,
        },
        {
          provide: BalanceService,
          useValue: mockBalanceService,
        },
        {
          provide: BlockchainService,
          useValue: mockBlockchainService,
        },
        {
          provide: VaultService,
          useValue: mockVaultService,
        },
        {
          provide: OasisWalletService,
          useValue: mockOasisWalletService,
        },
      ],
    }).compile();

    service = module.get<TransactionsService>(TransactionsService);
    transactionRepository = module.get<Repository<Transaction>>(
      getRepositoryToken(Transaction),
    );
    userRepository = module.get<Repository<User>>(getRepositoryToken(User));
    balanceService = module.get<BalanceService>(BalanceService);
    blockchainService = module.get<BlockchainService>(BlockchainService);
    vaultService = module.get<VaultService>(VaultService);
    oasisWalletService = module.get<OasisWalletService>(OasisWalletService);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should be defined', () => {
    expect(service).toBeDefined();
  });

  describe('initiateDeposit', () => {
    it('should create a deposit transaction', async () => {
      const userId = 'user-123';
      const depositDto = {
        assetId: 'asset-123',
        amount: 100,
        blockchain: 'solana',
      };

      const mockVaultAddress = 'vault-address-123';
      const mockUser = {
        id: userId,
        walletAddressSolana: 'wallet-123',
      };

      mockVaultService.getVaultAddress.mockResolvedValue(mockVaultAddress);
      mockUserRepository.findOne.mockResolvedValue(mockUser);
      mockTransactionRepository.create.mockReturnValue({
        transactionId: 'TXN-123',
        ...depositDto,
      });
      mockTransactionRepository.save.mockResolvedValue({
        transactionId: 'TXN-123',
        ...depositDto,
      });

      const result = await service.initiateDeposit(depositDto, userId);

      expect(mockVaultService.getVaultAddress).toHaveBeenCalledWith(
        depositDto.assetId,
        depositDto.blockchain,
      );
      expect(mockTransactionRepository.create).toHaveBeenCalled();
      expect(mockTransactionRepository.save).toHaveBeenCalled();
      expect(result.transactionId).toBe('TXN-123');
    });
  });

  describe('initiateWithdrawal', () => {
    it('should create a withdrawal transaction when balance is sufficient', async () => {
      const userId = 'user-123';
      const withdrawalDto = {
        assetId: 'asset-123',
        amount: 50,
        toAddress: 'recipient-123',
        blockchain: 'solana',
      };

      const mockBalance = {
        userId,
        assetId: withdrawalDto.assetId,
        availableBalance: BigInt(100 * 1e9),
        lockedBalance: BigInt(0),
      };

      const mockUser = {
        id: userId,
        walletAddressSolana: 'wallet-123',
      };

      const mockTransactionHash = 'tx-hash-123';

      mockBalanceService.getBalance.mockResolvedValue(mockBalance);
      mockUserRepository.findOne.mockResolvedValue(mockUser);
      mockBlockchainService.withdraw.mockResolvedValue(mockTransactionHash);
      mockTransactionRepository.create.mockReturnValue({
        transactionId: 'TXN-123',
        ...withdrawalDto,
      });
      mockTransactionRepository.save.mockResolvedValue({
        transactionId: 'TXN-123',
        transactionHash: mockTransactionHash,
        ...withdrawalDto,
      });

      const result = await service.initiateWithdrawal(withdrawalDto, userId);

      expect(mockBalanceService.getBalance).toHaveBeenCalledWith(
        userId,
        withdrawalDto.assetId,
      );
      expect(mockBalanceService.lockBalance).toHaveBeenCalled();
      expect(mockBlockchainService.withdraw).toHaveBeenCalled();
      expect(result.transactionHash).toBe(mockTransactionHash);
    });

    it('should throw error when balance is insufficient', async () => {
      const userId = 'user-123';
      const withdrawalDto = {
        assetId: 'asset-123',
        amount: 200,
        toAddress: 'recipient-123',
        blockchain: 'solana',
      };

      const mockBalance = {
        userId,
        assetId: withdrawalDto.assetId,
        availableBalance: BigInt(100 * 1e9), // Less than requested
        lockedBalance: BigInt(0),
      };

      mockBalanceService.getBalance.mockResolvedValue(mockBalance);

      await expect(
        service.initiateWithdrawal(withdrawalDto, userId),
      ).rejects.toThrow('Insufficient balance');
    });
  });

  describe('confirmTransaction', () => {
    it('should confirm a deposit transaction and update balance', async () => {
      const transactionId = 'TXN-123';
      const mockTransaction = {
        transactionId,
        userId: 'user-123',
        assetId: 'asset-123',
        transactionType: 'deposit',
        status: 'pending',
        amount: BigInt(100 * 1e9),
        blockchain: 'solana',
        transactionHash: 'tx-hash-123',
        user: { id: 'user-123' },
        asset: { id: 'asset-123' },
      };

      const mockBlockchainTx = {
        status: 'confirmed',
        blockNumber: BigInt(12345),
      };

      mockTransactionRepository.findOne.mockResolvedValue(mockTransaction);
      mockBlockchainService.getTransaction.mockResolvedValue(mockBlockchainTx);
      mockTransactionRepository.save.mockResolvedValue({
        ...mockTransaction,
        status: 'completed',
        confirmedAt: new Date(),
      });

      const result = await service.confirmTransaction(transactionId);

      expect(mockBlockchainService.getTransaction).toHaveBeenCalledWith(
        mockTransaction.transactionHash,
        mockTransaction.blockchain,
      );
      expect(mockBalanceService.addBalance).toHaveBeenCalledWith(
        mockTransaction.userId,
        mockTransaction.assetId,
        mockTransaction.amount,
      );
      expect(result.status).toBe('completed');
    });

    it('should confirm a withdrawal transaction and update balance', async () => {
      const transactionId = 'TXN-123';
      const mockTransaction = {
        transactionId,
        userId: 'user-123',
        assetId: 'asset-123',
        transactionType: 'withdrawal',
        status: 'processing',
        amount: BigInt(50 * 1e9),
        blockchain: 'solana',
        transactionHash: 'tx-hash-123',
        user: { id: 'user-123' },
        asset: { id: 'asset-123' },
      };

      const mockBlockchainTx = {
        status: 'confirmed',
        blockNumber: BigInt(12345),
      };

      mockTransactionRepository.findOne.mockResolvedValue(mockTransaction);
      mockBlockchainService.getTransaction.mockResolvedValue(mockBlockchainTx);
      mockTransactionRepository.save.mockResolvedValue({
        ...mockTransaction,
        status: 'completed',
        confirmedAt: new Date(),
      });

      const result = await service.confirmTransaction(transactionId);

      expect(mockBalanceService.subtractBalance).toHaveBeenCalledWith(
        mockTransaction.userId,
        mockTransaction.assetId,
        mockTransaction.amount,
      );
      expect(mockBalanceService.unlockBalance).toHaveBeenCalledWith(
        mockTransaction.userId,
        mockTransaction.assetId,
        mockTransaction.amount,
      );
      expect(result.status).toBe('completed');
    });
  });
});











