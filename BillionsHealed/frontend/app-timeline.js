// BillionsHealed Frontend Application - Timeline Version
const API_BASE_URL = 'http://localhost:3002/api';

// State
let thermometerData = {
    totalMinted: 0,
    maxThermometers: 100,
    fillPercentage: 0,
    currentLevel: { level: 'cold', name: 'Cold', color: '#4FC3F7', price: 0.01 },
    processedTweets: 0,
    tweets: [],
    tweetContributions: []
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
            drawTimelineWithConnections();
        }
    } catch (error) {
        console.error('Error loading thermometer status:', error);
    }
}

// Update thermometer UI
function updateThermometerUI() {
    const liquid = document.getElementById('thermometer-liquid');
    liquid.style.height = `${thermometerData.fillPercentage}%`;
    liquid.style.background = thermometerData.currentLevel.color;
    
    const bulb = document.getElementById('thermometer-bulb');
    bulb.style.background = thermometerData.currentLevel.color;
    
    document.getElementById('current-level').textContent = thermometerData.currentLevel.name;
    document.getElementById('nft-count').textContent = `${thermometerData.totalMinted}/${thermometerData.maxThermometers} NFTs`;
    document.getElementById('current-price').textContent = `Price: ${thermometerData.currentLevel.price} SOL`;
    document.getElementById('fill-percentage').textContent = `${thermometerData.fillPercentage.toFixed(1)}%`;
    document.getElementById('remaining-count').textContent = thermometerData.maxThermometers - thermometerData.totalMinted;
    
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
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ source: 'manual', metadata: { timestamp: Date.now() } })
        });
        
        const data = await response.json();
        
        if (data.success) {
            const bulb = document.getElementById('thermometer-bulb');
            bulb.style.boxShadow = `0 8px 32px rgba(0, 150, 255, 0.8), inset 0 1px 0 rgba(255, 255, 255, 0.4), inset 0 -1px 0 rgba(0, 0, 0, 0.2), inset 2px 2px 10px rgba(255, 255, 255, 0.1), 0 0 0 1px rgba(255, 255, 255, 0.2), 0 0 50px rgba(0, 150, 255, 0.6)`;
            setTimeout(() => {
                bulb.style.boxShadow = `0 8px 32px rgba(0, 150, 255, 0.3), inset 0 1px 0 rgba(255, 255, 255, 0.4), inset 0 -1px 0 rgba(0, 0, 0, 0.2), inset 2px 2px 10px rgba(255, 255, 255, 0.1), 0 0 0 1px rgba(255, 255, 255, 0.2)`;
            }, 800);
            
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

// Load tweets
async function loadTweets() {
    try {
        const response = await fetch(`${API_BASE_URL}/twitter/recent-tweets?limit=10`);
        const data = await response.json();
        
        if (data.success && data.tweets) {
            thermometerData.tweets = data.tweets;
            calculateTweetContributions(data.tweets);
            drawTimelineWithConnections();
        }
    } catch (error) {
        console.error('Error loading tweets:', error);
    }
}

// Calculate tweet contributions (newest at top, oldest at bottom)
function calculateTweetContributions(tweets) {
    thermometerData.tweetContributions = [];
    let currentTemp = 0;
    
    // Reverse to process oldest first (they contribute to lower temperatures)
    const oldestFirst = [...tweets].reverse();
    
    oldestFirst.forEach((tweet, index) => {
        const engagement = (tweet.public_metrics.like_count || 0) + 
                         (tweet.public_metrics.retweet_count || 0) * 2 + 
                         (tweet.public_metrics.reply_count || 0);
        
        const tempIncrease = Math.min(5, Math.floor(engagement / 10) + 1);
        currentTemp += tempIncrease;
        
        thermometerData.tweetContributions.push({
            tweet: tweet,
            tweetId: tweet.id,
            author: tweet.author?.username || 'user',
            authorName: tweet.author?.name || 'User',
            text: tweet.text,
            temperature: Math.min(100, currentTemp),
            tempIncrease: tempIncrease,
            side: index % 2 === 0 ? 'left' : 'right', // Alternate sides
            metrics: tweet.public_metrics,
            createdAt: tweet.created_at
        });
    });
    
    // Reverse back so newest is first in array
    thermometerData.tweetContributions.reverse();
}

// Draw timeline with hoverable markers
function drawTimelineWithConnections() {
    const timelineContainer = document.getElementById('tweet-timeline');
    if (!timelineContainer) return;
    
    // Clear existing
    timelineContainer.innerHTML = '';
    
    if (thermometerData.tweetContributions.length === 0) {
        timelineContainer.innerHTML = '<div class="no-tweets-timeline">No tweets yet. Click "Refresh from Twitter" to load tweets with #billionshealed</div>';
        return;
    }
    
    const thermoRect = document.querySelector('.thermometer-tube')?.getBoundingClientRect();
    if (!thermoRect) return;
    
    // Create SVG for lines
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.id = 'timeline-connections';
    svg.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:1;';
    timelineContainer.appendChild(svg);
    
    // Create markers for each tweet (newest at top, oldest at bottom)
    thermometerData.tweetContributions.forEach((contribution, index) => {
        const marker = createTweetMarker(contribution, index);
        timelineContainer.appendChild(marker);
        
        // Draw connection line after marker is in DOM
        setTimeout(() => drawConnectionLine(svg, marker, contribution, thermoRect), 10);
    });
}

// Create hoverable tweet marker
function createTweetMarker(contribution, index) {
    const marker = document.createElement('div');
    marker.className = `tweet-marker ${contribution.side}`;
    marker.dataset.index = index;
    
    // Compact marker view
    marker.innerHTML = `
        <div class="marker-compact">
            <div class="marker-avatar">X</div>
            <div class="marker-info">
                <div class="marker-author">@${contribution.author}</div>
                <div class="marker-temp">+${contribution.tempIncrease}° → ${contribution.temperature}%</div>
            </div>
        </div>
        
        <div class="marker-expanded">
            <div class="expanded-header">
                <div class="expanded-avatar">X</div>
                <div class="expanded-author-info">
                    <div class="expanded-name">${contribution.authorName}</div>
                    <div class="expanded-handle">@${contribution.author}</div>
                </div>
                <div class="expanded-time">${getTimeAgo(contribution.createdAt)}</div>
            </div>
            <div class="expanded-content">
                ${formatTweetText(contribution.text)}
            </div>
            <div class="expanded-metrics">
                <span>♥ ${contribution.metrics.like_count || 0}</span>
                <span>↻ ${contribution.metrics.retweet_count || 0}</span>
                <span>↵ ${contribution.metrics.reply_count || 0}</span>
            </div>
            <div class="expanded-impact">
                Contributed +${contribution.tempIncrease}° to reach ${contribution.temperature}%
            </div>
        </div>
    `;
    
    return marker;
}

// Draw connection line from marker to thermometer
function drawConnectionLine(svg, marker, contribution, thermoRect) {
    const markerRect = marker.getBoundingClientRect();
    const containerRect = svg.parentElement.getBoundingClientRect();
    
    // Calculate thermometer Y position for this temperature (from bottom)
    const tempY = thermoRect.bottom - (thermoRect.height * (contribution.temperature / 100));
    
    // Start point (from marker)
    const startX = contribution.side === 'left' 
        ? markerRect.right - containerRect.left
        : markerRect.left - containerRect.left;
    const startY = markerRect.top + markerRect.height / 2 - containerRect.top;
    
    // End point (thermometer)
    const endX = contribution.side === 'left'
        ? thermoRect.left - containerRect.left
        : thermoRect.right - containerRect.left;
    const endY = tempY - containerRect.top;
    
    // Create line
    const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
    line.setAttribute('x1', startX);
    line.setAttribute('y1', startY);
    line.setAttribute('x2', endX);
    line.setAttribute('y2', endY);
    line.setAttribute('stroke', getColorForTemperature(contribution.temperature));
    line.setAttribute('stroke-width', '2');
    line.setAttribute('stroke-dasharray', '5,5');
    line.setAttribute('opacity', '0.6');
    line.setAttribute('class', 'connection-line');
    
    svg.appendChild(line);
}

// Get color based on temperature level
function getColorForTemperature(temp) {
    if (temp <= 25) return '#4FC3F7';
    if (temp <= 50) return '#FFEB3B';
    if (temp <= 75) return '#FF9800';
    return '#F44336';
}

// Share impact - opens Twitter with pre-filled tweet
function shareImpact() {
    const totalContribution = thermometerData.tweetContributions
        .reduce((sum, c) => sum + c.tempIncrease, 0);
    
    const currentTemp = thermometerData.fillPercentage.toFixed(1);
    const tweetText = `I'm part of the #billionshealed movement!\n\nCurrent temperature: ${currentTemp}%\nMy contribution: +${totalContribution}°\n\nJoin the healing: ${window.location.origin}`;
    
    const twitterUrl = `https://twitter.com/intent/tweet?text=${encodeURIComponent(tweetText)}&hashtags=billionshealed`;
    window.open(twitterUrl, '_blank');
}

// Manual refresh from Twitter
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

// Update cache status
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

// Helper functions
function formatTweetText(text) {
    return text.replace(/#billionshealed/gi, '<span class="tweet-hashtag">#billionshealed</span>');
}

function getTimeAgo(dateString) {
    const now = new Date();
    const date = new Date(dateString);
    const diffInSeconds = Math.floor((now - date) / 1000);
    
    if (diffInSeconds < 60) return 'just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
    return `${Math.floor(diffInSeconds / 86400)}d ago`;
}

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
                <h3>Tweet Timeline</h3>
                <p>See how each tweet contributes to the temperature. Hover over markers to see full tweets and their impact.</p>
                <h3>Share Your Impact</h3>
                <p>Click "Share My Impact" to tweet about your contribution to the movement.</p>
            `
        },
        how: {
            title: 'How Does It Work?',
            body: `
                <h3>1. Tweet with #billionshealed</h3>
                <p>Share your healing journey using the #billionshealed hashtag on Twitter.</p>
                <h3>2. Watch Your Impact</h3>
                <p>Your tweet appears on the timeline with a marker showing how much temperature it added.</p>
                <h3>3. Hover to Explore</h3>
                <p>Hover over any marker to see the full tweet and its contribution details.</p>
                <h3>4. Share Your Progress</h3>
                <p>Use "Share My Impact" to show friends your contribution to the global healing movement.</p>
            `
        },
        whitepaper: {
            title: 'Whitepaper',
            body: `
                <h3>Vision</h3>
                <p>BillionsHealed creates a visual representation of collective healing through Twitter engagement and blockchain technology.</p>
                <h3>Technology</h3>
                <p>Built with HTML/CSS/JavaScript, Node.js backend, Twitter API v2, and optional Solana integration.</p>
                <h3>Impact Scoring</h3>
                <p>Each tweet contributes 1-5° based on engagement: likes, retweets, and replies combine to create measurable impact.</p>
            `
        },
        deliverables: {
            title: 'Deliverables & Roadmap',
            body: `
                <h3>Current Features</h3>
                <ul>
                    <li>Interactive thermometer visualization</li>
                    <li>Tweet timeline with hoverable markers</li>
                    <li>Visual connection lines</li>
                    <li>Share to Twitter functionality</li>
                    <li>Real-time statistics</li>
                </ul>
                <h3>Planned Features</h3>
                <ul>
                    <li>Actual NFT minting on Solana</li>
                    <li>User profiles and achievements</li>
                    <li>Leaderboards</li>
                    <li>Mobile app</li>
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

document.addEventListener('click', (e) => {
    const modal = document.getElementById('info-modal');
    if (e.target === modal) closeInfoModal();
});

// Add animations
const style = document.createElement('style');
style.textContent = `
    @keyframes pulse {
        0%, 100% { opacity: 1; }
        50% { opacity: 0.6; }
    }
`;
document.head.appendChild(style);

