/**
 * ThermometerService - Manages thermometer NFT minting and temperature tracking
 * Integrates with MetaBricks thermometer mode and social media updates
 */
class ThermometerService {
  constructor() {
    this.thermometerData = new Map();
    this.thermometerMinted = 0;
    this.maxThermometers = 100;
    
    // Temperature levels configuration
    this.temperatureLevels = [
      { level: 'cold', range: [0, 25], price: 0.01, color: '#4FC3F7', name: '❄️ Cold' },
      { level: 'warm', range: [26, 50], price: 0.05, color: '#FFEB3B', name: '🌡️ Warm' },
      { level: 'hot', range: [51, 75], price: 0.10, color: '#FF9800', name: '🔥 Hot' },
      { level: 'boiling', range: [76, 100], price: 0.25, color: '#F44336', name: '🌋 Boiling' }
    ];
  }

  /**
   * Mint a thermometer NFT
   */
  async mintThermometer(source = 'manual', metadata = {}) {
    try {
      if (this.thermometerMinted >= this.maxThermometers) {
        return { 
          success: false, 
          error: 'All 100 thermometer NFTs have been minted! Maximum temperature reached!' 
        };
      }

      const thermometerNumber = this.thermometerMinted + 1;
      const level = this.getCurrentTemperatureLevel();
      
      // Create thermometer NFT data
      const thermometerData = {
        number: thermometerNumber,
        level: level.level,
        color: level.color,
        price: level.price,
        mintedAt: new Date().toISOString(),
        source: source,
        metadata: metadata
      };

      // Store thermometer data
      this.thermometerData.set(thermometerNumber, thermometerData);
      this.thermometerMinted++;

      console.log(`🌡️ Minted thermometer #${thermometerNumber} (${level.name}) from ${source}`);
      
      return {
        success: true,
        thermometerNumber,
        level,
        thermometerData
      };
    } catch (error) {
      console.error('❌ Error minting thermometer:', error.message);
      return { success: false, error: error.message };
    }
  }

  /**
   * Mint thermometer from social media interaction
   */
  async mintThermometerFromSocialMedia(socialData) {
    try {
      // Create metadata for social media mint
      const metadata = {
        source: 'social_media',
        platform: socialData.source,
        socialId: socialData.tweetId || socialData.postId,
        socialText: socialData.tweetText || socialData.postText,
        impact: socialData.impact || 1,
        timestamp: socialData.timestamp
      };

      return await this.mintThermometer('social_media', metadata);
    } catch (error) {
      console.error('❌ Error minting thermometer from social media:', error.message);
      return { success: false, error: error.message };
    }
  }

  /**
   * Get current temperature level based on minted count
   */
  getCurrentTemperatureLevel() {
    for (const level of this.temperatureLevels) {
      if (this.thermometerMinted >= level.range[0] && this.thermometerMinted < level.range[1]) {
        return level;
      }
    }
    // If we've exceeded all levels, return the last one
    return this.temperatureLevels[this.temperatureLevels.length - 1];
  }

  /**
   * Get thermometer fill percentage (0-100)
   */
  getThermometerFillPercentage() {
    return Math.min((this.thermometerMinted / this.maxThermometers) * 100, 100);
  }

  /**
   * Get thermometer liquid color
   */
  getThermometerColor() {
    return this.getCurrentTemperatureLevel().color;
  }

  /**
   * Get thermometer statistics
   */
  getThermometerStatistics() {
    const level = this.getCurrentTemperatureLevel();
    const nextLevel = this.temperatureLevels.find(l => l.range[0] > this.thermometerMinted);
    
    return {
      mode: 'Thermometer Mode',
      description: 'Buy thermometer NFTs to fill up the temperature',
      totalThermometers: this.maxThermometers,
      minted: this.thermometerMinted,
      remaining: this.maxThermometers - this.thermometerMinted,
      currentLevel: level,
      nextLevel: nextLevel,
      fillPercentage: this.getThermometerFillPercentage(),
      currentPrice: level.price
    };
  }

  /**
   * Get all thermometer data
   */
  getAllThermometerData() {
    return Array.from(this.thermometerData.entries()).map(([number, data]) => ({
      number,
      ...data
    }));
  }

  /**
   * Get thermometer by number
   */
  getThermometerByNumber(number) {
    return this.thermometerData.get(number);
  }

  /**
   * Get thermometers by source
   */
  getThermometersBySource(source) {
    return this.getAllThermometerData().filter(t => t.source === source);
  }

  /**
   * Get thermometers minted from social media
   */
  getSocialMediaThermometers() {
    return this.getThermometersBySource('social_media');
  }

  /**
   * Get service status
   */
  getStatus() {
    return {
      totalMinted: this.thermometerMinted,
      maxThermometers: this.maxThermometers,
      fillPercentage: this.getThermometerFillPercentage(),
      currentLevel: this.getCurrentTemperatureLevel(),
      color: this.getThermometerColor(),
      socialMediaMints: this.getSocialMediaThermometers().length
    };
  }

  /**
   * Reset thermometer data (for testing)
   */
  resetThermometerData() {
    this.thermometerData.clear();
    this.thermometerMinted = 0;
    console.log('🔄 Thermometer data reset');
  }

  /**
   * Bulk mint thermometers (for testing or special events)
   */
  async bulkMintThermometers(count, source = 'bulk', metadata = {}) {
    const results = [];
    
    for (let i = 0; i < count; i++) {
      if (this.thermometerMinted >= this.maxThermometers) {
        break; // Stop if we've reached the maximum
      }
      
      const result = await this.mintThermometer(source, metadata);
      results.push(result);
    }
    
    return results;
  }
}

module.exports = ThermometerService;
