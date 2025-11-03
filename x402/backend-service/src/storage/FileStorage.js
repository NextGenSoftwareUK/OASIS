/**
 * x402 Storage Utilities
 * 
 * File-based storage for x402 revenue distribution
 * Follows existing MetaBricks storage pattern
 */

const fs = require('fs').promises;
const path = require('path');

// Storage file paths
const X402_CONFIG_FILE = path.join(__dirname, 'x402-config.json');
const X402_DISTRIBUTIONS_FILE = path.join(__dirname, 'x402-distributions.json');

/**
 * Read data from a JSON file
 */
async function readJsonFile(filePath) {
  try {
    const data = await fs.readFile(filePath, 'utf8');
    return JSON.parse(data);
  } catch (error) {
    if (error.code === 'ENOENT') {
      return { data: [], lastUpdated: null };
    }
    throw error;
  }
}

/**
 * Write data to a JSON file
 */
async function writeJsonFile(filePath, data) {
  await fs.writeFile(filePath, JSON.stringify(data, null, 2), 'utf8');
}

/**
 * Store x402 configuration for an NFT
 */
async function storeX402Config(nftMintAddress, x402Config) {
  try {
    const storage = await readJsonFile(X402_CONFIG_FILE);
    
    // Find existing or create new
    const existingIndex = storage.data.findIndex(item => item.nftMintAddress === nftMintAddress);
    
    const configData = {
      nftMintAddress,
      ...x402Config,
      createdAt: existingIndex >= 0 ? storage.data[existingIndex].createdAt : new Date().toISOString(),
      updatedAt: new Date().toISOString()
    };
    
    if (existingIndex >= 0) {
      storage.data[existingIndex] = configData;
    } else {
      storage.data.push(configData);
    }
    
    storage.lastUpdated = new Date().toISOString();
    
    await writeJsonFile(X402_CONFIG_FILE, storage);
    
    console.log(`✅ Stored x402 config for ${nftMintAddress}`);
    return true;
    
  } catch (error) {
    console.error('Error storing x402 config:', error);
    throw error;
  }
}

/**
 * Get x402 configuration for an NFT
 */
async function getX402Config(nftMintAddress) {
  try {
    const storage = await readJsonFile(X402_CONFIG_FILE);
    const config = storage.data.find(item => item.nftMintAddress === nftMintAddress);
    return config || null;
    
  } catch (error) {
    console.error('Error fetching x402 config:', error);
    return null;
  }
}

/**
 * Record a distribution event
 */
async function recordX402Distribution(distribution) {
  try {
    const storage = await readJsonFile(X402_DISTRIBUTIONS_FILE);
    
    const distributionData = {
      id: Date.now().toString(),
      ...distribution,
      createdAt: new Date().toISOString()
    };
    
    storage.data.push(distributionData);
    storage.lastUpdated = new Date().toISOString();
    
    await writeJsonFile(X402_DISTRIBUTIONS_FILE, storage);
    
    console.log(`📊 Recorded distribution for ${distribution.nftMintAddress}`);
    return true;
    
  } catch (error) {
    console.error('Error recording distribution:', error);
    throw error;
  }
}

/**
 * Get distribution history for an NFT
 */
async function getX402Distributions(nftMintAddress, limit = 10) {
  try {
    const storage = await readJsonFile(X402_DISTRIBUTIONS_FILE);
    
    const distributions = storage.data
      .filter(d => d.nftMintAddress === nftMintAddress)
      .sort((a, b) => b.timestamp - a.timestamp)
      .slice(0, limit);
    
    return distributions;
    
  } catch (error) {
    console.error('Error fetching distributions:', error);
    return [];
  }
}

/**
 * Get all NFTs with x402 enabled
 */
async function getAllX402EnabledNFTs() {
  try {
    const storage = await readJsonFile(X402_CONFIG_FILE);
    return storage.data.filter(item => item.enabled === true);
    
  } catch (error) {
    console.error('Error fetching x402 NFTs:', error);
    return [];
  }
}

class FileStorage {
  constructor(dataDir) {
    this.dataDir = dataDir || path.join(__dirname, '../../data');
    this.X402_CONFIG_FILE = path.join(this.dataDir, 'x402-config.json');
    this.X402_DISTRIBUTIONS_FILE = path.join(this.dataDir, 'x402-distributions.json');
  }

  async storeConfig(nftMintAddress, x402Config) {
    return await storeX402Config(nftMintAddress, x402Config);
  }

  async getConfig(nftMintAddress) {
    return await getX402Config(nftMintAddress);
  }

  async recordDistribution(distribution) {
    return await recordX402Distribution(distribution);
  }

  async getDistributions(nftMintAddress, limit) {
    return await getX402Distributions(nftMintAddress, limit);
  }

  async getAllEnabledNFTs() {
    return await getAllX402EnabledNFTs();
  }
}

module.exports = FileStorage;

