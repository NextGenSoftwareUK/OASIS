import {
  Controller,
  Get,
  Post,
  Body,
  Param,
  UseGuards,
  Request,
  HttpException,
  HttpStatus,
} from '@nestjs/common';
import { WalletConnectionService } from '../services/wallet-connection.service';
import { OasisWalletService } from '../services/oasis-wallet.service';
import { BalanceSyncService } from '../services/balance-sync.service';
import { ConnectWalletDto } from './dto/connect-wallet.dto';
import { VerifyWalletDto } from './dto/verify-wallet.dto';
import { GenerateWalletDto } from './dto/generate-wallet.dto';
import { BetterAuthGuard } from '../auth/guards/better-auth.guard';
import { OasisLinkService } from '../auth/services/oasis-link.service';

@Controller('wallet')
@UseGuards(BetterAuthGuard)
export class WalletController {
  constructor(
    private readonly walletConnectionService: WalletConnectionService,
    private readonly oasisWalletService: OasisWalletService,
    private readonly balanceSyncService: BalanceSyncService,
    private readonly oasisLinkService: OasisLinkService,
  ) {}

  /**
   * Get all balances for the authenticated user
   * GET /api/wallet/balance
   */
  @Get('balance')
  async getBalances(@Request() req: any) {
    const userId = req.user?.id;
    const email = req.user?.email;
    const name = req.user?.name;

    if (!userId || !email) {
      throw new HttpException('User not authenticated', HttpStatus.UNAUTHORIZED);
    }

    try {
      // Ensure OASIS avatar exists (lazy creation)
      const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email, name);

      // Get all wallets from OASIS
      const wallets = await this.oasisWalletService.getWallets(avatarId);
      
      // If no wallets exist, return helpful message
      if (!wallets || wallets.length === 0) {
        return {
          success: true,
          balances: [],
          message: 'No wallets found. Generate a wallet first using POST /api/wallet/generate',
          hasWallets: false,
        };
      }
      
      // Get balances for each wallet
      const balances = await Promise.all(
        wallets.map(async (wallet) => {
          try {
            const balance = await this.oasisWalletService.getBalance(
              wallet.walletId,
              wallet.providerType,
            );
            return {
              walletId: wallet.walletId,
              walletAddress: wallet.walletAddress,
              providerType: wallet.providerType,
              balance: balance.balance,
              tokenSymbol: balance.tokenSymbol,
            };
          } catch (error) {
            return {
              walletId: wallet.walletId,
              walletAddress: wallet.walletAddress,
              providerType: wallet.providerType,
              balance: 0,
              error: error.message,
            };
          }
        }),
      );

      return {
        success: true,
        balances,
        hasWallets: true,
      };
    } catch (error: any) {
      // Provide helpful error message
      if (error.response?.status === 404 || error.message?.includes('not found')) {
        return {
          success: true,
          balances: [],
          message: 'No wallets found. Generate a wallet first using POST /api/wallet/generate',
          hasWallets: false,
        };
      }
      throw new HttpException(
        `Failed to get balances: ${error.message}`,
        HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }

  /**
   * Get balance for a specific asset
   * GET /api/wallet/balance/:assetId
   */
  @Get('balance/:assetId')
  async getAssetBalance(@Request() req: any, @Param('assetId') assetId: string) {
    const userId = req.user?.id;
    const email = req.user?.email;
    const name = req.user?.name;

    if (!userId || !email) {
      throw new HttpException('User not authenticated', HttpStatus.UNAUTHORIZED);
    }

    // Ensure OASIS avatar exists (lazy creation)
    const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email, name);

    try {
      // Determine provider type from assetId or use default
      const providerType = assetId.includes('solana') || assetId.includes('SOL')
        ? 'SolanaOASIS'
        : 'EthereumOASIS';

      const balance = await this.balanceSyncService.syncAssetBalance(
        userId,
        avatarId,
        assetId,
        providerType,
      );

      if (!balance) {
        throw new HttpException('Balance not found', HttpStatus.NOT_FOUND);
      }

      return {
        success: true,
        balance,
      };
    } catch (error) {
      throw new HttpException(
        `Failed to get asset balance: ${error.message}`,
        HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }

  /**
   * Generate a new wallet for the authenticated user's avatar
   * POST /api/wallet/generate
   * 
   * Note: Avatar must exist first (created during registration/login).
   * This endpoint creates a new wallet and assigns it to the avatar.
   */
  @Post('generate')
  async generateWallet(@Request() req: any, @Body() dto: GenerateWalletDto) {
    const userId = req.user?.id;
    const email = req.user?.email;
    const name = req.user?.name;

    if (!userId || !email) {
      throw new HttpException('User not authenticated', HttpStatus.UNAUTHORIZED);
    }

    try {
      // Ensure OASIS avatar exists (lazy creation)
      const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email, name);

      const wallet = await this.oasisWalletService.generateWallet(
        avatarId,
        dto.providerType,
        dto.setAsDefault ?? true,
      );

      return {
        success: true,
        message: `Wallet generated successfully for ${dto.providerType}`,
        wallet: {
          walletId: wallet.walletId,
          walletAddress: wallet.walletAddress,
          providerType: wallet.providerType,
          isDefaultWallet: wallet.isDefaultWallet,
          balance: wallet.balance || 0,
        },
      };
    } catch (error: any) {
      throw new HttpException(
        {
          statusCode: HttpStatus.INTERNAL_SERVER_ERROR,
          message: `Failed to generate wallet: ${error.message}. Make sure your avatar exists (register/login first).`,
          error: 'Wallet generation failed',
        },
        HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }

  /**
   * Connect wallet (Phantom or MetaMask)
   * POST /api/wallet/connect
   */
  @Post('connect')
  async connectWallet(@Request() req: any, @Body() dto: ConnectWalletDto) {
    const userId = req.user?.id;

    if (!userId) {
      throw new HttpException('User not authenticated', HttpStatus.UNAUTHORIZED);
    }

    try {
      let result;
      if (dto.blockchain === 'solana') {
        result = await this.walletConnectionService.connectPhantom(
          userId,
          dto.walletAddress,
          dto.signature,
          dto.message,
        );
      } else if (dto.blockchain === 'ethereum') {
        result = await this.walletConnectionService.connectMetaMask(
          userId,
          dto.walletAddress,
          dto.signature,
          dto.message,
        );
      } else {
        throw new HttpException(
          `Unsupported blockchain: ${dto.blockchain}`,
          HttpStatus.BAD_REQUEST,
        );
      }

      // TODO: Link wallet to user account in database
      // await this.userService.linkWallet(userId, dto.walletAddress, dto.blockchain);

      return {
        success: true,
        message: 'Wallet connected successfully',
        wallet: {
          address: result.walletAddress,
          blockchain: result.blockchain,
        },
      };
    } catch (error) {
      throw new HttpException(
        `Failed to connect wallet: ${error.message}`,
        error.status || HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }

  /**
   * Verify wallet ownership
   * POST /api/wallet/verify
   */
  @Post('verify')
  async verifyWallet(@Request() req: any, @Body() dto: VerifyWalletDto) {
    const userId = req.user?.id;

    if (!userId) {
      throw new HttpException('User not authenticated', HttpStatus.UNAUTHORIZED);
    }

    try {
      const result = await this.walletConnectionService.verifyOwnership({
        walletAddress: dto.walletAddress,
        signature: dto.signature,
        message: dto.message,
        blockchain: dto.blockchain,
      });

      return {
        success: true,
        verified: result.isValid,
        wallet: {
          address: result.walletAddress,
          blockchain: result.blockchain,
        },
      };
    } catch (error) {
      throw new HttpException(
        `Failed to verify wallet: ${error.message}`,
        error.status || HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }

  /**
   * Manually trigger balance sync
   * POST /api/wallet/sync
   */
  @Post('sync')
  async syncBalances(@Request() req: any) {
    const userId = req.user?.id;
    const email = req.user?.email;
    const name = req.user?.name;

    if (!userId || !email) {
      throw new HttpException('User not authenticated', HttpStatus.UNAUTHORIZED);
    }

    try {
      // Ensure OASIS avatar exists (lazy creation)
      const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email, name);

      const balances = await this.balanceSyncService.syncUserBalances(userId, avatarId);

      return {
        success: true,
        message: 'Balances synced successfully',
        balances,
      };
    } catch (error) {
      throw new HttpException(
        `Failed to sync balances: ${error.message}`,
        HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }

  /**
   * Get wallet generation message for frontend
   * GET /api/wallet/verification-message
   */
  @Get('verification-message')
  async getVerificationMessage(@Request() req: any, @Body() body: { walletAddress: string; blockchain: string }) {
    const userId = req.user?.id;

    if (!userId) {
      throw new HttpException('User not authenticated', HttpStatus.UNAUTHORIZED);
    }

    const message = this.walletConnectionService.generateVerificationMessage(
      userId,
      body.walletAddress,
      body.blockchain,
    );

    return {
      success: true,
      message,
    };
  }

  /**
   * Get transaction history for a wallet
   * GET /api/wallet/transactions/:walletId
   */
  @Get('transactions/:walletId')
  async getTransactions(@Request() req: any, @Param('walletId') walletId: string) {
    const userId = req.user?.id;
    const email = req.user?.email;
    const name = req.user?.name;

    if (!userId || !email) {
      throw new HttpException('User not authenticated', HttpStatus.UNAUTHORIZED);
    }

    try {
      // Ensure OASIS avatar exists (lazy creation)
      const avatarId = await this.oasisLinkService.ensureOasisAvatar(userId, email, name);

      // Verify wallet belongs to user
      const wallets = await this.oasisWalletService.getWallets(avatarId);
      const wallet = wallets.find((w) => w.walletId === walletId);

      if (!wallet) {
        throw new HttpException('Wallet not found', HttpStatus.NOT_FOUND);
      }

      const transactions = await this.oasisWalletService.getTransactions(walletId);

      return {
        success: true,
        transactions,
      };
    } catch (error) {
      throw new HttpException(
        `Failed to get transactions: ${error.message}`,
        error.status || HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }
}












