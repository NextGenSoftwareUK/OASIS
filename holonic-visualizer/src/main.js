import { HolonicVisualizer } from './visualizer/HolonicVisualizer.js';
import { MockDataGenerator } from './data/MockDataGenerator.js';
import { OASISClient } from './api/OASISClient.js';
import { OASISDataTransformer } from './data/OASISDataTransformer.js';

// Initialize the visualizer
const container = document.getElementById('canvas-container');
const visualizer = new HolonicVisualizer(container);

// Initialize OASIS client
// Use HTTP (port 5003) for localhost to avoid HTTP/2 protocol errors
const oasisClient = new OASISClient({
    baseUrl: import.meta.env.VITE_OASIS_API_URL || 'http://localhost:5003',
    username: import.meta.env.VITE_OASIS_USERNAME || 'OASIS_ADMIN',
    password: import.meta.env.VITE_OASIS_PASSWORD || 'Uppermall1!'
});

// Notification log system - subtle corner log
let notificationLog = [];

function showNotification(message, type = 'success') {
    // Create log container if it doesn't exist
    let logContainer = document.getElementById('notification-log');
    if (!logContainer) {
        logContainer = document.createElement('div');
        logContainer.id = 'notification-log';
        logContainer.style.cssText = `
            position: fixed;
            bottom: 20px;
            right: 20px;
            width: 300px;
            max-height: 400px;
            overflow-y: auto;
            z-index: 1000;
            pointer-events: none;
        `;
        document.body.appendChild(logContainer);
    }
    
    // Create log entry
    const logEntry = document.createElement('div');
    const timestamp = new Date().toLocaleTimeString();
    logEntry.style.cssText = `
        background: ${type === 'error' ? 'rgba(255, 0, 0, 0.3)' : 'rgba(0, 255, 255, 0.2)'};
        color: ${type === 'error' ? '#ff6666' : '#00ffff'};
        padding: 8px 12px;
        margin-bottom: 6px;
        border-radius: 6px;
        font-size: 12px;
        font-weight: 500;
        border-left: 3px solid ${type === 'error' ? '#ff0000' : '#00ffff'};
        backdrop-filter: blur(5px);
        white-space: pre-line;
        line-height: 1.4;
        animation: logEntrySlideIn 0.3s ease-out;
        pointer-events: auto;
    `;
    logEntry.innerHTML = `<div style="opacity: 0.7; font-size: 10px; margin-bottom: 2px;">${timestamp}</div>${message}`;
    
    logContainer.appendChild(logEntry);
    notificationLog.push(logEntry);
    
    // Limit log entries to 10
    if (notificationLog.length > 10) {
        const oldest = notificationLog.shift();
        oldest.style.animation = 'logEntryFadeOut 0.3s ease-out';
        setTimeout(() => oldest.remove(), 300);
    }
    
    // Auto-remove after 8 seconds (longer for log)
    setTimeout(() => {
        if (logEntry.parentNode) {
            logEntry.style.animation = 'logEntryFadeOut 0.3s ease-out';
            setTimeout(() => {
                if (logEntry.parentNode) {
                    logEntry.remove();
                    const index = notificationLog.indexOf(logEntry);
                    if (index > -1) notificationLog.splice(index, 1);
                }
            }, 300);
        }
    }, 8000);
}

// Add animation styles
const style = document.createElement('style');
style.textContent = `
    @keyframes logEntrySlideIn {
        0% { transform: translateX(100%); opacity: 0; }
        100% { transform: translateX(0); opacity: 1; }
    }
    @keyframes logEntryFadeOut {
        0% { opacity: 1; transform: translateX(0); }
        100% { opacity: 0; transform: translateX(100%); }
    }
    @keyframes buttonPulse {
        0%, 100% { box-shadow: 0 0 10px currentColor; }
        50% { box-shadow: 0 0 40px currentColor, 0 0 60px currentColor; }
    }
    #notification-log::-webkit-scrollbar {
        width: 4px;
    }
    #notification-log::-webkit-scrollbar-track {
        background: rgba(0, 0, 0, 0.2);
        border-radius: 2px;
    }
    #notification-log::-webkit-scrollbar-thumb {
        background: rgba(0, 255, 255, 0.5);
        border-radius: 2px;
    }
`;
document.head.appendChild(style);

// Setup controls
document.getElementById('btn-reset-camera').addEventListener('click', () => {
    visualizer.resetCamera();
});

document.getElementById('btn-toggle-orbits').addEventListener('click', () => {
    visualizer.toggleOrbits();
});

document.getElementById('btn-toggle-labels').addEventListener('click', () => {
    visualizer.toggleLabels();
});

function generateWithFeedback(type, typeName) {
    try {
        // Visual feedback on button
        const button = document.getElementById(`btn-generate-${type}`);
        const originalStyle = button.style.cssText;
        button.style.animation = 'buttonPulse 0.5s ease-in-out';
        button.style.transform = 'scale(0.95)';
        
        setTimeout(() => {
            button.style.cssText = originalStyle;
        }, 500);
        
        // Show generating notification (subtle log)
        showNotification(`Generating ${typeName}...`, 'success');
        
        const mockData = MockDataGenerator.generate({ type: type, count: 1 });
        
        setTimeout(() => {
            visualizer.loadData(mockData);
            updateStats(mockData);
            
            // Show success notification (subtle log)
            const holonCount = mockData.holons.length;
            const oappName = mockData.oapps[0]?.name || typeName;
            showNotification(
                `${typeName} created: ${oappName}\n${holonCount.toLocaleString()} holons`,
                'success'
            );
        }, 100);
    } catch (error) {
        console.error(`Error generating ${type}:`, error);
        showNotification(`Error creating ${typeName}: ${error.message}`, 'error');
    }
}

document.getElementById('btn-generate-moon').addEventListener('click', () => {
    generateWithFeedback('moon', 'MOON');
});

document.getElementById('btn-generate-planet').addEventListener('click', () => {
    generateWithFeedback('planet', 'PLANET');
});

document.getElementById('btn-generate-star').addEventListener('click', () => {
    generateWithFeedback('star', 'STAR');
});

document.getElementById('btn-generate-random').addEventListener('click', () => {
    try {
        const button = document.getElementById('btn-generate-random');
        const originalStyle = button.style.cssText;
        button.style.animation = 'buttonPulse 0.5s ease-in-out';
        
        showNotification('Generating random mix...', 'success');
        
        const mockData = MockDataGenerator.generate();
        
        setTimeout(() => {
            visualizer.loadData(mockData);
            updateStats(mockData);
            
            const oappCount = mockData.oapps.length;
            const holonCount = mockData.holons.length;
            showNotification(
                `Random mix created: ${oappCount} OAPP(s), ${holonCount.toLocaleString()} holons`,
                'success'
            );
            
            button.style.cssText = originalStyle;
        }, 100);
    } catch (error) {
        console.error('Error generating random:', error);
        showNotification(`❌ ERROR: ${error.message}`, 'error');
    }
});

document.getElementById('btn-clear').addEventListener('click', () => {
    visualizer.clear();
    updateStats({ oapps: [], holons: [] });
    showNotification('Cleared all', 'success');
});

// Store current avatar ID for filtering
let currentAvatarId = null;

// Load data from OASIS API
async function loadFromOASIS(avatarId = null) {
    try {
        showNotification('Connecting to OASIS...', 'success');
        
        // Authenticate first
        showNotification('Authenticating...', 'success');
        await oasisClient.authenticate();
        
        // If avatarId is provided, get holons for that avatar, otherwise get all
        let holons, oapps;
        
        if (avatarId) {
            showNotification(`Loading holons for avatar...`, 'success');
            holons = await oasisClient.getHolonsForAvatar(avatarId);
            // Get OAPPs that belong to this avatar's holons
            const holonOAPPIds = [...new Set(holons.map(h => h.oappId || h.OAPPId || h.metadata?.oappId || h.MetaData?.oappId).filter(Boolean))];
            oapps = await oasisClient.getAllOAPPs();
            // Filter OAPPs to only those with holons from this avatar
            oapps = oapps.filter(oapp => {
                const oappId = oapp.id || oapp.Id;
                return holonOAPPIds.includes(oappId);
            });
        } else {
            showNotification('Loading OAPPs and Avatars...', 'success');
            
            // Instead of loading "All" (which can be too large and cause chunked encoding errors),
            // load specific types separately to avoid large responses
            let allHolons = [];
            
            try {
                // Load OAPPs using POST with LoadChildren: false to avoid loading all child holons
                // This prevents ERR_INCOMPLETE_CHUNKED_ENCODING errors
                showNotification('Loading OAPPs...', 'success');
                let oappHolons = await oasisClient.getAllHolonsWithOptions(74, { LoadChildren: false });
                if (!Array.isArray(oappHolons) || oappHolons.length === 0) {
                    console.log('Trying OAPP as string...');
                    oappHolons = await oasisClient.getAllHolonsWithOptions('OAPP', { LoadChildren: false });
                }
                if (Array.isArray(oappHolons)) {
                    allHolons.push(...oappHolons);
                    console.log(`Loaded ${oappHolons.length} OAPPs`);
                } else if (oappHolons && oappHolons.errorType === 'INCOMPLETE_CHUNKED_ENCODING') {
                    showNotification('OAPP response too large. Try using "Seed Sample Data" to create test data.', 'error');
                    return;
                }
                
                // Load Avatars using POST with LoadChildren: false
                showNotification('Loading Avatars...', 'success');
                let avatarHolons = await oasisClient.getAllHolonsWithOptions(3, { LoadChildren: false });
                if (!Array.isArray(avatarHolons) || avatarHolons.length === 0) {
                    console.log('Trying Avatar as string...');
                    avatarHolons = await oasisClient.getAllHolonsWithOptions('Avatar', { LoadChildren: false });
                }
                if (Array.isArray(avatarHolons)) {
                    allHolons.push(...avatarHolons);
                    console.log(`Loaded ${avatarHolons.length} Avatars`);
                } else if (avatarHolons && avatarHolons.errorType === 'INCOMPLETE_CHUNKED_ENCODING') {
                    showNotification('Avatar response too large. Try using "Seed Sample Data" to create test data.', 'error');
                    return;
                }
                
                // Load regular holons (type 0) - these should be smaller batches
                showNotification('Loading regular holons...', 'success');
                const regularHolons = await oasisClient.getAllHolonsWithOptions(0, { LoadChildren: false });
                if (Array.isArray(regularHolons)) {
                    // Filter out any OAPPs or Avatars that might have been returned
                    const filtered = regularHolons.filter(h => {
                        const type = h.holonType || h.HolonType || h.type;
                        return type !== 74 && type !== 3 && type !== 'OAPP' && type !== 'Avatar';
                    });
                    allHolons.push(...filtered);
                    console.log(`Loaded ${filtered.length} regular holons (filtered from ${regularHolons.length} total)`);
                }
                
                holons = allHolons;
            } catch (error) {
                console.warn('Error loading specific holon types:', error);
                showNotification(`Error loading data: ${error.message}. Try using "Seed Sample Data" to create test data.`, 'error');
                return;
            }
            
            // Ensure holons is an array
            if (!Array.isArray(holons)) {
                console.warn('getAllHolons returned non-array:', holons);
                holons = [];
            }
            
            // Separate OAPPs and avatars from other holons
            oapps = holons.filter(h => {
                const type = h.holonType || h.HolonType || h.type;
                return type === 'OAPP' || type === 74; // OAPP is enum value 74
            });
            
            const avatars = holons.filter(h => {
                const type = h.holonType || h.HolonType || h.type;
                return type === 'Avatar' || type === 3; // Avatar is enum value 3
            });
            
            // Filter out OAPPs and avatars from the main holons list (they'll be handled separately)
            holons = holons.filter(h => {
                const type = h.holonType || h.HolonType || h.type;
                return type !== 'OAPP' && type !== 74 && type !== 'Avatar' && type !== 3;
            });
            
            // Log what we found
            console.log(`Found ${holons.length} regular holons, ${avatars.length} avatars, ${oapps.length} OAPPs`);
        }

        // Ensure oapps and holons are arrays (handle undefined/null)
        if (!Array.isArray(holons)) holons = [];
        if (!Array.isArray(oapps)) oapps = [];
        
        const totalItems = holons.length + oapps.length;
        if (totalItems === 0) {
            // Check if this is due to a chunked encoding error (response too large)
            // vs actually no data
            const hasChunkedError = holons === null || (typeof holons === 'object' && holons.errorType === 'INCOMPLETE_CHUNKED_ENCODING');
            if (hasChunkedError) {
                showNotification('Response too large. Try using "Seed Sample Data" to create test data, or request specific holon types.', 'error');
            } else {
                showNotification('No data found in OASIS. Try using "Seed Sample Data" to create some test data.', 'error');
            }
            return;
        }

        // Transform data
        let data = OASISDataTransformer.transformToVisualizerFormat(oapps, holons);
        
        // Limit data if too large for performance
        if (data.holons.length > 50000) {
            showNotification(`Large dataset detected (${data.holons.length} holons). Sampling for performance...`, 'success');
            data = OASISDataTransformer.limitData(data, 50000);
        }

        // Load into visualizer
        visualizer.loadData(data);
        updateStats(data);

        const oappCount = data.oapps.length;
        const holonCount = data.holons.length;
        const avatarText = avatarId ? ` (Avatar: ${avatarId.substring(0, 8)}...)` : '';
        showNotification(
            `Loaded from OASIS${avatarText}\n${oappCount} OAPP(s)\n${holonCount.toLocaleString()} holon(s)`,
            'success'
        );
        
        currentAvatarId = avatarId;
    } catch (error) {
        console.error('Failed to load from OASIS:', error);
        showNotification(`Error loading from OASIS:\n${error.message}`, 'error');
    }
}

// Create a new avatar
async function createAvatar() {
    try {
        const username = prompt('Enter username:');
        if (!username) return;
        
        const email = prompt('Enter email:');
        if (!email) return;
        
        const password = prompt('Enter password:');
        if (!password) return;
        
        const firstName = prompt('Enter first name (optional):') || '';
        const lastName = prompt('Enter last name (optional):') || '';
        
        showNotification('Creating avatar...', 'success');
        
        await oasisClient.authenticate();
        
        const avatar = await oasisClient.registerAvatar({
            username: username,
            email: email,
            password: password,
            firstName: firstName,
            lastName: lastName,
            avatarType: 'User'
        });
        
        if (avatar && (avatar.id || avatar.Id)) {
            const avatarId = avatar.id || avatar.Id;
            showNotification(`Avatar created!\nUsername: ${username}\nID: ${avatarId.substring(0, 8)}...`, 'success');
            
            // Store avatar ID and reload with this avatar's holons
            currentAvatarId = avatarId;
            
            // Switch to this avatar's credentials for future requests
            oasisClient.username = username;
            oasisClient.password = password;
            
            // Load holons for this avatar
            setTimeout(() => {
                loadFromOASIS(avatarId);
            }, 1000);
        } else {
            showNotification('Avatar creation failed or returned invalid data', 'error');
        }
    } catch (error) {
        console.error('Failed to create avatar:', error);
        showNotification(`Error creating avatar:\n${error.message}`, 'error');
    }
}

// Load holons for current authenticated avatar
async function loadCurrentAvatarHolons() {
    try {
        showNotification('Getting current avatar...', 'success');
        await oasisClient.authenticate();
        
        const avatar = await oasisClient.getCurrentAvatar();
        if (avatar && (avatar.id || avatar.Id)) {
            const avatarId = avatar.id || avatar.Id;
            showNotification(`Loading holons for: ${avatar.username || avatar.Username || avatarId.substring(0, 8)}...`, 'success');
            await loadFromOASIS(avatarId);
        } else {
            showNotification('No avatar currently logged in', 'error');
        }
    } catch (error) {
        console.error('Failed to load current avatar holons:', error);
        showNotification(`Error: ${error.message}`, 'error');
    }
}

// Seed OASIS data function
async function seedOASISData() {
    try {
        showNotification('Seeding OASIS with sample data...', 'success');
        
        // Dynamically import the seed function
        const { seedOASISData } = await import('./utils/seedOASISData.js');
        const result = await seedOASISData();
        
        if (result.success) {
            showNotification(
                `Seeding complete!\n${result.oappsCreated} OAPPs created\n${result.unassignedHolons} unassigned holons`,
                'success'
            );
            // Auto-load the data after seeding
            setTimeout(() => {
                loadFromOASIS();
            }, 2000);
        } else {
            showNotification(`Seeding failed: ${result.error}`, 'error');
        }
    } catch (error) {
        console.error('Failed to seed OASIS data:', error);
        showNotification(`Error seeding: ${error.message}`, 'error');
    }
}

// Add event listeners (wait for DOM to be ready)
function setupEventListeners() {
    const loadBtn = document.getElementById('btn-load-oasis');
    if (loadBtn) {
        loadBtn.addEventListener('click', () => loadFromOASIS());
    }
    
    const seedBtn = document.getElementById('btn-seed-oasis');
    if (seedBtn) {
        seedBtn.addEventListener('click', seedOASISData);
    }
    
    const createAvatarBtn = document.getElementById('btn-create-avatar');
    if (createAvatarBtn) {
        createAvatarBtn.addEventListener('click', createAvatar);
    }
    
    const loadMyHolonsBtn = document.getElementById('btn-load-my-holons');
    if (loadMyHolonsBtn) {
        loadMyHolonsBtn.addEventListener('click', loadCurrentAvatarHolons);
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', setupEventListeners);
} else {
    setupEventListeners();
}

// Update stats
function updateStats(data) {
    const oapps = data.oapps || [];
    const holons = data.holons || [];
    
    let stars = 0, planets = 0, moons = 0;
    oapps.forEach(oapp => {
        if (oapp.celestialType === 'star') stars++;
        else if (oapp.celestialType === 'planet') planets++;
        else if (oapp.celestialType === 'moon') moons++;
    });
    
    document.getElementById('stat-oapps').textContent = oapps.length;
    document.getElementById('stat-holons').textContent = holons.length;
    document.getElementById('stat-stars').textContent = stars;
    document.getElementById('stat-planets').textContent = planets;
    document.getElementById('stat-moons').textContent = moons;
}

// Hide loading screen when ready
visualizer.onReady(() => {
    const loading = document.getElementById('loading');
    if (loading) {
        loading.style.display = 'none';
    }
    console.log('✅ Visualizer ready!');
    
    // Don't auto-generate data - let user choose
});

// Start the visualizer
try {
    visualizer.init();
    console.log('✅ Visualizer initialized');
} catch (error) {
    console.error('❌ Error initializing visualizer:', error);
    showNotification(`Error initializing: ${error.message}`, 'error');
}
