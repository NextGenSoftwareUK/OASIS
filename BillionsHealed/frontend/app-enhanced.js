// BillionsHealed Frontend Application - Enhanced Version
const API_BASE_URL = 'http://localhost:3002/api';

// State
let thermometerData = {
    totalMinted: 0,
    maxThermometers: 100,
    fillPercentage: 0,
    currentLevel: { level: 'cold', name: 'Cold', color: '#4FC3F7', price: 0.01 },
    processedTweets: 0,
    tweets: [], // Store tweets for linking
    tweetContributions: [] // Track which tweet contributed to which temperature point
};

// Temperature levels
const temperatureLevels = [
    { level: 'cold', range: [0, 25], price: 0.01, color: '#4FC3F7', name: 'Cold' },
    { level: 'warm', range: [26, 50], price: 0.05, color: '#FFEB3B', name: 'Warm' },
    { level: 'hot', range: [51, 75], price: 0.10, color: '#FF9800', name: 'Hot' },
    { level: 'boiling', range: [76, 100], price: 0.25, color: '#F44336', name: 'Boiling' }
];

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    console.log('BillionsHealed initialized');
    initializeApp();
});

async function initializeApp() {
    // Set up event listeners
    document.getElementById('mint-btn').addEventListener('click', mintThermometer);
    document.getElementById('refresh-twitter-btn').addEventListener('click', manualRefreshTwitter);
    document.getElementById('share-impact-btn')?.addEventListener('click', shareImpact);
    
    // Load initial data
    await loadThermometerStatus();
    await loadTweets();
    await updateCacheStatus();
    
    // Auto-refresh thermometer and cache status every minute
    setInterval(async () => {
        await loadThermometerStatus();
        await loadTweets();
        await updateCacheStatus();
    }, 60000);
}

// Load thermometer status
async function loadThermometerStatus() {
    try {
        const response = await fetch(`${API_BASE_URL}/thermometer/status`);
        const data = await response.json();
        
        if (data.success) {
            thermometerData = {
                ...thermometerData,
                totalMinted: data.status.totalMinted,
                maxThermometers: data.status.maxThermometers,
                fillPercentage: data.status.fillPercentage,
                currentLevel: data.status.currentLevel
            };
            
            updateThermometerUI();
            drawTweetConnections(); // Redraw connections after update
        }
    } catch (error) {
        console.error('Error loading thermometer status:', error);
    }
}

// Update thermometer UI
function updateThermometerUI() {
    // Update liquid fill
    const liquid = document.getElementById('thermometer-liquid');
    liquid.style.height = `${thermometerData.fillPercentage}%`;
    liquid.style.background = thermometerData.currentLevel.color;
    
    // Update bulb color
    const bulb = document.getElementById('thermometer-bulb');
    bulb.style.background = thermometerData.currentLevel.color;
    
    // Update info
    document.getElementById('current-level').textContent = thermometerData.currentLevel.name;
    document.getElementById('nft-count').textContent = `${thermometerData.totalMinted}/${thermometerData.maxThermometers} NFTs`;
    document.getElementById('current-price').textContent = `Price: ${thermometerData.currentLevel.price} SOL`;
    document.getElementById('fill-percentage').textContent = `${thermometerData.fillPercentage.toFixed(1)}%`;
    document.getElementById('remaining-count').textContent = thermometerData.maxThermometers - thermometerData.totalMinted;
    
    // Update mint button
    const mintBtn = document.getElementById('mint-btn');
    mintBtn.textContent = `Mint Thermometer #${thermometerData.totalMinted + 1}`;
    
    if (thermometerData.totalMinted >= thermometerData.maxThermometers) {
        mintBtn.disabled = true;
        mintBtn.textContent = 'All Minted!';
    }
}

// Mint thermometer
async function mintThermometer() {
    const mintBtn = document.getElementById('mint-btn');
    mintBtn.disabled = true;
    mintBtn.textContent = 'Minting...';
    
    try {
        const response = await fetch(`${API_BASE_URL}/thermometer/mint`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                source: 'manual',
                metadata: { timestamp: Date.now() }
            })
        });
        
        const data = await response.json();
        
        if (data.success) {
            console.log('Thermometer minted:', data);
            
            // Animate the thermometer with glow effect
            const bulb = document.getElementById('thermometer-bulb');
            bulb.style.boxShadow = `
                0 8px 32px rgba(0, 150, 255, 0.8),
                inset 0 1px 0 rgba(255, 255, 255, 0.4),
                inset 0 -1px 0 rgba(0, 0, 0, 0.2),
                inset 2px 2px 10px rgba(255, 255, 255, 0.1),
                0 0 0 1px rgba(255, 255, 255, 0.2),
                0 0 50px rgba(0, 150, 255, 0.6)
            `;
            setTimeout(() => {
                bulb.style.boxShadow = `
                    0 8px 32px rgba(0, 150, 255, 0.3),
                    inset 0 1px 0 rgba(255, 255, 255, 0.4),
                    inset 0 -1px 0 rgba(0, 0, 0, 0.2),
                    inset 2px 2px 10px rgba(255, 255, 255, 0.1),
                    0 0 0 1px rgba(255, 255, 255, 0.2)
                `;
            }, 800);
            
            // Track contribution
            addTweetContribution(thermometerData.totalMinted + 1, 'manual');
            
            // Reload status
            await loadThermometerStatus();
        } else {
            alert('Failed to mint thermometer: ' + data.error);
        }
    } catch (error) {
        console.error('Error minting thermometer:', error);
        alert('Failed to mint thermometer');
    } finally {
        mintBtn.disabled = false;
        updateThermometerUI();
    }
}

// Load tweets (from cache or mock)
async function loadTweets() {
    const container = document.getElementById('tweets-container');
    
    try {
        const response = await fetch(`${API_BASE_URL}/twitter/recent-tweets?limit=10`);
        const data = await response.json();
        
        if (data.success && data.tweets) {
            thermometerData.tweets = data.tweets; // Store tweets for linking
            renderTweets(data.tweets);
            
            // Calculate tweet contributions to temperature
            calculateTweetContributions(data.tweets);
            
            // Draw visual connections
            drawTweetConnections();
            
            // Show if tweets are cached or mock
            if (data.meta.cached) {
                console.log('Showing cached tweets from:', data.meta.cached_at);
            } else if (data.meta.mock) {
                console.log('Showing demo tweets (use Refresh from Twitter to get real tweets)');
            }
        } else {
            container.innerHTML = '<div class="loading">No tweets available</div>';
        }
    } catch (error) {
        console.error('Error loading tweets:', error);
        container.innerHTML = '<div class="loading">Error loading tweets</div>';
    }
}

// Calculate which tweet contributed to which temperature level
function calculateTweetContributions(tweets) {
    thermometerData.tweetContributions = [];
    let currentTemp = 0;
    
    tweets.forEach((tweet, index) => {
        const engagement = (tweet.public_metrics.like_count || 0) + 
                         (tweet.public_metrics.retweet_count || 0) * 2 + 
                         (tweet.public_metrics.reply_count || 0);
        
        // Each tweet contributes about 1% temperature per 10 engagement points
        const tempIncrease = Math.min(5, Math.floor(engagement / 10) + 1);
        currentTemp += tempIncrease;
        
        thermometerData.tweetContributions.push({
            tweetId: tweet.id,
            author: tweet.author?.username || 'user',
            temperature: Math.min(100, currentTemp),
            tempIncrease: tempIncrease
        });
    });
}

// Draw visual connections between tweets and thermometer
function drawTweetConnections() {
    // Remove existing connections
    const existing = document.getElementById('tweet-connections');
    if (existing) existing.remove();
    
    if (thermometerData.tweetContributions.length === 0) return;
    
    // Create SVG overlay
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.id = 'tweet-connections';
    svg.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:5;';
    
    document.querySelector('.main-content').style.position = 'relative';
    document.querySelector('.main-content').appendChild(svg);
    
    // Draw connections
    const thermoRect = document.querySelector('.thermometer-tube').getBoundingClientRect();
    const tweets = document.querySelectorAll('.tweet-item');
    
    tweets.forEach((tweetEl, index) => {
        if (index >= thermometerData.tweetContributions.length) return;
        
        const contribution = thermometerData.tweetContributions[index];
        const tweetRect = tweetEl.getBoundingClientRect();
        
        // Calculate thermometer position for this temperature
        const tempY = thermoRect.bottom - (thermoRect.height * (contribution.temperature / 100));
        
        // Create line
        const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
        line.setAttribute('x1', tweetRect.right - window.scrollX);
        line.setAttribute('y1', tweetRect.top + tweetRect.height / 2 - window.scrollY);
        line.setAttribute('x2', thermoRect.left - window.scrollX);
        line.setAttribute('y2', tempY - window.scrollY);
        line.setAttribute('stroke', thermometerData.currentLevel.color);
        line.setAttribute('stroke-width', '2');
        line.setAttribute('stroke-dasharray', '5,5');
        line.setAttribute('opacity', '0.5');
        
        svg.appendChild(line);
        
        // Add temperature marker on tweet
        const marker = document.createElement('div');
        marker.className = 'tweet-temp-marker';
        marker.textContent = `+${contribution.tempIncrease}° ${contribution.temperature}%`;
        marker.style.cssText = `
            position: absolute;
            right: 10px;
            bottom: 10px;
            background: ${thermometerData.currentLevel.color};
            color: white;
            padding: 2px 8px;
            border-radius: 12px;
            font-size: 0.75rem;
            font-weight: bold;
        `;
        tweetEl.style.position = 'relative';
        tweetEl.appendChild(marker);
    });
}

// Share impact functionality
function shareImpact() {
    const shareText = `I'm part of the #billionshealed movement! 🌡️\n\nCurrent temperature: ${thermometerData.fillPercentage.toFixed(1)}%\nTotal thermometers: ${thermometerData.totalMinted}/100\n\nJoin us: https://billionshealed.com`;
    
    if (navigator.share) {
        navigator.share({
            title: 'BillionsHealed - My Impact',
            text: shareText,
            url: window.location.href
        }).catch(err => console.log('Error sharing:', err));
    } else {
        // Fallback: copy to clipboard
        navigator.clipboard.writeText(shareText).then(() => {
            alert('Share text copied to clipboard!');
        });
    }
}

// Manual refresh from Twitter API
async function manualRefreshTwitter() {
    const refreshBtn = document.getElementById('refresh-twitter-btn');
    const originalText = refreshBtn.textContent;
    
    refreshBtn.disabled = true;
    refreshBtn.textContent = 'Fetching from Twitter...';
    refreshBtn.style.animation = 'pulse 1s infinite';
    
    try {
        const response = await fetch(`${API_BASE_URL}/twitter/manual-refresh`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });
        
        const data = await response.json();
        
        if (data.success) {
            if (data.rate_limited) {
                alert(`Rate Limited!\n\n${data.message}\n\nShowing cached tweets from: ${new Date(data.cached_at).toLocaleString()}\n\nYou can refresh again in 24 hours.`);
            } else {
                alert(`Success!\n\nFetched ${data.count} fresh tweets with #billionshealed\n\nCached at: ${new Date(data.cached_at).toLocaleString()}`);
            }
            
            // Reload tweets to show fresh data
            await loadTweets();
            await updateCacheStatus();
        } else {
            alert(`Error refreshing tweets:\n\n${data.error}\n\n${data.details || ''}`);
        }
    } catch (error) {
        console.error('Error refreshing from Twitter:', error);
        alert(`Error refreshing tweets:\n\n${error.message}`);
    } finally {
        refreshBtn.disabled = false;
        refreshBtn.textContent = originalText;
        refreshBtn.style.animation = '';
    }
}

// Update cache status indicator
async function updateCacheStatus() {
    try {
        const response = await fetch(`${API_BASE_URL}/twitter/cache-status`);
        const data = await response.json();
        
        const statusEl = document.getElementById('cache-status');
        
        if (data.success && data.cache.exists) {
            statusEl.textContent = `Cached: ${data.cache.count} tweets (${data.cache.age})`;
            statusEl.style.color = '#4FC3F7';
        } else {
            statusEl.textContent = 'Demo Mode - Click refresh for real tweets';
            statusEl.style.color = '#FFA726';
        }
    } catch (error) {
        console.error('Error updating cache status:', error);
    }
}

// Render tweets
function renderTweets(tweets) {
    const container = document.getElementById('tweets-container');
    
    if (!tweets || tweets.length === 0) {
        container.innerHTML = `
            <div class="no-tweets">
                <div class="tweet-cta">
                    Be the first to tweet with <strong>#billionshealed</strong> and watch the thermometer rise!
                </div>
            </div>
        `;
        return;
    }
    
    container.innerHTML = tweets.map((tweet, index) => {
        const engagementLevel = getEngagementLevel(tweet.public_metrics);
        const timeAgo = getTimeAgo(tweet.created_at);
        
        return `
            <div class="tweet-item" data-tweet-index="${index}">
                <div class="tweet-header">
                    <div class="tweet-author">
                        <div class="author-avatar">X</div>
                        <div class="author-info">
                            <span class="author-name">${tweet.author?.name || 'X User'}</span>
                            <span class="author-handle">@${tweet.author?.username || 'user'}</span>
                        </div>
                    </div>
                    <div class="tweet-time">${timeAgo}</div>
                </div>
                
                <div class="tweet-content">
                    <p>${formatTweetText(tweet.text)}</p>
                </div>
                
                <div class="tweet-engagement">
                    <div class="engagement-stats">
                        ${tweet.public_metrics.like_count > 0 ? `<span class="engagement-item"><span class="engagement-icon">♥</span> ${tweet.public_metrics.like_count}</span>` : ''}
                        ${tweet.public_metrics.retweet_count > 0 ? `<span class="engagement-item"><span class="engagement-icon">↻</span> ${tweet.public_metrics.retweet_count}</span>` : ''}
                        ${tweet.public_metrics.reply_count > 0 ? `<span class="engagement-item"><span class="engagement-icon">↵</span> ${tweet.public_metrics.reply_count}</span>` : ''}
                    </div>
                    <div class="engagement-level ${engagementLevel}">
                        ${formatEngagement(tweet.public_metrics)}
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

// Format tweet text
function formatTweetText(text) {
    // Highlight #billionshealed
    return text.replace(/#billionshealed/gi, '<span class="tweet-hashtag">#billionshealed</span>');
}

// Get engagement level
function getEngagementLevel(metrics) {
    const total = (metrics.like_count || 0) + (metrics.retweet_count || 0) + (metrics.reply_count || 0);
    
    if (total < 5) return 'low';
    if (total < 20) return 'medium';
    return 'high';
}

// Format engagement
function formatEngagement(metrics) {
    const total = (metrics.like_count || 0) + (metrics.retweet_count || 0) + (metrics.reply_count || 0);
    
    if (total === 0) return 'No engagement';
    if (total < 10) return `${total} interactions`;
    if (total < 100) return `${total} interactions`;
    if (total < 1000) return `${(total / 10).toFixed(1)}k interactions`;
    return `${(total / 100).toFixed(1)}k interactions`;
}

// Get time ago
function getTimeAgo(dateString) {
    const now = new Date();
    const date = new Date(dateString);
    const diffInSeconds = Math.floor((now - date) / 1000);
    
    if (diffInSeconds < 60) return 'just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
    return `${Math.floor(diffInSeconds / 86400)}d ago`;
}

// Track tweet contribution
function addTweetContribution(thermometerNumber, source) {
    thermometerData.tweetContributions.push({
        thermometerNumber,
        source,
        timestamp: Date.now()
    });
}

// Add pulse animation to CSS
const style = document.createElement('style');
style.textContent = `
    @keyframes spin {
        from { transform: rotate(0deg); }
        to { transform: rotate(360deg); }
    }
    
    @keyframes glow {
        0%, 100% { 
            box-shadow: 
                0 8px 32px rgba(0, 150, 255, 0.3),
                inset 0 1px 0 rgba(255, 255, 255, 0.4),
                inset 0 -1px 0 rgba(0, 0, 0, 0.2),
                inset 2px 2px 10px rgba(255, 255, 255, 0.1),
                0 0 0 1px rgba(255, 255, 255, 0.2);
        }
        50% { 
            box-shadow: 
                0 8px 32px rgba(0, 150, 255, 0.8),
                inset 0 1px 0 rgba(255, 255, 255, 0.4),
                inset 0 -1px 0 rgba(0, 0, 0, 0.2),
                inset 2px 2px 10px rgba(255, 255, 255, 0.1),
                0 0 0 1px rgba(255, 255, 255, 0.2),
                0 0 50px rgba(0, 150, 255, 0.6);
        }
    }
`;
document.head.appendChild(style);

// Modal functions
function toggleNavPosition() {
    const navBox = document.getElementById('nav-box');
    const arrow = navBox.querySelector('.nav-move-arrow');
    
    if (navBox.classList.contains('right-side')) {
        navBox.classList.remove('right-side');
        arrow.textContent = '→';
    } else {
        navBox.classList.add('right-side');
        arrow.textContent = '←';
    }
}

function openInfoModal(type) {
    const modal = document.getElementById('info-modal');
    const title = document.getElementById('modal-title');
    const body = document.getElementById('modal-body');
    
    const modalContent = {
        what: {
            title: 'What is #BillionsHealed?',
            body: `
                <p>#BillionsHealed is a community-driven platform that visualizes global healing through social media engagement.</p>
                
                <h3>Interactive Thermometer</h3>
                <p>Watch the temperature rise from cold (blue) to boiling (red) as people share their healing journeys with #billionshealed on Twitter.</p>
                
                <h3>Twitter Integration</h3>
                <p>Every tweet with #billionshealed contributes to the temperature, creating a powerful visual representation of collective healing.</p>
                
                <h3>NFT System</h3>
                <p>Thermometer NFTs can be minted at progressive price points:
                    <ul>
                        <li>Cold (0-25): 0.01 SOL</li>
                        <li>Warm (26-50): 0.05 SOL</li>
                        <li>Hot (51-75): 0.10 SOL</li>
                        <li>Boiling (76-100): 0.25 SOL</li>
                    </ul>
                </p>
                
                <h3>Global Impact</h3>
                <p>Together, we create a visual representation of worldwide healing, one tweet at a time.</p>
            `
        },
        how: {
            title: 'How Does It Work?',
            body: `
                <h3>1. Tweet with #billionshealed</h3>
                <p>Share your healing journey, progress, or support for others using the #billionshealed hashtag on Twitter.</p>
                
                <h3>2. Engagement Matters</h3>
                <p>Tweets with high engagement (likes, retweets, replies) contribute more to the thermometer temperature.</p>
                
                <h3>3. Watch the Temperature Rise</h3>
                <p>As more people participate, the thermometer fills up and changes color, progressing through four temperature levels.</p>
                
                <h3>4. See Your Impact</h3>
                <p>Visual lines connect your tweets to the thermometer, showing exactly how much you've contributed to the collective healing.</p>
                
                <h3>5. Share Your Progress</h3>
                <p>Use the share button to show friends your impact on the global healing movement.</p>
            `
        },
        whitepaper: {
            title: 'Whitepaper',
            body: `
                <h3>Vision</h3>
                <p>BillionsHealed aims to create a decentralized platform for tracking and celebrating global healing through social media and blockchain technology.</p>
                
                <h3>Technology Stack</h3>
                <ul>
                    <li><strong>Frontend:</strong> Pure HTML/CSS/JavaScript (zero dependencies)</li>
                    <li><strong>Backend:</strong> Node.js + Express API</li>
                    <li><strong>Social Integration:</strong> Twitter API v2</li>
                    <li><strong>Blockchain:</strong> Solana (optional OASIS integration)</li>
                </ul>
                
                <h3>Thermometer Algorithm</h3>
                <p>The thermometer tracks 100 NFTs across 4 temperature levels with progressive pricing. Each tweet contributes to the temperature based on engagement metrics.</p>
                
                <h3>Social Impact Scoring</h3>
                <p>Tweets are scored based on:
                    <ul>
                        <li>Base impact: 1 point per tweet</li>
                        <li>Likes: +1 point per 10 likes</li>
                        <li>Retweets: +1 point per 5 retweets</li>
                        <li>Replies: +1 point per 3 replies</li>
                        <li>Maximum: 5 points per tweet</li>
                    </ul>
                </p>
            `
        },
        deliverables: {
            title: 'Deliverables & Roadmap',
            body: `
                <h3>Current Features (v1.0)</h3>
                <ul>
                    <li>Interactive thermometer visualization</li>
                    <li>Twitter feed integration (#billionshealed)</li>
                    <li>Progressive temperature levels</li>
                    <li>Visual tweet-to-thermometer linking</li>
                    <li>Share functionality</li>
                    <li>Real-time statistics</li>
                    <li>Responsive design</li>
                </ul>
                
                <h3>Planned Features (v2.0)</h3>
                <ul>
                    <li>OASIS blockchain integration</li>
                    <li>Actual NFT minting on Solana</li>
                    <li>User profiles and achievements</li>
                    <li>Leaderboards</li>
                    <li>Multi-language support</li>
                </ul>
                
                <h3>Future Vision (v3.0+)</h3>
                <ul>
                    <li>Cross-platform integration (Instagram, TikTok)</li>
                    <li>AI sentiment analysis</li>
                    <li>NFT marketplace</li>
                    <li>Mobile app (iOS/Android)</li>
                    <li>Community governance (DAO)</li>
                </ul>
            `
        }
    };
    
    if (modalContent[type]) {
        title.textContent = modalContent[type].title;
        body.innerHTML = modalContent[type].body;
        modal.style.display = 'flex';
    }
}

function closeInfoModal() {
    const modal = document.getElementById('info-modal');
    modal.style.display = 'none';
}

// Close modal on background click
document.addEventListener('click', (e) => {
    const modal = document.getElementById('info-modal');
    if (e.target === modal) {
        closeInfoModal();
    }
});




