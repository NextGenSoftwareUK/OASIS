const express = require('express');
const cors = require('cors');
const TwitterService = require('./TwitterService');
const ThermometerService = require('./ThermometerService');

const app = express();
const PORT = process.env.PORT || 3002;
const NODE_ENV = process.env.NODE_ENV || 'development';

// Initialize services
const thermometerService = new ThermometerService();
const twitterService = new TwitterService();

// Connect services
twitterService.setThermometerService(thermometerService);

// Middleware
const corsOptions = {
    origin: process.env.CORS_ORIGIN || (NODE_ENV === 'production' ? 'https://billionshealed.com' : true),
    credentials: true
};
app.use(cors(corsOptions));
app.use(express.json());

// Health check
app.get('/health', (req, res) => {
  res.json({
    status: 'healthy',
    timestamp: new Date().toISOString(),
    service: 'BillionsHealed API'
  });
});

// =============================================================================
// TWITTER ENDPOINTS
// =============================================================================

// Initialize Twitter service
app.post('/api/twitter/initialize', async (req, res) => {
  try {
    const { bearerToken, hashtag } = req.body;
    
    if (!bearerToken) {
      return res.status(400).json({ error: 'Twitter Bearer Token is required' });
    }

    const config = {
      bearerToken,
      hashtag: hashtag || '#billionshealed',
      webhookUrl: `${process.env.BACKEND_URL || 'http://localhost:3002'}/api/twitter/webhook`
    };

    const result = await twitterService.initialize(config);
    
    if (result.success) {
      await twitterService.startMonitoring();
      res.json({ success: true, message: 'Twitter service initialized and monitoring started' });
    } else {
      res.status(500).json({ success: false, error: result.error });
    }
  } catch (error) {
    console.error('❌ Twitter initialization error:', error);
    res.status(500).json({ success: false, error: error.message });
  }
});

// Get Twitter service status
app.get('/api/twitter/status', async (req, res) => {
  try {
    const status = twitterService.getStatus();
    res.json({ success: true, status });
  } catch (error) {
    console.error('❌ Get status error:', error);
    res.status(500).json({ success: false, error: error.message });
  }
});

// Get recent tweets with hashtag
app.get('/api/twitter/recent-tweets', async (req, res) => {
  const limit = parseInt(req.query.limit) || 10;
  
  // Return mock data for demo (Essential tier has strict limits)
  console.log('📊 Returning mock tweets for demo');
  const mockTweets = generateMockTweets(limit);
  res.json({ success: true, tweets: mockTweets, meta: { result_count: mockTweets.length } });
});

// Generate mock tweets for demo
function generateMockTweets(count) {
  const mockTweets = [
    {
      id: '1',
      text: 'Just started my healing journey! Taking it one day at a time 🌟 #billionshealed',
      author_id: '1',
      created_at: new Date(Date.now() - 5 * 60000).toISOString(),
      public_metrics: { like_count: 15, retweet_count: 3, reply_count: 2, quote_count: 0 },
      author: { username: 'healingwarrior', name: 'Healing Warrior', profile_image_url: null }
    },
    {
      id: '2',
      text: 'Every step forward is a victory. Proud of my progress today! #billionshealed #selfcare',
      author_id: '2',
      created_at: new Date(Date.now() - 30 * 60000).toISOString(),
      public_metrics: { like_count: 45, retweet_count: 12, reply_count: 8, quote_count: 1 },
      author: { username: 'journeyofhealing', name: 'Journey of Healing', profile_image_url: null }
    },
    {
      id: '3',
      text: 'Sharing my story to help others heal. We are stronger together 💪 #billionshealed',
      author_id: '3',
      created_at: new Date(Date.now() - 60 * 60000).toISOString(),
      public_metrics: { like_count: 78, retweet_count: 23, reply_count: 15, quote_count: 3 },
      author: { username: 'togetherwerise', name: 'Together We Rise', profile_image_url: null }
    },
    {
      id: '4',
      text: 'Healing isn\'t linear, but every moment counts. Keep going! #billionshealed',
      author_id: '4',
      created_at: new Date(Date.now() - 2 * 3600000).toISOString(),
      public_metrics: { like_count: 4, retweet_count: 1, reply_count: 0, quote_count: 0 },
      author: { username: 'mindfulhealer', name: 'Mindful Healer', profile_image_url: null }
    },
    {
      id: '5',
      text: 'Grateful for this community and the support we share. #billionshealed #gratitude',
      author_id: '5',
      created_at: new Date(Date.now() - 4 * 3600000).toISOString(),
      public_metrics: { like_count: 32, retweet_count: 7, reply_count: 5, quote_count: 2 },
      author: { username: 'gratefulheart', name: 'Grateful Heart', profile_image_url: null }
    },
    {
      id: '6',
      text: 'Today I choose peace over perfection. #billionshealed #mentalhealth',
      author_id: '6',
      created_at: new Date(Date.now() - 6 * 3600000).toISOString(),
      public_metrics: { like_count: 67, retweet_count: 18, reply_count: 11, quote_count: 4 },
      author: { username: 'peacefulpath', name: 'Peaceful Path', profile_image_url: null }
    },
    {
      id: '7',
      text: 'Small wins matter. Celebrating every tiny step forward. 🎉 #billionshealed',
      author_id: '7',
      created_at: new Date(Date.now() - 8 * 3600000).toISOString(),
      public_metrics: { like_count: 23, retweet_count: 5, reply_count: 3, quote_count: 0 },
      author: { username: 'smallwins', name: 'Small Wins', profile_image_url: null }
    },
    {
      id: '8',
      text: 'The healing community gives me hope every day. Thank you all! 💙 #billionshealed',
      author_id: '8',
      created_at: new Date(Date.now() - 12 * 3600000).toISOString(),
      public_metrics: { like_count: 89, retweet_count: 25, reply_count: 19, quote_count: 6 },
      author: { username: 'hopefulheart', name: 'Hopeful Heart', profile_image_url: null }
    },
    {
      id: '9',
      text: 'Boundaries are self-care. Learning to say no with love. #billionshealed',
      author_id: '9',
      created_at: new Date(Date.now() - 18 * 3600000).toISOString(),
      public_metrics: { like_count: 41, retweet_count: 9, reply_count: 6, quote_count: 1 },
      author: { username: 'boundariesrock', name: 'Boundaries Rock', profile_image_url: null }
    },
    {
      id: '10',
      text: '1 year of healing work. So proud of how far I\'ve come! 🌟 #billionshealed #recovery',
      author_id: '10',
      created_at: new Date(Date.now() - 24 * 3600000).toISOString(),
      public_metrics: { like_count: 156, retweet_count: 42, reply_count: 28, quote_count: 8 },
      author: { username: 'yearofhealing', name: 'Year of Healing', profile_image_url: null }
    }
  ];
  
  return mockTweets.slice(0, count);
}

// =============================================================================
// THERMOMETER ENDPOINTS
// =============================================================================

// Get thermometer statistics
app.get('/api/thermometer/statistics', async (req, res) => {
  try {
    const stats = thermometerService.getThermometerStatistics();
    res.json({ success: true, statistics: stats });
  } catch (error) {
    console.error('❌ Get thermometer statistics error:', error);
    res.status(500).json({ success: false, error: error.message });
  }
});

// Get thermometer status
app.get('/api/thermometer/status', async (req, res) => {
  try {
    const status = thermometerService.getStatus();
    res.json({ success: true, status });
  } catch (error) {
    console.error('❌ Get thermometer status error:', error);
    res.status(500).json({ success: false, error: error.message });
  }
});

// Get all thermometer data
app.get('/api/thermometer/data', async (req, res) => {
  try {
    const data = thermometerService.getAllThermometerData();
    res.json({ success: true, data });
  } catch (error) {
    console.error('❌ Get thermometer data error:', error);
    res.status(500).json({ success: false, error: error.message });
  }
});

// Manual thermometer mint
app.post('/api/thermometer/mint', async (req, res) => {
  try {
    const { source = 'manual', metadata = {} } = req.body;
    const result = await thermometerService.mintThermometer(source, metadata);
    res.json(result);
  } catch (error) {
    console.error('❌ Manual thermometer mint error:', error);
    res.status(500).json({ success: false, error: error.message });
  }
});

// Reset thermometer data
app.post('/api/thermometer/reset', async (req, res) => {
  try {
    thermometerService.resetThermometerData();
    res.json({ success: true, message: 'Thermometer data reset' });
  } catch (error) {
    console.error('❌ Reset thermometer data error:', error);
    res.status(500).json({ success: false, error: error.message });
  }
});

// =============================================================================
// START SERVER
// =============================================================================

app.listen(PORT, () => {
  console.log('');
  console.log('🌡️  ========================================');
  console.log('🌡️  BillionsHealed API Server');
  console.log('🌡️  ========================================');
  console.log(`🌐 Server running on: http://localhost:${PORT}`);
  console.log(`🔗 Health check: http://localhost:${PORT}/health`);
  console.log(`🐦 Twitter status: http://localhost:${PORT}/api/twitter/status`);
  console.log(`🌡️  Thermometer status: http://localhost:${PORT}/api/thermometer/status`);
  console.log('🌡️  ========================================');
  console.log('');
});

module.exports = app;

