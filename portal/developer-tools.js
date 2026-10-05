/**
 * Developer Tools Module
 * Provides API key management, usage stats, and developer resources
 */

function loadDeveloperTools() {
    const container = document.getElementById('developer-tools-content');
    if (!container) return;

    container.innerHTML = `
        <div class="portal-section">
            <div class="portal-grid portal-grid-2">
                <!-- API Keys Section -->
                <div class="portal-card">
                    <div class="portal-card-header">
                        <div class="portal-card-title">API Keys</div>
                        <button class="btn-primary" onclick="generateAPIKey()">Generate New Key</button>
                    </div>
                    <div class="api-keys-list" id="apiKeysList">
                        <div class="empty-state">
                            <p class="empty-state-text">No API keys generated yet</p>
                            <p class="empty-state-subtext">Generate an API key to start using the OASIS API</p>
                        </div>
                    </div>
                </div>

                <!-- Usage Stats Section -->
                <div class="portal-card">
                    <div class="portal-card-header">
                        <div class="portal-card-title">Usage Statistics</div>
                    </div>
                    <div class="usage-stats" id="usageStats">
                        <div class="stat-item">
                            <div class="stat-label">API Calls (30 days)</div>
                            <div class="stat-value">0</div>
                        </div>
                        <div class="stat-item">
                            <div class="stat-label">Rate Limit</div>
                            <div class="stat-value">1000/hour</div>
                        </div>
                        <div class="stat-item">
                            <div class="stat-label">Remaining</div>
                            <div class="stat-value">1000</div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Documentation Section -->
            <div class="portal-card" style="margin-top: 2rem;">
                <div class="portal-card-header">
                    <div class="portal-card-title">Developer Resources</div>
                </div>
                <div class="developer-resources">
                    <a href="https://oasis-web4.gitbook.io/oasis-web4-docs/" target="_blank" class="resource-link">
                        <div class="resource-icon">📚</div>
                        <div>
                            <div class="resource-title">API Documentation</div>
                            <div class="resource-description">Complete API reference and guides</div>
                        </div>
                    </a>
                    <a href="https://github.com/NextGenSoftwareUK/OASIS" target="_blank" class="resource-link">
                        <div class="resource-icon">💻</div>
                        <div>
                            <div class="resource-title">GitHub Repository</div>
                            <div class="resource-description">Source code and examples</div>
                        </div>
                    </a>
                    <a href="https://oasis-web4.gitbook.io/oasis-web4-docs/core-architecture/oasis_interoperability_architecture" target="_blank" class="resource-link">
                        <div class="resource-icon">🏗️</div>
                        <div>
                            <div class="resource-title">Architecture Guide</div>
                            <div class="resource-description">Learn about OASIS architecture</div>
                        </div>
                    </a>
                </div>
            </div>
        </div>
    `;
}

function generateAPIKey() {
    // TODO: Implement API key generation
    alert('API key generation will be implemented soon. For now, please use the OASIS API directly.');
}

// Make function globally available
window.loadDeveloperTools = loadDeveloperTools;









