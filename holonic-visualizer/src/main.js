import { HolonicVisualizer } from './visualizer/HolonicVisualizer.js';
import { MockDataGenerator } from './data/MockDataGenerator.js';

// Initialize the visualizer
const container = document.getElementById('canvas-container');
const visualizer = new HolonicVisualizer(container);

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
