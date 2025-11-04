const axios = require('axios');
const cheerio = require('cheerio');

/**
 * NitterService - Free, unlimited Twitter scraping via Nitter instances
 * No API keys required, no rate limits
 * Follows OASIS provider pattern
 */
class NitterService {
  constructor() {
    this.serviceName = 'NitterService';
    this.isActive = false;
    
    // List of public Nitter instances (fallback if one is down)
    this.nitterInstances = [
      'https://nitter.net',
      'https://nitter.privacydev.net',
      'https://nitter.poast.org',
      'https://nitter.1d4.us'
    ];
    
    this.currentInstanceIndex = 0;
  }

  /**
   * Get current Nitter instance URL
   */
  getCurrentInstance() {
    return this.nitterInstances[this.currentInstanceIndex];
  }

  /**
   * Try next Nitter instance if current one fails
   */
  rotateInstance() {
    this.currentInstanceIndex = (this.currentInstanceIndex + 1) % this.nitterInstances.length;
    console.log(`🔄 Rotating to Nitter instance: ${this.getCurrentInstance()}`);
  }

  /**
   * Search for tweets with hashtag
   */
  async searchHashtag(hashtag, maxResults = 100) {
    const cleanHashtag = hashtag.replace('#', '');
    let attempts = 0;
    const maxAttempts = this.nitterInstances.length;

    while (attempts < maxAttempts) {
      try {
        const instance = this.getCurrentInstance();
        const url = `${instance}/search?q=%23${cleanHashtag}&f=tweets`;
        
        console.log(`🔍 Searching Nitter for #${cleanHashtag}...`);
        console.log(`📡 Using instance: ${instance}`);

        const response = await axios.get(url, {
          timeout: 10000,
          headers: {
            'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36'
          }
        });

        const tweets = this.parseTweets(response.data, maxResults);
        
        console.log(`✅ Found ${tweets.length} tweets with #${cleanHashtag}`);
        
        return {
          success: true,
          tweets: tweets,
          source: 'nitter',
          instance: instance,
          count: tweets.length
        };

      } catch (error) {
        console.error(`❌ Error with ${this.getCurrentInstance()}:`, error.message);
        attempts++;
        
        if (attempts < maxAttempts) {
          this.rotateInstance();
          console.log(`⏭️  Trying next instance...`);
        } else {
          console.error('❌ All Nitter instances failed');
          return {
            success: false,
            error: 'All Nitter instances unavailable',
            tweets: []
          };
        }
      }
    }
  }

  /**
   * Parse tweets from Nitter HTML
   */
  parseTweets(html, maxResults) {
    const $ = cheerio.load(html);
    const tweets = [];
    
    $('.timeline-item').each((index, element) => {
      if (index >= maxResults) return false; // Stop after maxResults
      
      try {
        const $tweet = $(element);
        
        // Extract tweet data
        const username = $tweet.find('.username').first().text().trim().replace('@', '');
        const fullname = $tweet.find('.fullname').first().text().trim();
        const tweetText = $tweet.find('.tweet-content').text().trim();
        const tweetLink = $tweet.find('.tweet-link').attr('href') || '';
        const tweetId = tweetLink.split('/').pop()?.split('#')[0] || `nitter_${Date.now()}_${index}`;
        
        // Extract timestamp
        const timeText = $tweet.find('.tweet-date a').attr('title') || '';
        const createdAt = this.parseNitterDate(timeText);
        
        // Extract metrics
        const stats = $tweet.find('.tweet-stat');
        let likes = 0, retweets = 0, replies = 0;
        
        stats.each((i, stat) => {
          const $stat = $(stat);
          const iconClass = $stat.find('.icon').attr('class') || '';
          const count = parseInt($stat.find('.icon-text').text().trim().replace(/,/g, '')) || 0;
          
          if (iconClass.includes('heart')) likes = count;
          if (iconClass.includes('retweet')) retweets = count;
          if (iconClass.includes('comment')) replies = count;
        });

        // Create tweet object in Twitter API v2 format
        const tweet = {
          id: tweetId,
          text: tweetText,
          author_id: username,
          created_at: createdAt,
          public_metrics: {
            like_count: likes,
            retweet_count: retweets,
            reply_count: replies,
            quote_count: 0
          },
          author: {
            username: username,
            name: fullname || username,
            profile_image_url: null
          }
        };

        tweets.push(tweet);
        
      } catch (err) {
        console.error('Error parsing tweet:', err.message);
      }
    });

    return tweets;
  }

  /**
   * Parse Nitter date format to ISO string
   */
  parseNitterDate(dateStr) {
    try {
      // Nitter formats: "Jan 15, 2025 · 3:45 PM UTC"
      if (dateStr) {
        const date = new Date(dateStr);
        if (!isNaN(date.getTime())) {
          return date.toISOString();
        }
      }
    } catch (err) {
      // Fallback to current time if parsing fails
    }
    return new Date().toISOString();
  }

  /**
   * Get service status
   */
  getStatus() {
    return {
      serviceName: this.serviceName,
      isActive: this.isActive,
      currentInstance: this.getCurrentInstance(),
      availableInstances: this.nitterInstances.length
    };
  }

  /**
   * Activate service
   */
  activate() {
    this.isActive = true;
    console.log('✅ NitterService activated');
    return { success: true };
  }

  /**
   * Deactivate service
   */
  deactivate() {
    this.isActive = false;
    console.log('🛑 NitterService deactivated');
    return { success: true };
  }
}

module.exports = NitterService;

