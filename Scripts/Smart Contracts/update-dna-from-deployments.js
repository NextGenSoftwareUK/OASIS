#!/usr/bin/env node

/**
 * Update OASIS_DNA.json with deployed contract addresses
 * Reads from deployed-addresses.json and updates OASIS_DNA.json
 */

const fs = require('fs');
const path = require('path');

const DNA_PATH = path.join(__dirname, '..', 'OASIS Architecture', 'NextGenSoftware.OASIS.API.DNA', 'OASIS_DNA.json');
const DEPLOYED_PATH = path.join(__dirname, '..', 'deployed-addresses.json');

// Mapping from deployment keys to DNA provider keys
const PROVIDER_MAP = {
    'ethereum': 'EthereumOASIS',
    'arbitrum': 'ArbitrumOASIS',
    'optimism': 'OptimismOASIS',
    'base': 'BaseOASIS',
    'polygon': 'PolygonOASIS',
    'bnb': 'BNBChainOASIS',
    'fantom': 'FantomOASIS',
    'avalanche': 'AvalancheOASIS',
    'rootstock': 'RootstockOASIS',
    'zkSync': 'ZkSyncOASIS',
    'linea': 'LineaOASIS',
    'scroll': 'ScrollOASIS',
    'AptosOASIS': 'AptosOASIS',
    'SuiOASIS': 'SuiOASIS'
};

function loadJSON(filePath) {
    if (!fs.existsSync(filePath)) {
        return {};
    }
    try {
        return JSON.parse(fs.readFileSync(filePath, 'utf8'));
    } catch (error) {
        console.error(`Error reading ${filePath}:`, error.message);
        return {};
    }
}

function saveJSON(filePath, data) {
    fs.writeFileSync(filePath, JSON.stringify(data, null, 4));
}

function updateDNA() {
    console.log('📝 Updating OASIS_DNA.json with deployed addresses...\n');
    
    const dna = loadJSON(DNA_PATH);
    const deployed = loadJSON(DEPLOYED_PATH);
    
    if (Object.keys(deployed).length === 0) {
        console.log('⚠️  No deployed addresses found in deployed-addresses.json');
        return;
    }
    
    const providers = dna.OASIS?.StorageProviders || {};
    let updated = 0;
    
    // Update EVM chains
    Object.keys(deployed).forEach(chainKey => {
        const providerKey = PROVIDER_MAP[chainKey] || chainKey;
        const chainDeployments = deployed[chainKey];
        
        if (!chainDeployments || typeof chainDeployments !== 'object') {
            return;
        }
        
        // Check both mainnet and testnet
        ['mainnet', 'testnet'].forEach(networkType => {
            const deployment = chainDeployments[networkType];
            if (!deployment || !deployment.address) {
                return;
            }
            
            if (!providers[providerKey]) {
                providers[providerKey] = {};
            }
            
            // For mainnet, update ContractAddress
            if (networkType === 'mainnet') {
                const oldAddress = providers[providerKey].ContractAddress || '';
                if (oldAddress !== deployment.address) {
                    providers[providerKey].ContractAddress = deployment.address;
                    console.log(`✅ Updated ${providerKey} mainnet: ${deployment.address}`);
                    updated++;
                }
            }
            
            // Update RPC endpoint if not set
            if (!providers[providerKey].RpcEndpoint && deployment.explorer) {
                // Extract RPC from explorer or use default
                const rpcMap = {
                    'EthereumOASIS': 'https://eth.llamarpc.com',
                    'ArbitrumOASIS': 'https://arb1.arbitrum.io/rpc',
                    'OptimismOASIS': 'https://mainnet.optimism.io',
                    'BaseOASIS': 'https://mainnet.base.org',
                    'PolygonOASIS': 'https://polygon-rpc.com',
                    'BNBChainOASIS': 'https://bsc-dataseed.binance.org',
                    'FantomOASIS': 'https://rpc.ftm.tools',
                    'AvalancheOASIS': 'https://api.avax.network/ext/bc/C/rpc',
                    'ZkSyncOASIS': 'https://mainnet.era.zksync.io',
                    'LineaOASIS': 'https://rpc.linea.build',
                    'ScrollOASIS': 'https://rpc.scroll.io'
                };
                if (rpcMap[providerKey]) {
                    providers[providerKey].RpcEndpoint = rpcMap[providerKey];
                }
            }
        });
    });
    
    // Update Move chains
    if (deployed.AptosOASIS) {
        const aptosMainnet = deployed.AptosOASIS.mainnet;
        const aptosTestnet = deployed.AptosOASIS.testnet;
        
        if (aptosMainnet && aptosMainnet.address) {
            if (!providers.AptosOASIS) providers.AptosOASIS = {};
            providers.AptosOASIS.ContractAddress = aptosMainnet.accountAddress || aptosMainnet.address;
            providers.AptosOASIS.RpcEndpoint = 'https://fullnode.mainnet.aptoslabs.com';
            providers.AptosOASIS.Network = 'mainnet';
            console.log(`✅ Updated AptosOASIS mainnet: ${providers.AptosOASIS.ContractAddress}`);
            updated++;
        }
    }
    
    if (deployed.SuiOASIS) {
        const suiMainnet = deployed.SuiOASIS.mainnet;
        const suiTestnet = deployed.SuiOASIS.testnet;
        
        if (suiMainnet && suiMainnet.packageId) {
            if (!providers.SuiOASIS) providers.SuiOASIS = {};
            providers.SuiOASIS.ContractAddress = suiMainnet.packageId;
            providers.SuiOASIS.RpcEndpoint = 'https://fullnode.mainnet.sui.io:443';
            providers.SuiOASIS.Network = 'mainnet';
            console.log(`✅ Updated SuiOASIS mainnet: ${providers.SuiOASIS.ContractAddress}`);
            updated++;
        }
    }
    
    // Save updated DNA
    dna.OASIS.StorageProviders = providers;
    saveJSON(DNA_PATH, dna);
    
    console.log(`\n✅ Updated ${updated} provider(s) in OASIS_DNA.json`);
    console.log(`📄 DNA file: ${DNA_PATH}`);
}

// Run update
updateDNA();


