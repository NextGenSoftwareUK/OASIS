const fs = require('fs');
const path = require('path');

/**
 * TwitterCache - Simple file-based cache for Twitter data
 * Stores tweets and metadata to minimize API calls
 */
class TwitterCache {
  constructor() {
    this.cacheDir = path.join(__dirname, 'cache');
    this.cacheFile = path.join(this.cacheDir, 'twitter-cache.json');
    this.ensureCacheDir();
  }

  /**
   * Ensure cache directory exists
   */
  ensureCacheDir() {
    if (!fs.existsSync(this.cacheDir)) {
      fs.mkdirSync(this.cacheDir, { recursive: true });
      console.log('📁 Created cache directory');
    }
  }

  /**
   * Save tweets to cache
   */
  saveTweets(tweets) {
    try {
      const cacheData = {
        tweets: tweets,
        timestamp: new Date().toISOString(),
        count: tweets.length
      };

      fs.writeFileSync(this.cacheFile, JSON.stringify(cacheData, null, 2));
      console.log(`💾 Cached ${tweets.length} tweets at ${cacheData.timestamp}`);
      
      return { success: true, count: tweets.length };
    } catch (error) {
      console.error('❌ Error saving cache:', error.message);
      return { success: false, error: error.message };
    }
  }

  /**
   * Load tweets from cache
   */
  loadTweets() {
    try {
      if (!fs.existsSync(this.cacheFile)) {
        console.log('⚠️  No cache file found');
        return { success: false, tweets: [], cached: false };
      }

      const cacheData = JSON.parse(fs.readFileSync(this.cacheFile, 'utf8'));
      
      console.log(`📂 Loaded ${cacheData.count} tweets from cache (${cacheData.timestamp})`);
      
      return {
        success: true,
        tweets: cacheData.tweets,
        cached: true,
        cachedAt: cacheData.timestamp,
        count: cacheData.count
      };
    } catch (error) {
      console.error('❌ Error loading cache:', error.message);
      return { success: false, tweets: [], cached: false, error: error.message };
    }
  }

  /**
   * Get cache metadata
   */
  getCacheInfo() {
    try {
      if (!fs.existsSync(this.cacheFile)) {
        return {
          exists: false,
          message: 'No cached tweets'
        };
      }

      const cacheData = JSON.parse(fs.readFileSync(this.cacheFile, 'utf8'));
      const cacheAge = Date.now() - new Date(cacheData.timestamp).getTime();
      const hoursAgo = Math.floor(cacheAge / (1000 * 60 * 60));
      const minutesAgo = Math.floor((cacheAge % (1000 * 60 * 60)) / (1000 * 60));

      return {
        exists: true,
        count: cacheData.count,
        timestamp: cacheData.timestamp,
        age: `${hoursAgo}h ${minutesAgo}m ago`
      };
    } catch (error) {
      return {
        exists: false,
        error: error.message
      };
    }
  }

  /**
   * Clear cache
   */
  clearCache() {
    try {
      if (fs.existsSync(this.cacheFile)) {
        fs.unlinkSync(this.cacheFile);
        console.log('🗑️  Cache cleared');
        return { success: true };
      }
      return { success: true, message: 'No cache to clear' };
    } catch (error) {
      console.error('❌ Error clearing cache:', error.message);
      return { success: false, error: error.message };
    }
  }
}

module.exports = TwitterCache;




