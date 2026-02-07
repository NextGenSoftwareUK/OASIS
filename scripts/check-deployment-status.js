#!/usr/bin/env node

/**
 * OASIS Contract Deployment Status Checker
 * 
 * This script checks the deployment status of OASIS contracts across all chains
 * by reading OASIS_DNA.json and optionally verifying on-chain.
 */

const fs = require('fs');
const path = require('path');

// ANSI colors for terminal output
const colors = {
    reset: '\x1b[0m',
    red: '\x1b[31m',
    green: '\x1b[32m',
    yellow: '\x1b[33m',
    blue: '\x1b[34m',
    cyan: '\x1b[36m'
};

function colorize(text, color) {
    return `${colors[color]}${text}${colors.reset}`;
}

// Read OASIS_DNA.json
function readDNA() {
    const dnaPath = path.join(__dirname, '..', 'OASIS Architecture', 'NextGenSoftware.OASIS.API.DNA', 'OASIS_DNA.json');
    try {
        const content = fs.readFileSync(dnaPath, 'utf8');
        return JSON.parse(content);
    } catch (error) {
        console.error(colorize('❌ Error reading OASIS_DNA.json:', 'red'), error.message);
        process.exit(1);
    }
}

// Check if address is empty or placeholder
function isEmptyAddress(address) {
    if (!address) return true;
    if (typeof address === 'string') {
        const trimmed = address.trim();
        return trimmed === '' || 
               trimmed === '0x0000000000000000000000000000000000000000' ||
               trimmed === '0x0' ||
               trimmed.startsWith('0x') && trimmed.length === 42 && /^0x0+$/.test(trimmed);
    }
    return false;
}

// Check deployment status
function checkDeploymentStatus() {
    const dna = readDNA();
    const providers = dna.OASIS?.StorageProviders || {};
    
    console.log(colorize('\n=== OASIS Contract Deployment Status ===\n', 'cyan'));
    
    const status = {
        deployed: [],
        notDeployed: [],
        noContractNeeded: []
    };
    
    // EVM chains that need contracts
    const evmChains = [
        'EthereumOASIS', 'ArbitrumOASIS', 'OptimismOASIS', 'BaseOASIS',
        'PolygonOASIS', 'BNBChainOASIS', 'FantomOASIS', 'AvalancheOASIS',
        'RootstockOASIS', 'TRONOASIS', 'ZkSyncOASIS', 'LineaOASIS', 'ScrollOASIS'
    ];
    
    // Chains that don't need contracts
    const noContractChains = [
        'BitcoinOASIS', 'CardanoOASIS', 'XRPLOASIS'
    ];
    
    // Move chains
    const moveChains = ['AptosOASIS', 'SuiOASIS'];
    
    // Other chains
    const otherChains = [
        'CosmosBlockChainOASIS', 'PolkadotOASIS', 'SolanaOASIS',
        'NEAROASIS', 'EOSIOOASIS', 'HashgraphOASIS'
    ];
    
    // Check EVM chains
    evmChains.forEach(provider => {
        const config = providers[provider];
        if (!config) {
            status.notDeployed.push({ provider, reason: 'Not configured in DNA' });
            return;
        }
        
        const address = config.ContractAddress || config.contractAddress || '';
        if (isEmptyAddress(address)) {
            status.notDeployed.push({ provider, address: 'empty' });
        } else {
            status.deployed.push({ provider, address });
        }
    });
    
    // Check Move chains
    moveChains.forEach(provider => {
        const config = providers[provider];
        if (!config) {
            status.notDeployed.push({ provider, reason: 'Not configured in DNA' });
            return;
        }
        
        const address = config.ContractAddress || config.RpcEndpoint || '';
        if (isEmptyAddress(address) || !address.includes('::')) {
            status.notDeployed.push({ provider, address: 'empty or invalid format' });
        } else {
            status.deployed.push({ provider, address });
        }
    });
    
    // Check other chains
    otherChains.forEach(provider => {
        const config = providers[provider];
        if (!config) {
            status.notDeployed.push({ provider, reason: 'Not configured in DNA' });
            return;
        }
        
        const address = config.ContractAddress || config.ProgramId || config.AccountName || '';
        if (isEmptyAddress(address)) {
            status.notDeployed.push({ provider, address: 'empty' });
        } else {
            status.deployed.push({ provider, address });
        }
    });
    
    // Mark no-contract chains
    noContractChains.forEach(provider => {
        const config = providers[provider];
        if (config) {
            status.noContractNeeded.push({ provider, note: 'Uses native storage' });
        }
    });
    
    // Print results
    console.log(colorize('✅ DEPLOYED:', 'green'));
    if (status.deployed.length === 0) {
        console.log('  (none)');
    } else {
        status.deployed.forEach(({ provider, address }) => {
            console.log(`  ${provider.padEnd(30)} ${address}`);
        });
    }
    
    console.log(colorize('\n❌ NOT DEPLOYED:', 'red'));
    if (status.notDeployed.length === 0) {
        console.log('  (none)');
    } else {
        status.notDeployed.forEach(({ provider, address, reason }) => {
            const reasonText = reason || `ContractAddress: ${address}`;
            console.log(`  ${provider.padEnd(30)} ${reasonText}`);
        });
    }
    
    console.log(colorize('\nℹ️  NO CONTRACT NEEDED:', 'blue'));
    if (status.noContractNeeded.length === 0) {
        console.log('  (none)');
    } else {
        status.noContractNeeded.forEach(({ provider, note }) => {
            console.log(`  ${provider.padEnd(30)} ${note}`);
        });
    }
    
    // Summary
    console.log(colorize('\n=== Summary ===', 'cyan'));
    console.log(`Total Providers: ${evmChains.length + moveChains.length + otherChains.length + noContractChains.length}`);
    console.log(colorize(`✅ Deployed: ${status.deployed.length}`, 'green'));
    console.log(colorize(`❌ Not Deployed: ${status.notDeployed.length}`, 'red'));
    console.log(colorize(`ℹ️  No Contract Needed: ${status.noContractNeeded.length}`, 'blue'));
    
    const deploymentPercentage = ((status.deployed.length / (evmChains.length + moveChains.length + otherChains.length)) * 100).toFixed(1);
    console.log(colorize(`\nDeployment Progress: ${deploymentPercentage}%`, deploymentPercentage > 50 ? 'green' : 'yellow'));
    
    // Recommendations
    if (status.notDeployed.length > 0) {
        console.log(colorize('\n📋 Next Steps:', 'yellow'));
        console.log('1. Review CONTRACT_DEPLOYMENT.md for deployment instructions');
        console.log('2. Deploy contracts to testnet first');
        console.log('3. Update OASIS_DNA.json with deployed addresses');
        console.log('4. Verify contracts on block explorers');
        console.log('5. Run integration tests');
    }
    
    console.log('');
}

// Run the check
checkDeploymentStatus();


