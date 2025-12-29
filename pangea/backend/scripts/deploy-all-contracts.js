/**
 * Deploy all 4 smart contracts to Solana devnet
 * This script directly calls the SmartContractGenerator API
 */

const axios = require('axios');
const FormData = require('form-data');
const fs = require('fs');
const path = require('path');

const SMART_CONTRACT_GENERATOR_URL = process.env.SMART_CONTRACT_GENERATOR_URL || 'http://localhost:5000';
const KEYPAIR_PATH = process.env.SOLANA_WALLET_KEYPAIR_PATH || path.join(process.env.HOME, '.config', 'solana', 'id.json');
const WALLET_ADDRESS = process.env.ISSUER_WALLET || 'FQsRrE7pXHJg5jftcWUzqcHvUfk8AQoUviijWuiD4JFn';

console.log('🚀 Starting Smart Contract Deployment to Solana Devnet\n');
console.log(`📋 Configuration:`);
console.log(`   SmartContractGenerator: ${SMART_CONTRACT_GENERATOR_URL}`);
console.log(`   Keypair: ${KEYPAIR_PATH}`);
console.log(`   Wallet: ${WALLET_ADDRESS}`);
console.log(`   Network: Devnet\n`);

// Verify keypair exists
if (!fs.existsSync(KEYPAIR_PATH)) {
  console.error(`❌ Keypair file not found: ${KEYPAIR_PATH}`);
  process.exit(1);
}

// Contract specifications
const contracts = [
  {
    name: 'RWA Token',
    spec: {
      contract_type: 'rwa_token',
      blockchain: 'solana',
      language: 'Rust',
      framework: 'Anchor',
      spec: {
        name: 'Pangea RWA Token',
        symbol: 'PRWA',
        total_supply: 1000000,
        decimals: 0,
        metadata_uri: 'https://pangea.rkund.com/metadata/rwa-token.json',
        features: ['mint', 'burn', 'transfer', 'freeze'],
        authority: WALLET_ADDRESS,
        freeze_authority: WALLET_ADDRESS,
      },
    },
  },
  {
    name: 'Order Book',
    spec: {
      contract_type: 'order_book',
      blockchain: 'solana',
      language: 'Rust',
      framework: 'Anchor',
      spec: {
        features: [
          'create_buy_order',
          'create_sell_order',
          'cancel_order',
          'match_orders',
          'get_order_book',
          'get_user_orders',
          'get_order_by_id',
        ],
        order_types: ['limit', 'market'],
        matching_algorithm: 'price_time_priority',
      },
    },
  },
  {
    name: 'Trade Execution',
    spec: {
      contract_type: 'trade_execution',
      blockchain: 'solana',
      language: 'Rust',
      framework: 'Anchor',
      spec: {
        features: [
          'execute_trade',
          'settle_trade',
          'get_trade_history',
          'calculate_fees',
        ],
        fee_structure: {
          platform_fee_percentage: 0.01,
          fee_recipient: WALLET_ADDRESS,
        },
      },
    },
  },
  {
    name: 'Vault',
    spec: {
      contract_type: 'vault',
      blockchain: 'solana',
      language: 'Rust',
      framework: 'Anchor',
      spec: {
        features: [
          'deposit',
          'withdraw',
          'get_balance',
          'transfer_to_order',
          'lock_balance',
          'unlock_balance',
        ],
        supported_tokens: ['USDC', 'SOL'],
      },
    },
  },
];

async function generateContract(spec, contractName) {
  console.log(`\n📦 [${contractName}] Step 1/3: Generating contract...`);
  
  const formData = new FormData();
  const jsonBuffer = Buffer.from(JSON.stringify(spec, null, 2));
  formData.append('JsonFile', jsonBuffer, {
    filename: 'spec.json',
    contentType: 'application/json',
  });
  formData.append('Language', 'Rust');

  try {
    const response = await axios.post(
      `${SMART_CONTRACT_GENERATOR_URL}/api/v1/contracts/generate`,
      formData,
      {
        headers: formData.getHeaders(),
        responseType: 'arraybuffer',
        timeout: 300000, // 5 minutes
      },
    );

    console.log(`✅ [${contractName}] Contract generated`);
    return Buffer.from(response.data);
  } catch (error) {
    console.error(`❌ [${contractName}] Generation failed:`, error.message);
    if (error.response) {
      console.error('Response:', error.response.data.toString());
    }
    throw error;
  }
}

async function compileContract(generatedBlob, contractName) {
  console.log(`📦 [${contractName}] Step 2/3: Compiling contract...`);
  console.log(`⏳ This may take 20+ minutes for first build (downloading Rust dependencies)...`);

  const formData = new FormData();
  formData.append('Source', generatedBlob, {
    filename: 'contract.zip',
    contentType: 'application/zip',
  });
  formData.append('Language', 'Rust');

  try {
      const response = await axios.post(
        `${SMART_CONTRACT_GENERATOR_URL}/api/v1/contracts/compile`,
        formData,
        {
          headers: formData.getHeaders(),
          responseType: 'arraybuffer',
          timeout: 1800000, // 30 minutes for first build
          maxContentLength: Infinity,
          maxBodyLength: Infinity,
        },
      );

    console.log(`✅ [${contractName}] Contract compiled`);
    return Buffer.from(response.data);
  } catch (error) {
    console.error(`❌ [${contractName}] Compilation failed:`, error.message);
    if (error.code === 'ECONNABORTED') {
      console.error('⏱️  Compilation timeout. The API may be under heavy load.');
    }
    if (error.response) {
      let errorData = error.response.data;
      if (Buffer.isBuffer(errorData)) {
        errorData = errorData.toString();
      }
      if (typeof errorData === 'string') {
        try {
          const parsed = JSON.parse(errorData);
          console.error('Full Error Response:', JSON.stringify(parsed, null, 2));
        } catch (e) {
          console.error('Error Response (raw):', errorData.substring(0, 2000));
        }
      } else {
        console.error('Error Response:', JSON.stringify(errorData, null, 2));
      }
    }
    throw error;
  }
}

async function deployContract(compiledBlob, contractName) {
  console.log(`📦 [${contractName}] Step 3/3: Deploying to Solana devnet...`);

  const formData = new FormData();
  formData.append('Language', 'Rust');
  formData.append('CompiledContractFile', compiledBlob, {
    filename: 'program.so',
    contentType: 'application/octet-stream',
  });

  // Add wallet keypair
  const keypairContent = fs.readFileSync(KEYPAIR_PATH, 'utf-8');
  const keypairBuffer = Buffer.from(keypairContent);
  formData.append('WalletKeypair', keypairBuffer, {
    filename: 'wallet.json',
    contentType: 'application/json',
  });

  // Schema is required
  const schemaContent = '{}';
  formData.append('Schema', Buffer.from(schemaContent), {
    filename: 'schema.json',
    contentType: 'application/json',
  });

  try {
    const response = await axios.post(
      `${SMART_CONTRACT_GENERATOR_URL}/api/v1/contracts/deploy`,
      formData,
      {
        headers: formData.getHeaders(),
        timeout: 300000, // 5 minutes
      },
    );

    const result = response.data;
    const contractAddress = result.contractAddress || result.programId || '';
    const txHash = result.transactionHash || result.signature || '';

    console.log(`✅ [${contractName}] Deployed successfully!`);
    console.log(`   Contract Address: ${contractAddress}`);
    if (txHash) {
      console.log(`   Transaction: ${txHash}`);
    }

    return {
      name: contractName,
      success: true,
      contractAddress,
      transactionHash: txHash,
    };
  } catch (error) {
    console.error(`❌ [${contractName}] Deployment failed:`, error.message);
    if (error.response) {
      console.error('Response:', JSON.stringify(error.response.data, null, 2));
    }
    return {
      name: contractName,
      success: false,
      error: error.message,
    };
  }
}

async function deployAllContracts() {
  const results = [];

  for (const contract of contracts) {
    try {
      console.log(`\n${'='.repeat(60)}`);
      console.log(`🚀 Deploying: ${contract.name}`);
      console.log('='.repeat(60));

      // Generate
      const generated = await generateContract(contract.spec, contract.name);

      // Compile
      const compiled = await compileContract(generated, contract.name);

      // Deploy
      const result = await deployContract(compiled, contract.name);
      results.push(result);

      if (result.success) {
        console.log(`\n✅ ${contract.name} deployment complete!`);
      } else {
        console.log(`\n❌ ${contract.name} deployment failed`);
      }
    } catch (error) {
      console.error(`\n❌ ${contract.name} failed:`, error.message);
      results.push({
        name: contract.name,
        success: false,
        error: error.message,
      });
    }
  }

  // Summary
  console.log(`\n${'='.repeat(60)}`);
  console.log('📊 Deployment Summary');
  console.log('='.repeat(60));
  
  results.forEach((result) => {
    if (result.success) {
      console.log(`✅ ${result.name}:`);
      console.log(`   Address: ${result.contractAddress}`);
      if (result.transactionHash) {
        console.log(`   TX: ${result.transactionHash}`);
      }
    } else {
      console.log(`❌ ${result.name}: ${result.error}`);
    }
  });

  const successCount = results.filter((r) => r.success).length;
  console.log(`\n✅ Successfully deployed: ${successCount}/4 contracts`);

  // Save results
  const resultsPath = path.join(process.cwd(), 'deployment-results.json');
  fs.writeFileSync(resultsPath, JSON.stringify(results, null, 2));
  console.log(`\n💾 Results saved to: ${resultsPath}`);

  return results;
}

// Run deployment
deployAllContracts()
  .then((results) => {
    const successCount = results.filter((r) => r.success).length;
    if (successCount < 4) {
      console.log('\n⚠️  Some contracts failed to deploy. Check logs above.');
      process.exit(1);
    } else {
      console.log('\n🎉 All contracts deployed successfully!');
      process.exit(0);
    }
  })
  .catch((error) => {
    console.error('\n💥 Fatal error:', error);
    process.exit(1);
  });












