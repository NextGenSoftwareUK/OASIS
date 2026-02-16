// BillionsHealed Frontend Application
// Standalone version for GoDaddy deployment

// Configuration
const CONFIG = {
    // For GoDaddy deployment, we'll use mock data since we can't run a Node.js backend
    USE_MOCK_DATA: true,
    MOCK_API_DELAY: 500
};

// Mock data for demonstration
const MOCK_THERMOMETER_DATA = {
    thermometerMinted: 0,
    thermometerData: [],
    maxThermometers: 100,
    temperatureLevels: {
        cold: { min: 0, max: 24, color: '#0096FF', label: 'Cold' },
        warm: { min: 25, max: 49, color: '#00BFFF', label: 'Warm' },
        hot: { min: 50, max: 74, color: '#FFD700', label: 'Hot' },
        boiling: { min: 75, max: 100, color: '#FF4500', label: 'Boiling' }
    }
};

const MOCK_TWEETS = [
    {
        id: '1',
        text: 'Just discovered #billionshealed and I\'m amazed by this healing movement! 🌡️✨',
        author: { username: 'healing_soul', name: 'Healing Soul' },
        public_metrics: { like_count: 42, retweet_count: 12, reply_count: 8 },
        created_at: new Date(Date.now() - 30 * 60 * 1000).toISOString()
    },
    {
        id: '2',
        text: 'The thermometer is rising! We\'re making real progress toward healing billions. #billionshealed',
        author: { username: 'consciousness_guide', name: 'Consciousness Guide' },
        public_metrics: { like_count: 89, retweet_count: 23, reply_count: 15 },
        created_at: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString()
    },
    {
        id: '3',
        text: 'Every NFT minted brings us closer to global healing. Join the movement! #billionshealed 🌡️',
        author: { username: 'oasis_member', name: 'OASIS Member' },
        public_metrics: { like_count: 156, retweet_count: 45, reply_count: 28 },
        created_at: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString()
    },
    {
        id: '4',
        text: 'Technology meets consciousness in the most beautiful way. #billionshealed is the future!',
        author: { username: 'tech_healer', name: 'Tech Healer' },
        public_metrics: { like_count: 73, retweet_count: 19, reply_count: 11 },
        created_at: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString()
    }
];

// Global state
let thermometerData = { ...MOCK_THERMOMETER_DATA };
let tweetsData = [...MOCK_TWEETS];
let twitterFeedVisible = false;

// Initialize application
document.addEventListener('DOMContentLoaded', function() {
    console.log('BillionsHealed Frontend Initialized');
    loadThermometerStatus();
    loadTweets();
    initializeEventListeners();
});

// Event Listeners
function initializeEventListeners() {
    // Close modal when clicking outside
    document.getElementById('info-modal').addEventListener('click', function(e) {
        if (e.target === this) {
            closeInfoModal();
        }
    });

    // Keyboard shortcuts
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            closeInfoModal();
        }
        if (e.key === 'r' && e.ctrlKey) {
            e.preventDefault();
            refreshData();
        }
    });
}

// Thermometer Functions
async function loadThermometerStatus() {
    try {
        if (CONFIG.USE_MOCK_DATA) {
            // Simulate API delay
            await new Promise(resolve => setTimeout(resolve, CONFIG.MOCK_API_DELAY));
            updateThermometerUI();
        } else {
            // Real API call would go here
            const response = await fetch('/api/thermometer/status');
            const data = await response.json();
            thermometerData = data;
            updateThermometerUI();
        }
    } catch (error) {
        console.error('Error loading thermometer status:', error);
        updateThermometerUI(); // Use mock data on error
    }
}

function updateThermometerUI() {
    const percentage = Math.min((thermometerData.thermometerMinted / thermometerData.maxThermometers) * 100, 100);
    const temperatureLevel = getCurrentTemperatureLevel(percentage);
    
    // Update liquid height
    const liquid = document.getElementById('thermometer-liquid');
    if (liquid) {
        liquid.style.height = `${percentage}%`;
    }
    
    // Update bulb color and glow
    const bulb = document.getElementById('thermometer-bulb');
    if (bulb) {
        bulb.style.boxShadow = `
            0 0 30px ${temperatureLevel.color}40,
            inset 0 0 20px rgba(255, 255, 255, 0.1),
            0 0 0 1px rgba(255, 255, 255, 0.1)
        `;
    }
    
    // Update stats
    const healingLevel = document.getElementById('healing-level');
    const nftCount = document.getElementById('nft-count');
    const nftPrice = document.getElementById('nft-price');
    
    if (healingLevel) healingLevel.textContent = temperatureLevel.label;
    if (nftCount) nftCount.textContent = thermometerData.thermometerMinted;
    if (nftPrice) nftPrice.textContent = calculatePrice();
}

function getCurrentTemperatureLevel(percentage) {
    for (const [level, config] of Object.entries(thermometerData.temperatureLevels)) {
        if (percentage >= config.min && percentage <= config.max) {
            return config;
        }
    }
    return thermometerData.temperatureLevels.cold;
}

function calculatePrice() {
    const basePrice = 0.001;
    const currentMinted = thermometerData.thermometerMinted;
    const priceIncrease = Math.floor(currentMinted / 10) * 0.0005;
    return `${(basePrice + priceIncrease).toFixed(3)} ETH`;
}

async function mintThermometer() {
    try {
        const button = document.querySelector('.mint-thermometer-btn');
        if (button) {
            button.disabled = true;
            button.textContent = 'Minting...';
        }
        
        if (CONFIG.USE_MOCK_DATA) {
            // Simulate minting process
            await new Promise(resolve => setTimeout(resolve, 1500));
            
            // Add new thermometer data
            thermometerData.thermometerMinted++;
            thermometerData.thermometerData.push({
                id: Date.now(),
                timestamp: new Date().toISOString(),
                source: 'manual',
                impact_points: 1
            });
            
            // Update UI
            updateThermometerUI();
            showThermometerMintAnimation();
            
            // Show success message
            showNotification('Healing NFT minted successfully! 🌡️✨', 'success');
            
        } else {
            // Real API call would go here
            const response = await fetch('/api/thermometer/mint', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });
            const data = await response.json();
            
            if (data.success) {
                thermometerData = data.thermometerData;
                updateThermometerUI();
                showThermometerMintAnimation();
                showNotification('Healing NFT minted successfully! 🌡️✨', 'success');
            } else {
                showNotification('Failed to mint NFT. Please try again.', 'error');
            }
        }
        
    } catch (error) {
        console.error('Error minting thermometer:', error);
        showNotification('Failed to mint NFT. Please try again.', 'error');
    } finally {
        const button = document.querySelector('.mint-thermometer-btn');
        if (button) {
            button.disabled = false;
            button.textContent = 'Mint Healing NFT';
        }
    }
}

function showThermometerMintAnimation() {
    const bulb = document.getElementById('thermometer-bulb');
    if (bulb) {
        // Add glow effect
        bulb.style.boxShadow = `
            0 0 50px #00BFFF80,
            0 0 100px #00BFFF40,
            inset 0 0 20px rgba(255, 255, 255, 0.2),
            0 0 0 1px rgba(255, 255, 255, 0.1)
        `;
        
        // Reset after animation
        setTimeout(() => {
            updateThermometerUI();
        }, 2000);
    }
}

// Twitter Feed Functions
async function loadTweets() {
    try {
        const tweetsContainer = document.getElementById('tweets-container');
        const serviceStatus = document.getElementById('service-status');
        const lastUpdate = document.getElementById('last-update');
        
        if (CONFIG.USE_MOCK_DATA) {
            // Simulate API delay
            await new Promise(resolve => setTimeout(resolve, CONFIG.MOCK_API_DELAY));
            
            if (serviceStatus) serviceStatus.textContent = 'Mock Data';
            if (lastUpdate) lastUpdate.textContent = getTimeAgo(new Date());
            renderTweets(tweetsData);
            
        } else {
            // Real API call would go here
            const response = await fetch('/api/twitter/recent-tweets');
            const data = await response.json();
            
            if (data.success && data.tweets) {
                tweetsData = data.tweets;
                if (serviceStatus) serviceStatus.textContent = 'Connected';
                if (lastUpdate) lastUpdate.textContent = getTimeAgo(new Date());
                renderTweets(tweetsData);
            } else {
                throw new Error('Failed to load tweets');
            }
        }
        
    } catch (error) {
        console.error('Error loading tweets:', error);
        const tweetsContainer = document.getElementById('tweets-container');
        if (tweetsContainer) {
            tweetsContainer.innerHTML = `
                <div class="loading-tweets">
                    <p>Unable to load tweets at this time.</p>
                    <p>Displaying demo content...</p>
                </div>
            `;
        }
        
        // Still render mock data
        renderTweets(tweetsData);
    }
}

function renderTweets(tweets) {
    const tweetsContainer = document.getElementById('tweets-container');
    if (!tweetsContainer) return;
    
    if (!tweets || tweets.length === 0) {
        tweetsContainer.innerHTML = '<div class="loading-tweets">No healing tweets found yet. Be the first to tweet #billionshealed!</div>';
        return;
    }
    
    tweetsContainer.innerHTML = tweets.map(tweet => `
        <div class="tweet-item">
            <div class="tweet-header">
                <div class="author-avatar">𝕏</div>
                <div class="author-name">@${tweet.author.username}</div>
            </div>
            <div class="tweet-text">${formatTweetText(tweet.text)}</div>
            <div class="tweet-metrics">
                <span>❤️ ${tweet.public_metrics.like_count}</span>
                <span>🔄 ${tweet.public_metrics.retweet_count}</span>
                <span>💬 ${tweet.public_metrics.reply_count}</span>
            </div>
            <div class="tweet-time">${getTimeAgo(new Date(tweet.created_at))}</div>
        </div>
    `).join('');
}

function formatTweetText(text) {
    // Highlight hashtags and URLs
    return text
        .replace(/#billionshealed/gi, '<strong style="color: #00BFFF;">#billionshealed</strong>')
        .replace(/(https?:\/\/[^\s]+)/g, '<a href="$1" target="_blank" style="color: #0096FF;">$1</a>');
}

function getTimeAgo(date) {
    const now = new Date();
    const diffInSeconds = Math.floor((now - date) / 1000);
    
    if (diffInSeconds < 60) return 'Just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
    return `${Math.floor(diffInSeconds / 86400)}d ago`;
}

function toggleTwitterFeed() {
    const feed = document.getElementById('twitter-feed');
    const fab = document.querySelector('.fab-twitter');
    
    if (twitterFeedVisible) {
        feed.style.display = 'none';
        fab.style.display = 'flex';
        twitterFeedVisible = false;
    } else {
        feed.style.display = 'block';
        fab.style.display = 'none';
        twitterFeedVisible = true;
        loadTweets(); // Refresh tweets when opening
    }
}

// Navigation Functions
function openInfoModal(type) {
    const modal = document.getElementById('info-modal');
    const modalBody = document.getElementById('modal-body');
    
    if (!modal || !modalBody) return;
    
    const content = getInfoContent(type);
    modalBody.innerHTML = content;
    modal.classList.add('active');
}

function closeInfoModal() {
    const modal = document.getElementById('info-modal');
    if (modal) {
        modal.classList.remove('active');
    }
}

function getInfoContent(type) {
    const content = {
        what: `
            <h2>What is BillionsHealed?</h2>
            <p>BillionsHealed is a revolutionary movement that combines blockchain technology with collective consciousness to track and accelerate global healing.</p>
            <p>Through our interactive thermometer, we visualize the collective healing energy as NFTs are minted, representing individual contributions to global wellness.</p>
            <h3>Key Features:</h3>
            <ul>
                <li>Interactive healing thermometer</li>
                <li>NFT-based healing contributions</li>
                <li>Social media integration</li>
                <li>Real-time progress tracking</li>
                <li>Community-driven healing</li>
            </ul>
        `,
        how: `
            <h2>How Does It Work?</h2>
            <p>The BillionsHealed system operates through a multi-layered approach:</p>
            <h3>1. NFT Minting</h3>
            <p>Each time someone mints a healing NFT, the thermometer rises, representing collective healing energy.</p>
            <h3>2. Social Integration</h3>
            <p>We monitor social media for #billionshealed hashtags, automatically updating the thermometer when healing content is shared.</p>
            <h3>3. Temperature Levels</h3>
            <ul>
                <li><strong>Cold (0-24%):</strong> Early stage healing</li>
                <li><strong>Warm (25-49%):</strong> Growing awareness</li>
                <li><strong>Hot (50-74%):</strong> Active participation</li>
                <li><strong>Boiling (75-100%):</strong> Global healing achieved</li>
            </ul>
        `,
        whitepaper: `
            <h2>BillionsHealed Whitepaper</h2>
            <p><strong>Abstract:</strong> This whitepaper presents a novel approach to collective healing through blockchain technology and social consciousness.</p>
            <h3>Technical Architecture</h3>
            <p>Our system consists of three main components:</p>
            <ul>
                <li><strong>Frontend Interface:</strong> Interactive thermometer and social feed</li>
                <li><strong>Blockchain Integration:</strong> NFT minting and smart contracts</li>
                <li><strong>Social Monitoring:</strong> Real-time hashtag tracking</li>
            </ul>
            <h3>Economic Model</h3>
            <p>Each healing NFT has a base price of 0.001 ETH, with dynamic pricing based on demand and thermometer level.</p>
            <h3>Impact Measurement</h3>
            <p>We track healing impact through multiple metrics including NFT mints, social engagement, and community growth.</p>
            <p><em>Full whitepaper coming soon...</em></p>
        `,
        deliverables: `
            <h2>Project Deliverables</h2>
            <h3>Phase 1: Foundation (Completed)</h3>
            <ul>
                <li>✅ Interactive thermometer interface</li>
                <li>✅ NFT minting functionality</li>
                <li>✅ Social media integration framework</li>
                <li>✅ Responsive web design</li>
            </ul>
            <h3>Phase 2: Enhancement (In Progress)</h3>
            <ul>
                <li>🔄 Advanced Twitter API integration</li>
                <li>🔄 Smart contract deployment</li>
                <li>🔄 User authentication system</li>
                <li>🔄 Analytics dashboard</li>
            </ul>
            <h3>Phase 3: Scale (Planned)</h3>
            <ul>
                <li>📋 Multi-platform social integration</li>
                <li>📋 Mobile application</li>
                <li>📋 Advanced NFT features</li>
                <li>📋 Global community platform</li>
            </ul>
        `
    };
    
    return content[type] || '<p>Content not found.</p>';
}

// Utility Functions
function refreshData() {
    showNotification('Refreshing data...', 'info');
    loadThermometerStatus();
    loadTweets();
}

function connectWallet() {
    showNotification('Wallet connection coming soon! 🔗', 'info');
}

function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.textContent = message;
    
    // Style the notification
    notification.style.cssText = `
        position: fixed;
        top: 100px;
        right: 20px;
        background: ${type === 'success' ? '#00BFFF' : type === 'error' ? '#FF4500' : '#0096FF'};
        color: ${type === 'success' || type === 'info' ? '#000' : '#fff'};
        padding: 1rem 1.5rem;
        border-radius: 8px;
        box-shadow: 0 5px 15px rgba(0, 0, 0, 0.3);
        z-index: 3000;
        font-weight: bold;
        max-width: 300px;
        animation: slideInRight 0.3s ease-out;
    `;
    
    document.body.appendChild(notification);
    
    // Remove after 3 seconds
    setTimeout(() => {
        notification.style.animation = 'slideOutRight 0.3s ease-out';
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    }, 3000);
}

// Add CSS for notification animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slideInRight {
        from { transform: translateX(100%); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
    }
    @keyframes slideOutRight {
        from { transform: translateX(0); opacity: 1; }
        to { transform: translateX(100%); opacity: 0; }
    }
`;
document.head.appendChild(style);

// Initialize Twitter feed as hidden
document.addEventListener('DOMContentLoaded', function() {
    const feed = document.getElementById('twitter-feed');
    if (feed) {
        feed.style.display = 'none';
    }
});

