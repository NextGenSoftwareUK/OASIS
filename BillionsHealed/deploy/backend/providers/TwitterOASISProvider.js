const axios = require('axios');

/**
 * TwitterOASIS Provider - Integrates Twitter API v2 with MetaBricks Thermometer
 * Monitors #billionshealed hashtag and updates thermometer temperature
 * Following OASIS provider pattern like TelegramOASIS
 */
class TwitterOASISProvider {
  constructor(config) {
    this.providerName = 'TwitterOASIS';
    this.providerDescription = 'Twitter Provider for hashtag monitoring and thermometer updates';
    
    // Twitter API v2 Configuration
    this.bearerToken = config.bearerToken;
    this.webhookUrl = config.webhookUrl;
    this.hashtag = config.hashtag || '#billionshealed';
    
    // Thermometer integration
    this.thermometerService = config.thermometerService;
    this.tweetsPerThermometerLevel = config.tweetsPerThermometerLevel || 10; // 10 tweets = 1 thermometer level
    
    // Rate limiting (adjusted for Essential tier)
    this.lastRequestTime = 0;
    this.requestInterval = 60000; // 60 seconds between requests (Essential tier limit)
    
    this.isActive = false;
  }

  /**
   * Activate the Twitter provider
   */
  async activateProvider() {
    try {
      console.log('🐦 Activating TwitterOASIS provider...');
      
      if (!this.bearerToken) {
        throw new Error('Twitter Bearer Token not configured');
      }

      // Test Twitter API connection
      await this.testTwitterConnection();
      
      // Start hashtag monitoring
      await this.startHashtagMonitoring();
      
      this.isActive = true;
      console.log('✅ TwitterOASIS provider activated successfully');
      
      return { success: true, message: 'TwitterOASIS provider activated' };
    } catch (error) {
      console.error('❌ TwitterOASIS activation failed:', error.message);
      return { success: false, error: error.message };
    }
  }

  /**
   * Test Twitter API connection
   */
  async testTwitterConnection() {
    try {
      const response = await axios.get('https://api.twitter.com/2/tweets/search/recent', {
        params: {
          query: this.hashtag,
          max_results: 10
        },
        headers: {
          'Authorization': `Bearer ${this.bearerToken}`,
          'Content-Type': 'application/json'
        }
      });
      
      console.log(`✅ Twitter API connection successful. Found ${response.data.meta?.result_count || 0} recent tweets`);
      return response.data;
    } catch (error) {
      console.error('❌ Twitter API connection failed:', error.response?.data || error.message);
      throw error;
    }
  }

  /**
   * Start monitoring hashtag tweets
   */
  async startHashtagMonitoring() {
    console.log(`🔍 Starting hashtag monitoring for ${this.hashtag}...`);
    
    // Set up interval for checking new tweets (adjusted for Essential tier)
    this.monitoringInterval = setInterval(async () => {
      await this.checkForNewTweets();
    }, 120000); // Check every 2 minutes (Essential tier limit)
    
    // Initial check
    await this.checkForNewTweets();
  }

  /**
   * Check for new tweets with the hashtag
   */
  async checkForNewTweets() {
    try {
      const now = Date.now();
      if (now - this.lastRequestTime < this.requestInterval) {
        return; // Rate limiting
      }
      
      this.lastRequestTime = now;
      
      const response = await axios.get('https://api.twitter.com/2/tweets/search/recent', {
        params: {
          query: `${this.hashtag} -is:retweet`,
          max_results: 10,
          'tweet.fields': 'created_at,author_id,public_metrics'
        },
        headers: {
          'Authorization': `Bearer ${this.bearerToken}`,
          'Content-Type': 'application/json'
        }
      });

      const tweets = response.data.data || [];
      const newTweets = this.filterNewTweets(tweets);
      
      if (newTweets.length > 0) {
        console.log(`🐦 Found ${newTweets.length} new tweets with ${this.hashtag}`);
        await this.processNewTweets(newTweets);
      }
      
    } catch (error) {
      console.error('❌ Error checking for new tweets:', error.response?.data || error.message);
    }
  }

  /**
   * Filter out tweets we've already processed
   */
  filterNewTweets(tweets) {
    const processedTweets = this.getProcessedTweets();
    return tweets.filter(tweet => !processedTweets.includes(tweet.id));
  }

  /**
   * Process new tweets and update thermometer
   */
  async processNewTweets(tweets) {
    for (const tweet of tweets) {
      try {
        // Mark tweet as processed
        this.markTweetAsProcessed(tweet.id);
        
        // Calculate thermometer impact
        const thermometerImpact = this.calculateThermometerImpact(tweet);
        
        // Update thermometer if we have impact
        if (thermometerImpact > 0 && this.thermometerService) {
          await this.updateThermometer(thermometerImpact, tweet);
        }
        
        console.log(`🌡️ Tweet processed: ${tweet.text.substring(0, 50)}... Impact: ${thermometerImpact}`);
        
      } catch (error) {
        console.error('❌ Error processing tweet:', error.message);
      }
    }
  }

  /**
   * Calculate thermometer impact based on tweet metrics
   */
  calculateThermometerImpact(tweet) {
    const metrics = tweet.public_metrics || {};
    
    // Base impact from tweet engagement
    let impact = 1; // Base impact for any tweet
    
    // Bonus for engagement
    impact += Math.floor((metrics.like_count || 0) / 10); // 1 point per 10 likes
    impact += Math.floor((metrics.retweet_count || 0) / 5); // 1 point per 5 retweets
    impact += Math.floor((metrics.reply_count || 0) / 3); // 1 point per 3 replies
    
    // Cap the impact to prevent abuse
    return Math.min(impact, 5);
  }

  /**
   * Update thermometer based on tweet impact
   */
  async updateThermometer(impact, tweet) {
    try {
      // Calculate how many thermometer NFTs to mint based on impact
      const thermometersToMint = Math.floor(impact / this.tweetsPerThermometerLevel);
      
      if (thermometersToMint > 0) {
        // Mint thermometer NFTs
        for (let i = 0; i < thermometersToMint; i++) {
          await this.thermometerService.mintThermometerFromTweet({
            tweetId: tweet.id,
            tweetText: tweet.text,
            impact: impact,
            timestamp: new Date().toISOString()
          });
        }
        
        console.log(`🌡️ Minted ${thermometersToMint} thermometer NFTs from tweet ${tweet.id}`);
      }
      
    } catch (error) {
      console.error('❌ Error updating thermometer:', error.message);
    }
  }

  /**
   * Get processed tweets (stored in memory for now, could use database)
   */
  getProcessedTweets() {
    if (!this.processedTweets) {
      this.processedTweets = new Set();
    }
    return this.processedTweets;
  }

  /**
   * Mark tweet as processed
   */
  markTweetAsProcessed(tweetId) {
    if (!this.processedTweets) {
      this.processedTweets = new Set();
    }
    this.processedTweets.add(tweetId);
  }

  /**
   * Deactivate the provider
   */
  deactivateProvider() {
    if (this.monitoringInterval) {
      clearInterval(this.monitoringInterval);
    }
    this.isActive = false;
    console.log('🛑 TwitterOASIS provider deactivated');
  }

  /**
   * Get provider status
   */
  getStatus() {
    return {
      providerName: this.providerName,
      isActive: this.isActive,
      hashtag: this.hashtag,
      processedTweets: this.processedTweets?.size || 0,
      lastRequestTime: this.lastRequestTime
    };
  }
}

module.exports = TwitterOASISProvider;
