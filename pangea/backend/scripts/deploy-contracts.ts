/**
 * Script to deploy all 4 required smart contracts
 * Run with: npx ts-node scripts/deploy-contracts.ts
 */

import { SmartContractService } from '../src/smart-contracts/services/smart-contract.service';
import { Logger } from '@nestjs/common';

const logger = new Logger('DeployContracts');

async function deployAllContracts() {
  logger.log('🚀 Starting deployment of all 4 smart contracts...\n');

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

  if (successCount < 4) {
    process.exit(1);
  }
}

// Run the deployment
deployAllContracts().catch((error) => {
  logger.error('Fatal error:', error);
  process.exit(1);
});












