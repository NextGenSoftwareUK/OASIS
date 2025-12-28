/**
 * Script to authenticate OASIS_ADMIN avatar and get wallet info
 * Run with: node scripts/auth-and-deploy.js
 */

const axios = require('axios');
const fs = require('fs');
const path = require('path');

const OASIS_API_URL = process.env.OASIS_API_URL || 'https://api.oasisplatform.world';
const USERNAME = 'OASIS_ADMIN';
const PASSWORD = 'Uppermall1!';

async function authenticateAndGetWallets() {
  console.log('🔐 Authenticating OASIS_ADMIN avatar...');

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
      console.error('❌ No JWT token received');
      console.error('Response:', JSON.stringify(authResponse.data, null, 2));
      throw new Error('Authentication failed: No token received');
    }

    console.log('✅ Authentication successful');
    console.log(`🔑 Token: ${token.substring(0, 50)}...`);

    // Extract avatar ID
    const avatarId =
      authResponse.data?.result?.result?.avatarId ||
      authResponse.data?.result?.result?.id ||
      authResponse.data?.result?.avatarId ||
      authResponse.data?.avatarId;

    if (!avatarId) {
      console.error('❌ No avatar ID received');
      throw new Error('Authentication failed: No avatar ID received');
    }

    console.log(`👤 Avatar ID: ${avatarId}`);

    // Step 2: Get wallets
    console.log('🔍 Fetching Solana wallets...');

    const walletsResponse = await axios.get(
      `${OASIS_API_URL}/api/wallet/avatar/${avatarId}/wallets`,
      {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      },
    );

    const wallets = walletsResponse.data?.result || walletsResponse.data || [];
    console.log(`📦 Found ${wallets.length} wallet(s)`);

    // Find Solana wallet
    const solanaWallet = wallets.find(
      (w) =>
        w.providerType === 'SolanaOASIS' ||
        w.providerType === 'Solana' ||
        w.blockchain === 'Solana',
    );

    if (!solanaWallet) {
      console.error('❌ No Solana wallet found');
      console.error('Available wallets:', wallets.map((w) => w.providerType || w.blockchain));
      throw new Error('No Solana wallet found for avatar');
    }

    console.log(`✅ Found Solana wallet: ${solanaWallet.address || solanaWallet.publicKey}`);
    console.log(`   Wallet ID: ${solanaWallet.id || solanaWallet.walletId}`);
    console.log(`   Provider: ${solanaWallet.providerType}`);

    // Step 3: Try to get wallet details with keypair
    let keypairData = null;
    let keypairPath = null;

    try {
      const walletDetailsResponse = await axios.get(
        `${OASIS_API_URL}/api/wallet/${solanaWallet.id || solanaWallet.walletId}`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        },
      );

      const walletDetails = walletDetailsResponse.data?.result || walletDetailsResponse.data;
      
      if (walletDetails.privateKey || walletDetails.keypair) {
        keypairData = walletDetails.privateKey || walletDetails.keypair;
        console.log('✅ Retrieved keypair from API');
        
        // Save to temp file
        const tempKeypairPath = path.join(process.cwd(), 'temp-wallet-keypair.json');
        fs.writeFileSync(tempKeypairPath, JSON.stringify(keypairData, null, 2));
        keypairPath = tempKeypairPath;
        console.log(`💾 Saved keypair to: ${keypairPath}`);
      }
    } catch (error) {
      console.warn(`⚠️ Could not fetch keypair from API: ${error.message}`);
    }

    // Check for local keypair files
    if (!keypairPath) {
      const possiblePaths = [
        path.join(process.cwd(), 'wallet-keypair.json'),
        path.join(process.cwd(), 'solana-keypair.json'),
        path.join(process.env.HOME || '', '.config', 'solana', 'id.json'),
      ];

      for (const possiblePath of possiblePaths) {
        if (fs.existsSync(possiblePath)) {
          keypairPath = possiblePath;
          console.log(`✅ Found keypair file: ${keypairPath}`);
          break;
        }
      }
    }

    return {
      token,
      avatarId,
      wallet: solanaWallet,
      keypairPath,
      walletAddress: solanaWallet.address || solanaWallet.publicKey,
    };
  } catch (error) {
    console.error(`❌ Authentication failed: ${error.message}`);
    if (error.response) {
      console.error('Response:', JSON.stringify(error.response.data, null, 2));
    }
    throw error;
  }
}

// Main execution
async function main() {
  try {
    const result = await authenticateAndGetWallets();

    console.log('\n' + '='.repeat(50));
    console.log('Authentication Summary:');
    console.log(`Avatar ID: ${result.avatarId}`);
    console.log(`Wallet Address: ${result.walletAddress}`);
    console.log(`Keypair Path: ${result.keypairPath || 'Not found'}`);
    console.log('='.repeat(50));

    // Save to .env file for contract deployment
    const envContent = `
# OASIS Authentication (from script)
OASIS_ADMIN_AVATAR_ID=${result.avatarId}
OASIS_ADMIN_WALLET_ADDRESS=${result.walletAddress}
${result.keypairPath ? `SOLANA_WALLET_KEYPAIR_PATH=${result.keypairPath}` : '# SOLANA_WALLET_KEYPAIR_PATH=path/to/your/keypair.json'}
ISSUER_WALLET=${result.walletAddress}
PLATFORM_WALLET=${result.walletAddress}
`;

    const envPath = path.join(process.cwd(), '.env.deployment');
    fs.writeFileSync(envPath, envContent);
    console.log(`\n💾 Saved deployment config to: ${envPath}`);
    console.log('\n✅ Ready to deploy contracts!');
    console.log('   Run: npm run start:dev (or use the API endpoints)');
  } catch (error) {
    console.error('Fatal error:', error.message);
    process.exit(1);
  }
}

// Run the script
main();












