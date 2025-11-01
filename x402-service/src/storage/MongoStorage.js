/**
 * MongoDB Storage Adapter for x402 Service
 * 
 * Stores NFT configurations and distribution history in MongoDB
 */

class MongoStorage {
  constructor(options = {}) {
    this.options = {
      url: options.url || process.env.MONGODB_URL || 'mongodb://localhost:27017',
      database: options.database || process.env.MONGODB_DATABASE || 'oasis_x402',
      ...options
    };

    this.client = null;
    this.db = null;
    this.connected = false;
  }

  /**
   * Connect to MongoDB
   */
  async connect() {
    if (this.connected) return;

    try {
      const { MongoClient } = require('mongodb');
      this.client = new MongoClient(this.options.url);
      await this.client.connect();
      this.db = this.client.db(this.options.database);
      this.connected = true;
      console.log(`✅ Connected to MongoDB: ${this.options.database}`);
    } catch (error) {
      console.error('❌ MongoDB connection failed:', error);
      throw error;
    }
  }

  /**
   * Ensure connection
   */
  async ensureConnected() {
    if (!this.connected) {
      await this.connect();
    }
  }

  /**
   * Store x402 configuration for an NFT
   */
  async storeConfig(nftMintAddress, x402Config) {
    await this.ensureConnected();

    const collection = this.db.collection('x402_configs');
    
    const document = {
      nftMintAddress,
      ...x402Config,
      createdAt: new Date(),
      updatedAt: new Date()
    };

    await collection.updateOne(
      { nftMintAddress },
      { $set: document },
      { upsert: true }
    );

    console.log(`✅ Stored x402 config in MongoDB: ${nftMintAddress}`);
    return true;
  }

  /**
   * Get x402 configuration for an NFT
   */
  async getConfig(nftMintAddress) {
    await this.ensureConnected();

    const collection = this.db.collection('x402_configs');
    const config = await collection.findOne({ nftMintAddress });

    return config || null;
  }

  /**
   * Record a distribution event
   */
  async recordDistribution(distribution) {
    await this.ensureConnected();

    const collection = this.db.collection('x402_distributions');
    
    const document = {
      ...distribution,
      createdAt: new Date()
    };

    await collection.insertOne(document);

    console.log(`📊 Recorded distribution in MongoDB: ${distribution.nftMintAddress}`);
    return true;
  }

  /**
   * Get distribution history for an NFT
   */
  async getDistributions(nftMintAddress, limit = 10) {
    await this.ensureConnected();

    const collection = this.db.collection('x402_distributions');
    
    const distributions = await collection
      .find({ nftMintAddress })
      .sort({ timestamp: -1 })
      .limit(limit)
      .toArray();

    return distributions;
  }

  /**
   * Get all NFTs with x402 enabled
   */
  async getAllEnabledNFTs() {
    await this.ensureConnected();

    const collection = this.db.collection('x402_configs');
    const nfts = await collection.find({ enabled: true }).toArray();

    return nfts;
  }

  /**
   * Close connection
   */
  async close() {
    if (this.client) {
      await this.client.close();
      this.connected = false;
      console.log('✅ MongoDB connection closed');
    }
  }
}

module.exports = MongoStorage;

