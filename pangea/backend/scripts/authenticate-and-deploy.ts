/**
 * Script to authenticate OASIS_ADMIN avatar and deploy contracts using their Solana wallet
 * Run with: cd backend && npx ts-node scripts/authenticate-and-deploy.ts
 */

import axios from 'axios';
import * as fs from 'fs';
import * as path from 'path';

// Simple logger
const logger = {
  log: (msg: string) => console.log(`[LOG] ${msg}`),
  error: (msg: string) => console.error(`[ERROR] ${msg}`),
  warn: (msg: string) => console.warn(`[WARN] ${msg}`),
};

const OASIS_API_URL = process.env.OASIS_API_URL || 'https://api.oasisplatform.world';
const USERNAME = 'OASIS_ADMIN';
const PASSWORD = 'Uppermall1!';

interface OASISWallet {
  id: string;
  address: string;
  providerType: string;
  privateKey?: string;
  keypair?: any;
}

async function authenticateAndGetWallets() {
  logger.log('🔐 Authenticating OASIS_ADMIN avatar...');

  try {
    // Step 1: Authenticate
    const authResponse = await axios.post(
      `${OASIS_API_URL}/api/avatar/authenticate`,
      {
        username: USERNAME,
        password: PASSWORD,
      },
    );

    // Extract JWT token from nested response
    const token =
      authResponse.data?.result?.result?.jwtToken ||
      authResponse.data?.result?.jwtToken ||
      authResponse.data?.jwtToken;

    if (!token) {
      logger.error('❌ No JWT token received');
      logger.error('Response:', JSON.stringify(authResponse.data, null, 2));
      throw new Error('Authentication failed: No token received');
    }

    logger.log('✅ Authentication successful');
    logger.log(`🔑 Token: ${token.substring(0, 50)}...`);

    // Extract avatar ID
    const avatarId =
      authResponse.data?.result?.result?.avatarId ||
      authResponse.data?.result?.result?.id ||
      authResponse.data?.result?.avatarId ||
      authResponse.data?.avatarId;

    if (!avatarId) {
      logger.error('❌ No avatar ID received');
      throw new Error('Authentication failed: No avatar ID received');
    }

    logger.log(`👤 Avatar ID: ${avatarId}`);

    // Step 2: Get wallets
    logger.log('🔍 Fetching Solana wallets...');

    const walletsResponse = await axios.get(
      `${OASIS_API_URL}/api/wallet/avatar/${avatarId}/wallets`,
      {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      },
    );

    const wallets = walletsResponse.data?.result || walletsResponse.data || [];
    logger.log(`📦 Found ${wallets.length} wallet(s)`);

    // Find Solana wallet
    const solanaWallet = wallets.find(
      (w: any) =>
        w.providerType === 'SolanaOASIS' ||
        w.providerType === 'Solana' ||
        w.blockchain === 'Solana',
    );

    if (!solanaWallet) {
      logger.error('❌ No Solana wallet found');
      logger.error('Available wallets:', wallets.map((w: any) => w.providerType || w.blockchain));
      throw new Error('No Solana wallet found for avatar');
    }

    logger.log(`✅ Found Solana wallet: ${solanaWallet.address || solanaWallet.publicKey}`);

    // Step 3: Get wallet keypair (if available via API)
    let keypairPath: string | null = null;
    let keypairData: any = null;

    try {
      // Try to get wallet details with keypair
      const walletDetailsResponse = await axios.get(
        `${OASIS_API_URL}/api/wallet/${solanaWallet.id}`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        },
      );

      const walletDetails = walletDetailsResponse.data?.result || walletDetailsResponse.data;
      
      // Check if keypair is in the response
      if (walletDetails.privateKey || walletDetails.keypair) {
        keypairData = walletDetails.privateKey || walletDetails.keypair;
        logger.log('✅ Retrieved keypair from API');
      }
    } catch (error: any) {
      logger.warn(`⚠️ Could not fetch keypair from API: ${error.message}`);
    }

    // If no keypair from API, try to find it in local storage or generate temp file
    if (!keypairData) {
      logger.warn('⚠️ Keypair not available via API. Checking for local keypair file...');
      
      // Check common keypair locations
      const possiblePaths = [
        path.join(process.cwd(), 'wallet-keypair.json'),
        path.join(process.cwd(), 'solana-keypair.json'),
        path.join(process.env.HOME || '', '.config', 'solana', 'id.json'),
      ];

      for (const possiblePath of possiblePaths) {
        if (fs.existsSync(possiblePath)) {
          keypairPath = possiblePath;
          logger.log(`✅ Found keypair file: ${keypairPath}`);
          break;
        }
      }
    } else {
      // Save keypair to temp file
      const tempKeypairPath = path.join(process.cwd(), 'temp-wallet-keypair.json');
      fs.writeFileSync(tempKeypairPath, JSON.stringify(keypairData, null, 2));
      keypairPath = tempKeypairPath;
      logger.log(`💾 Saved keypair to: ${keypairPath}`);
    }

    return {
      token,
      avatarId,
      wallet: solanaWallet,
      keypairPath,
    };
  } catch (error: any) {
    logger.error(`❌ Authentication failed: ${error.message}`);
    if (error.response) {
      logger.error('Response:', JSON.stringify(error.response.data, null, 2));
    }
    throw error;
  }
}

async function deployContracts(keypairPath: string | null, walletAddress: string) {
  logger.log('\n🚀 Starting contract deployment...\n');

  // Set keypair path in environment if available
  if (keypairPath) {
    process.env.SOLANA_WALLET_KEYPAIR_PATH = keypairPath;
    logger.log(`🔑 Using keypair: ${keypairPath}`);
  } else {
    logger.warn('⚠️ No keypair path provided. Deployment may fail without wallet.');
  }

  // Set issuer wallet
  if (!process.env.ISSUER_WALLET) {
    process.env.ISSUER_WALLET = walletAddress;
    logger.log(`💼 Using issuer wallet: ${walletAddress}`);
  }

  // Import SmartContractService dynamically
  const { SmartContractService } = await import('../src/smart-contracts/services/smart-contract.service');
  const service = new SmartContractService();

  const contracts = [
    {
      name: 'RWA Token',
      deploy: () =>
        service.generateRwaToken({
          name: 'Pangea RWA Token',
          symbol: 'PRWA',
          totalSupply: 1000000,
          metadataUri: 'https://pangea.rkund.com/metadata/rwa-token.json',
          issuerWallet: process.env.ISSUER_WALLET || '',
          decimals: 0,
        }),
    },
    {
      name: 'Order Book',
      deploy: () => service.deployOrderBook(),
    },
    {
      name: 'Trade Execution',
      deploy: () => service.deployTradeExecution(),
    },
    {
      name: 'Vault',
      deploy: () => service.deployVault(),
    },
  ];

  const results: Array<{
    name: string;
    success: boolean;
    address?: string;
    error?: string;
  }> = [];

  for (const contract of contracts) {
    try {
      logger.log(`\n📦 Deploying ${contract.name} contract...`);
      logger.log('⏳ This may take 20+ minutes for first compilation...');
      
      const result = await contract.deploy();
      results.push({
        name: contract.name,
        success: true,
        address: result.contractAddress,
      });
      logger.log(`✅ ${contract.name} deployed: ${result.contractAddress}`);
    } catch (error: any) {
      results.push({
        name: contract.name,
        success: false,
        error: error.message,
      });
      logger.error(`❌ ${contract.name} deployment failed: ${error.message}`);
    }
  }

  logger.log('\n📊 Deployment Summary:');
  logger.log('='.repeat(50));
  results.forEach((result) => {
    if (result.success) {
      logger.log(`✅ ${result.name}: ${result.address}`);
    } else {
      logger.log(`❌ ${result.name}: ${result.error}`);
    }
  });
  logger.log('='.repeat(50));

  const successCount = results.filter((r) => r.success).length;
  logger.log(`\n✅ Successfully deployed: ${successCount}/4 contracts`);

  return results;
}

// Main execution
async function main() {
  try {
    // Step 1: Authenticate and get wallets
    const { token, avatarId, wallet, keypairPath } = await authenticateAndGetWallets();

    logger.log('\n' + '='.repeat(50));
    logger.log('Authentication Summary:');
    logger.log(`Avatar ID: ${avatarId}`);
    logger.log(`Wallet Address: ${wallet.address || wallet.publicKey}`);
    logger.log(`Keypair Path: ${keypairPath || 'Not found'}`);
    logger.log('='.repeat(50) + '\n');

    // Step 2: Deploy contracts
    const results = await deployContracts(keypairPath);

    // Cleanup temp keypair file if created
    if (keypairPath && keypairPath.includes('temp-wallet-keypair.json')) {
      try {
        fs.unlinkSync(keypairPath);
        logger.log(`🧹 Cleaned up temp keypair file`);
      } catch (e) {
        // Ignore cleanup errors
      }
    }

    if (results.filter((r) => r.success).length < 4) {
      process.exit(1);
    }
  } catch (error: any) {
    logger.error('Fatal error:', error.message);
    process.exit(1);
  }
}

// Run the script
main();












