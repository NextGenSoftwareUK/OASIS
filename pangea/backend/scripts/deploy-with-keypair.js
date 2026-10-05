/**
 * Deploy contracts using a local Solana keypair file
 * Usage: node scripts/deploy-with-keypair.js [keypair-path]
 */

const fs = require('fs');
const path = require('path');

// Get keypair path from command line or use default
const keypairPath = process.argv[2] || process.env.SOLANA_WALLET_KEYPAIR_PATH;

if (!keypairPath || !fs.existsSync(keypairPath)) {
  console.error('❌ Keypair file not found!');
  console.error('Usage: node scripts/deploy-with-keypair.js [path/to/keypair.json]');
  console.error('Or set: SOLANA_WALLET_KEYPAIR_PATH=/path/to/keypair.json');
  process.exit(1);
}

console.log(`✅ Using keypair: ${keypairPath}`);

// Read keypair to get wallet address
let walletAddress = '';
try {
  const keypair = JSON.parse(fs.readFileSync(keypairPath, 'utf-8'));
  // Solana keypairs can be in different formats
  if (keypair.publicKey) {
    walletAddress = keypair.publicKey;
  } else if (Array.isArray(keypair) && keypair.length > 0) {
    // Solana keypair is often an array of numbers
    const { PublicKey } = require('@solana/web3.js');
    // This would require @solana/web3.js to be installed
    console.log('⚠️  Keypair format detected. Wallet address will be derived during deployment.');
  }
} catch (error) {
  console.warn(`⚠️  Could not parse keypair: ${error.message}`);
}

// Update environment
process.env.SOLANA_WALLET_KEYPAIR_PATH = keypairPath;
if (walletAddress) {
  process.env.ISSUER_WALLET = walletAddress;
  process.env.PLATFORM_WALLET = walletAddress;
  console.log(`💼 Wallet Address: ${walletAddress}`);
}

// Set OASIS API URL to local
process.env.OASIS_API_URL = process.env.OASIS_API_URL || 'http://localhost:5003';
process.env.SMART_CONTRACT_GENERATOR_URL = process.env.SMART_CONTRACT_GENERATOR_URL || 'http://localhost:5000';

console.log(`\n📋 Configuration:`);
console.log(`   OASIS API: ${process.env.OASIS_API_URL}`);
console.log(`   SmartContractGenerator: ${process.env.SMART_CONTRACT_GENERATOR_URL}`);
console.log(`   Keypair: ${keypairPath}`);
console.log(`   Wallet: ${walletAddress || 'Will be derived'}`);

console.log('\n🚀 Ready to deploy contracts!');
console.log('   The contracts will be deployed using the provided keypair.');
console.log('   Run the NestJS backend and use the API endpoints, or');
console.log('   use the deployment script once the backend is running.\n');

// Save to .env file
const envContent = `
# Contract Deployment Configuration
SOLANA_WALLET_KEYPAIR_PATH=${keypairPath}
${walletAddress ? `ISSUER_WALLET=${walletAddress}` : '# ISSUER_WALLET=your-wallet-address'}
${walletAddress ? `PLATFORM_WALLET=${walletAddress}` : '# PLATFORM_WALLET=your-wallet-address'}
OASIS_API_URL=${process.env.OASIS_API_URL}
SMART_CONTRACT_GENERATOR_URL=${process.env.SMART_CONTRACT_GENERATOR_URL}
`;

const envPath = path.join(process.cwd(), '.env.deployment');
fs.writeFileSync(envPath, envContent);
console.log(`💾 Configuration saved to: ${envPath}`);
console.log(`\n✅ Next steps:`);
console.log(`   1. Start NestJS backend: npm run start:dev`);
console.log(`   2. Authenticate via: POST /api/auth/login`);
console.log(`   3. Deploy contracts via: POST /smart-contracts/deploy-*`);












