#!/usr/bin/env node

/**
 * OASIS EVM Chain Deployment Script
 * 
 * Usage:
 *   node deploy-evm-chain.js <chain-name> [network-type]
 * 
 * Examples:
 *   node deploy-evm-chain.js ethereum mainnet
 *   node deploy-evm-chain.js arbitrum testnet
 *   node deploy-evm-chain.js base mainnet
 */

const hre = require("hardhat");
const fs = require("fs");
const path = require("path");

// Chain configuration mapping
const CHAIN_CONFIG = {
  // Mainnet networks
  ethereum: { name: "Ethereum", network: "ethereum", chainId: 1, explorer: "etherscan.io" },
  arbitrum: { name: "Arbitrum", network: "arbitrum", chainId: 42161, explorer: "arbiscan.io" },
  optimism: { name: "Optimism", network: "optimism", chainId: 10, explorer: "optimistic.etherscan.io" },
  base: { name: "Base", network: "base", chainId: 8453, explorer: "basescan.org" },
  polygon: { name: "Polygon", network: "polygon", chainId: 137, explorer: "polygonscan.com" },
  bnb: { name: "BNB Chain", network: "bnb", chainId: 56, explorer: "bscscan.com" },
  fantom: { name: "Fantom", network: "fantom", chainId: 250, explorer: "ftmscan.com" },
  avalanche: { name: "Avalanche", network: "avalanche", chainId: 43114, explorer: "snowtrace.io" },
  rootstock: { name: "Rootstock", network: "rootstock", chainId: 30, explorer: "explorer.rsk.co" },
  zkSync: { name: "zkSync Era", network: "zkSync", chainId: 324, explorer: "explorer.zksync.io" },
  linea: { name: "Linea", network: "linea", chainId: 59144, explorer: "lineascan.build" },
  scroll: { name: "Scroll", network: "scroll", chainId: 534352, explorer: "scrollscan.com" },
  
  // Testnet networks
  sepolia: { name: "Ethereum Sepolia", network: "sepolia", chainId: 11155111, explorer: "sepolia.etherscan.io" },
  arbitrumSepolia: { name: "Arbitrum Sepolia", network: "arbitrumSepolia", chainId: 421614, explorer: "sepolia.arbiscan.io" },
  optimismSepolia: { name: "Optimism Sepolia", network: "optimismSepolia", chainId: 11155420, explorer: "sepolia-optimism.etherscan.io" },
  baseSepolia: { name: "Base Sepolia", network: "baseSepolia", chainId: 84532, explorer: "sepolia.basescan.org" },
  amoy: { name: "Polygon Amoy", network: "amoy", chainId: 80002, explorer: "amoy.polygonscan.com" },
  bnbTestnet: { name: "BNB Chain Testnet", network: "bnbTestnet", chainId: 97, explorer: "testnet.bscscan.com" },
  fantomTestnet: { name: "Fantom Testnet", network: "fantomTestnet", chainId: 4002, explorer: "testnet.ftmscan.com" },
  fuji: { name: "Avalanche Fuji", network: "fuji", chainId: 43113, explorer: "testnet.snowtrace.io" },
  rootstockTestnet: { name: "Rootstock Testnet", network: "rootstockTestnet", chainId: 31, explorer: "explorer.testnet.rsk.co" },
  zkSyncTestnet: { name: "zkSync Era Testnet", network: "zkSyncTestnet", chainId: 300, explorer: "sepolia.explorer.zksync.io" },
  lineaTestnet: { name: "Linea Testnet", network: "lineaTestnet", chainId: 59140, explorer: "goerli.lineascan.build" },
  scrollSepolia: { name: "Scroll Sepolia", network: "scrollSepolia", chainId: 534351, explorer: "sepolia.scrollscan.com" }
};

// Network type mapping
const NETWORK_TYPE_MAP = {
  mainnet: {
    ethereum: "ethereum",
    arbitrum: "arbitrum",
    optimism: "optimism",
    base: "base",
    polygon: "polygon",
    bnb: "bnb",
    fantom: "fantom",
    avalanche: "avalanche",
    rootstock: "rootstock",
    zkSync: "zkSync",
    linea: "linea",
    scroll: "scroll"
  },
  testnet: {
    ethereum: "sepolia",
    arbitrum: "arbitrumSepolia",
    optimism: "optimismSepolia",
    base: "baseSepolia",
    polygon: "amoy",
    bnb: "bnbTestnet",
    fantom: "fantomTestnet",
    avalanche: "fuji",
    rootstock: "rootstockTestnet",
    zkSync: "zkSyncTestnet",
    linea: "lineaTestnet",
    scroll: "scrollSepolia"
  }
};

function getDeploymentFilePath() {
  return path.join(__dirname, "..", "deployed-addresses.json");
}

function loadDeployedAddresses() {
  const filePath = getDeploymentFilePath();
  if (fs.existsSync(filePath)) {
    try {
      return JSON.parse(fs.readFileSync(filePath, "utf8"));
    } catch (error) {
      console.error("Error reading deployed-addresses.json:", error.message);
      return {};
    }
  }
  return {};
}

function saveDeployedAddress(address, chainKey, networkType, chainConfig) {
  const addresses = loadDeployedAddresses();
  
  if (!addresses[chainKey]) {
    addresses[chainKey] = {};
  }
  
  addresses[chainKey][networkType] = {
    chain: chainConfig.name,
    network: chainConfig.network,
    address: address,
    chainId: chainConfig.chainId,
    explorer: `https://${chainConfig.explorer}/address/${address}`,
    deployedAt: new Date().toISOString(),
    txHash: "" // Will be filled if available
  };
  
  fs.writeFileSync(getDeploymentFilePath(), JSON.stringify(addresses, null, 2));
  console.log(`✅ Saved deployment address to deployed-addresses.json`);
}

async function deployContract(chainConfig) {
  console.log(`\n🚀 Deploying OASIS contract to ${chainConfig.name}...`);
  console.log(`   Network: ${chainConfig.network}`);
  console.log(`   Chain ID: ${chainConfig.chainId}`);
  
  try {
    // Get deployer account
    const [deployer] = await hre.ethers.getSigners();
    console.log(`   Deploying with account: ${deployer.address}`);
    
    // Check balance
    const balance = await hre.ethers.provider.getBalance(deployer.address);
    console.log(`   Account balance: ${hre.ethers.formatEther(balance)} ETH`);
    
    if (balance === 0n) {
      throw new Error("Insufficient balance. Please fund the deployer account.");
    }
    
    // Deploy contract
    console.log(`\n   Deploying OASIS contract...`);
    const OASIS = await hre.ethers.getContractFactory("OASIS");
    const oasis = await OASIS.deploy();
    
    console.log(`   Waiting for deployment transaction...`);
    await oasis.waitForDeployment();
    
    const address = await oasis.getAddress();
    const deploymentTx = oasis.deploymentTransaction();
    const txHash = deploymentTx ? deploymentTx.hash : "";
    
    console.log(`\n✅ OASIS contract deployed successfully!`);
    console.log(`   Contract Address: ${address}`);
    console.log(`   Transaction Hash: ${txHash}`);
    console.log(`   Explorer: https://${chainConfig.explorer}/address/${address}`);
    if (txHash) {
      console.log(`   TX Explorer: https://${chainConfig.explorer}/tx/${txHash}`);
    }
    
    return { address, txHash };
  } catch (error) {
    console.error(`\n❌ Deployment failed:`, error.message);
    if (error.transaction) {
      console.error(`   Transaction:`, error.transaction);
    }
    throw error;
  }
}

async function verifyContract(address, chainConfig) {
  if (!process.env.ETHERSCAN_API_KEY && !process.env[`${chainConfig.network.toUpperCase()}_API_KEY`]) {
    console.log(`\n⚠️  Skipping verification (no API key configured)`);
    return;
  }
  
  console.log(`\n🔍 Verifying contract on ${chainConfig.explorer}...`);
  try {
    await hre.run("verify:verify", {
      address: address,
      constructorArguments: []
    });
    console.log(`✅ Contract verified successfully!`);
  } catch (error) {
    console.error(`⚠️  Verification failed:`, error.message);
  }
}

async function main() {
  const args = process.argv.slice(2);
  
  if (args.length < 1) {
    console.error("Usage: node deploy-evm-chain.js <chain-name> [network-type]");
    console.error("\nAvailable chains:");
    console.error("  Mainnet: ethereum, arbitrum, optimism, base, polygon, bnb, fantom, avalanche, rootstock, zkSync, linea, scroll");
    console.error("  Testnet: sepolia, arbitrumSepolia, optimismSepolia, baseSepolia, amoy, bnbTestnet, fantomTestnet, fuji, rootstockTestnet, zkSyncTestnet, lineaTestnet, scrollSepolia");
    console.error("\nExamples:");
    console.error("  node deploy-evm-chain.js ethereum mainnet");
    console.error("  node deploy-evm-chain.js arbitrum testnet");
    console.error("  node deploy-evm-chain.js baseSepolia");
    process.exit(1);
  }
  
  const chainName = args[0].toLowerCase();
  const networkType = args[1] ? args[1].toLowerCase() : null;
  
  // Determine network configuration
  let chainConfig;
  let chainKey;
  
  if (CHAIN_CONFIG[chainName]) {
    // Direct network name (e.g., "sepolia", "baseSepolia")
    chainConfig = CHAIN_CONFIG[chainName];
    chainKey = chainName;
  } else if (networkType && NETWORK_TYPE_MAP[networkType] && NETWORK_TYPE_MAP[networkType][chainName]) {
    // Chain name + network type (e.g., "ethereum mainnet", "arbitrum testnet")
    const networkName = NETWORK_TYPE_MAP[networkType][chainName];
    chainConfig = CHAIN_CONFIG[networkName];
    chainKey = chainName;
  } else {
    console.error(`❌ Unknown chain: ${chainName}`);
    console.error(`   Available chains: ${Object.keys(CHAIN_CONFIG).join(", ")}`);
    process.exit(1);
  }
  
  if (!chainConfig) {
    console.error(`❌ Could not determine network configuration for ${chainName} ${networkType || ""}`);
    process.exit(1);
  }
  
  // Check if already deployed
  const addresses = loadDeployedAddresses();
  const networkTypeKey = networkType || (chainName.includes("testnet") || chainName.includes("Sepolia") || chainName.includes("Amoy") || chainName.includes("Fuji") ? "testnet" : "mainnet");
  
  if (addresses[chainKey] && addresses[chainKey][networkTypeKey]) {
    console.log(`⚠️  Contract already deployed to ${chainConfig.name}:`);
    console.log(`   Address: ${addresses[chainKey][networkTypeKey].address}`);
    const overwrite = process.argv.includes("--overwrite");
    if (!overwrite) {
      console.log(`   Use --overwrite flag to redeploy`);
      process.exit(0);
    }
  }
  
  // Deploy
  try {
    const { address, txHash } = await deployContract(chainConfig);
    saveDeployedAddress(address, chainKey, networkTypeKey, chainConfig);
    
    // Verify (optional, can be slow)
    if (process.argv.includes("--verify")) {
      await verifyContract(address, chainConfig);
    }
    
    console.log(`\n✅ Deployment complete!`);
    console.log(`   Update OASIS_DNA.json with: "${address}"`);
  } catch (error) {
    console.error(`\n❌ Deployment failed:`, error);
    process.exit(1);
  }
}

main();


