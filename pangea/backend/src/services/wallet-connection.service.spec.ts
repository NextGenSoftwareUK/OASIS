import { Test, TestingModule } from '@nestjs/testing';
import { WalletConnectionService } from './wallet-connection.service';

describe('WalletConnectionService', () => {
  let service: WalletConnectionService;

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      providers: [WalletConnectionService],
    }).compile();

    service = module.get<WalletConnectionService>(WalletConnectionService);
  });

  it('should be defined', () => {
    expect(service).toBeDefined();
  });

  describe('generateVerificationMessage', () => {
    it('should generate a verification message with user details', () => {
      const userId = 'test-user-id';
      const walletAddress = 'test-wallet-address';
      const blockchain = 'solana';

      const message = service.generateVerificationMessage(userId, walletAddress, blockchain);

      expect(message).toContain(userId);
      expect(message).toContain(walletAddress);
      expect(message).toContain(blockchain);
    });
  });
});












