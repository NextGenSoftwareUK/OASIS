// BillionsHealed Frontend Application
const API_BASE_URL = 'http://localhost:3002/api';

// State
let thermometerData = {
    totalMinted: 0,
    maxThermometers: 100,
    fillPercentage: 0,
    currentLevel: { level: 'cold', name: '❄️ Cold', color: '#4FC3F7', price: 0.01 },
    processedTweets: 0
};

// Temperature levels
const temperatureLevels = [
    { level: 'cold', range: [0, 25], price: 0.01, color: '#4FC3F7', name: '❄️ Cold' },
    { level: 'warm', range: [26, 50], price: 0.05, color: '#FFEB3B', name: '🌡️ Warm' },
    { level: 'hot', range: [51, 75], price: 0.10, color: '#FF9800', name: '🔥 Hot' },
    { level: 'boiling', range: [76, 100], price: 0.25, color: '#F44336', name: '🌋 Boiling' }
];

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    console.log('🌡️ BillionsHealed initialized');
    initializeApp();
});

async function initializeApp() {
    // Set up event listeners
    document.getElementById('mint-btn').addEventListener('click', mintThermometer);
    document.getElementById('refresh-btn').addEventListener('click', loadTweets);
    
    // Load initial data
    await loadThermometerStatus();
    await loadTweets();
    
    // Auto-refresh every minute
    setInterval(async () => {
        await loadThermometerStatus();
        await loadTweets();
    }, 60000);
}

// Load thermometer status
async function loadThermometerStatus() {
    try {
        const response = await fetch(`${API_BASE_URL}/thermometer/status`);
        const data = await response.json();
        
        if (data.success) {
            thermometerData = {
                totalMinted: data.status.totalMinted,
                maxThermometers: data.status.maxThermometers,
                fillPercentage: data.status.fillPercentage,
                currentLevel: data.status.currentLevel,
                processedTweets: thermometerData.processedTweets // Keep current value
            };
            
            updateThermometerUI();
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
        mintBtn.textContent = '🌋 All Minted!';
    }
    
    // Update feed stats
    document.getElementById('total-minted').textContent = thermometerData.totalMinted;
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
            console.log('✅ Thermometer minted:', data);
            
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

// Load tweets
async function loadTweets() {
    const container = document.getElementById('tweets-container');
    const refreshBtn = document.getElementById('refresh-btn');
    
    refreshBtn.style.animation = 'spin 1s linear infinite';
    
    try {
        const response = await fetch(`${API_BASE_URL}/twitter/recent-tweets?limit=10`);
        const data = await response.json();
        
        if (data.success && data.tweets) {
            renderTweets(data.tweets);
            thermometerData.processedTweets = data.meta.result_count;
            document.getElementById('processed-tweets').textContent = data.meta.result_count;
        } else {
            container.innerHTML = '<div class="loading">No tweets available</div>';
        }
    } catch (error) {
        console.error('Error loading tweets:', error);
        container.innerHTML = '<div class="loading">Error loading tweets</div>';
    } finally {
        refreshBtn.style.animation = '';
    }
}

// Render tweets
function renderTweets(tweets) {
    const container = document.getElementById('tweets-container');
    
    if (!tweets || tweets.length === 0) {
        container.innerHTML = `
            <div class="no-tweets">
                <div class="tweet-cta">
                    Be the first to tweet with <strong>#billionshealed</strong> and watch the thermometer rise! 🌟
                </div>
            </div>
        `;
        return;
    }
    
    container.innerHTML = tweets.map(tweet => {
        const engagementLevel = getEngagementLevel(tweet.public_metrics);
        const timeAgo = getTimeAgo(tweet.created_at);
        
        return `
            <div class="tweet-item">
                <div class="tweet-header">
                    <div class="tweet-author">
                        <div class="author-avatar">𝕏</div>
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
                        ${tweet.public_metrics.like_count > 0 ? `<span class="engagement-item"><span class="engagement-icon">❤️</span> ${tweet.public_metrics.like_count}</span>` : ''}
                        ${tweet.public_metrics.retweet_count > 0 ? `<span class="engagement-item"><span class="engagement-icon">🔄</span> ${tweet.public_metrics.retweet_count}</span>` : ''}
                        ${tweet.public_metrics.reply_count > 0 ? `<span class="engagement-item"><span class="engagement-icon">💬</span> ${tweet.public_metrics.reply_count}</span>` : ''}
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

// Modal Content Data
const modalContent = {
    what: {
        title: 'What is #BillionsHealed?',
        body: `
            <p>#BillionsHealed is a community-driven platform that visualizes global healing through social media engagement.</p>
            
            <h3>🌡️ Interactive Thermometer</h3>
            <p>Watch the temperature rise from cold (blue) to boiling (red) as people share their healing journeys with #billionshealed on Twitter.</p>
            
            <h3>🐦 Twitter Integration</h3>
            <p>Every tweet with #billionshealed contributes to the temperature, creating a powerful visual representation of collective healing.</p>
            
            <h3>💎 NFT System</h3>
            <p>Thermometer NFTs can be minted at progressive price points:
                <ul>
                    <li>❄️ Cold (0-25): 0.01 SOL</li>
                    <li>🌡️ Warm (26-50): 0.05 SOL</li>
                    <li>🔥 Hot (51-75): 0.10 SOL</li>
                    <li>🌋 Boiling (76-100): 0.25 SOL</li>
                </ul>
            </p>
            
            <h3>🌍 Global Impact</h3>
            <p>Together, we create a visual representation of worldwide healing, one tweet at a time.</p>
        `
    },
    how: {
        title: 'How Does It Work?',
        body: `
            <h3>1️⃣ Tweet with #billionshealed</h3>
            <p>Share your healing journey, progress, or support for others using the #billionshealed hashtag on Twitter.</p>
            
            <h3>2️⃣ Engagement Matters</h3>
            <p>Tweets with high engagement (likes, retweets, replies) contribute more to the thermometer temperature.</p>
            
            <h3>3️⃣ Watch the Temperature Rise</h3>
            <p>As more people participate, the thermometer fills up and changes color, progressing through four temperature levels.</p>
            
            <h3>4️⃣ Mint Thermometer NFTs</h3>
            <p>Anyone can mint a thermometer NFT at any time. The price increases as the temperature rises.</p>
            
            <h3>5️⃣ Community Impact</h3>
            <p>The thermometer serves as a real-time visualization of global healing awareness and community support.</p>
            
            <h3>🔄 Auto-Refresh</h3>
            <p>The feed updates automatically every minute to show the latest tweets and thermometer progress.</p>
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
            <p>The thermometer tracks 100 NFTs across 4 temperature levels with progressive pricing:</p>
            <ul>
                <li>Each level has 25 NFTs available</li>
                <li>Price increases at each level</li>
                <li>Visual liquid rises and changes color</li>
                <li>Tweet engagement contributes to progress</li>
            </ul>
            
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
            
            <h3>Future Roadmap</h3>
            <p>See the Deliverables section for upcoming features and enhancements.</p>
        `
    },
    deliverables: {
        title: 'Deliverables & Roadmap',
        body: `
            <h3>✅ Current Features (v1.0)</h3>
            <ul>
                <li>Interactive thermometer visualization</li>
                <li>Twitter feed integration (#billionshealed)</li>
                <li>Progressive temperature levels</li>
                <li>Manual thermometer minting</li>
                <li>Real-time statistics</li>
                <li>Responsive design</li>
            </ul>
            
            <h3>🚧 In Progress (v1.5)</h3>
            <ul>
                <li>Live Twitter API integration (currently using mock data)</li>
                <li>WebSocket for real-time updates</li>
                <li>User authentication</li>
                <li>Wallet connection (Phantom/MetaMask)</li>
            </ul>
            
            <h3>📅 Planned Features (v2.0)</h3>
            <ul>
                <li>OASIS blockchain integration</li>
                <li>Actual NFT minting on Solana</li>
                <li>User profiles and achievements</li>
                <li>Leaderboards</li>
                <li>Social sharing features</li>
                <li>Multi-language support</li>
            </ul>
            
            <h3>🌟 Future Vision (v3.0+)</h3>
            <ul>
                <li>Cross-platform integration (Instagram, TikTok)</li>
                <li>AI sentiment analysis</li>
                <li>NFT marketplace</li>
                <li>Mobile app (iOS/Android)</li>
                <li>Community governance (DAO)</li>
                <li>Healing impact analytics</li>
            </ul>
            
            <h3>🎯 Mission</h3>
            <p>Create the world's largest decentralized platform for tracking, celebrating, and incentivizing global healing.</p>
        `
    }
};

// Toggle Navigation Position
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

// Open Info Modal
function openInfoModal(type) {
    const modal = document.getElementById('info-modal');
    const title = document.getElementById('modal-title');
    const body = document.getElementById('modal-body');
    
    if (modalContent[type]) {
        title.textContent = modalContent[type].title;
        body.innerHTML = modalContent[type].body;
        modal.style.display = 'flex';
    }
}

// Close Info Modal
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
