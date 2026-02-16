const TwitterOASISProvider = require('./providers/TwitterOASISProvider');

/**
 * TwitterService - Manages Twitter integration for MetaBricks
 * Handles hashtag monitoring and thermometer updates
 */
class TwitterService {
  constructor() {
    this.provider = null;
    this.thermometerService = null;
    this.isInitialized = false;
  }

  /**
   * Initialize the Twitter service
   */
  async initialize(config) {
    try {
      console.log('🐦 Initializing Twitter service...');
      
      // Create Twitter provider
      this.provider = new TwitterOASISProvider({
        bearerToken: config.bearerToken,
        webhookUrl: config.webhookUrl,
        hashtag: config.hashtag || '#billionshealed',
        thermometerService: this,
        tweetsPerThermometerLevel: config.tweetsPerThermometerLevel || 10
      });

      this.isInitialized = true;
      console.log('✅ Twitter service initialized');
      
      return { success: true };
    } catch (error) {
      console.error('❌ Twitter service initialization failed:', error.message);
      return { success: false, error: error.message };
    }
  }

  /**
   * Start monitoring hashtag tweets
   */
  async startMonitoring() {
    if (!this.isInitialized) {
      throw new Error('Twitter service not initialized');
    }

    const result = await this.provider.activateProvider();
    if (result.success) {
      console.log('🐦 Twitter hashtag monitoring started');
    }
    
    return result;
  }

  /**
   * Stop monitoring
   */
  stopMonitoring() {
    if (this.provider) {
      this.provider.deactivateProvider();
      console.log('🛑 Twitter hashtag monitoring stopped');
    }
  }

  /**
   * Set thermometer service reference
   */
  setThermometerService(thermometerService) {
    this.thermometerService = thermometerService;
    if (this.provider) {
      this.provider.thermometerService = thermometerService;
    }
  }

  /**
   * Mint thermometer NFT from tweet (called by Twitter provider)
   */
  async mintThermometerFromTweet(tweetData) {
    try {
      if (!this.thermometerService) {
        console.warn('⚠️ Thermometer service not available');
        return { success: false, error: 'Thermometer service not available' };
      }

      // Call thermometer service to mint NFT
      const result = await this.thermometerService.mintThermometerFromSocialMedia({
        source: 'twitter',
        tweetId: tweetData.tweetId,
        tweetText: tweetData.tweetText,
        impact: tweetData.impact,
        timestamp: tweetData.timestamp
      });

      return result;
    } catch (error) {
      console.error('❌ Error minting thermometer from tweet:', error.message);
      return { success: false, error: error.message };
    }
  }

  /**
   * Get service status
   */
  getStatus() {
    return {
      isInitialized: this.isInitialized,
      provider: this.provider?.getStatus() || null,
      thermometerService: this.thermometerService ? 'Connected' : 'Not connected'
    };
  }

  /**
   * Manual hashtag check (for testing)
   */
  async manualHashtagCheck() {
    if (!this.provider) {
      throw new Error('Twitter provider not initialized');
    }

    return await this.provider.checkForNewTweets();
  }

  /**
   * Get recent tweets with hashtag
   */
  async getRecentTweets(limit = 10) {
    try {
      const response = await this.provider.testTwitterConnection();
      return {
        success: true,
        tweets: response.data || [],
        meta: response.meta || {}
      };
    } catch (error) {
      console.error('❌ Error fetching recent tweets:', error.message);
      // Re-throw the error so the server can handle it and provide mock data
      throw error;
    }
  }
}

module.exports = TwitterService;
