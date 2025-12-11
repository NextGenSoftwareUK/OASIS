/**
 * Tracking Component
 * Timeline visualization for shipment tracking
 */

class TrackingComponent {
    constructor() {
        this.trackingData = null;
    }

    async init(trackingNumber = null) {
        this.trackingNumber = trackingNumber || this.getTrackingNumberFromHash();
        this.render();
        
        if (this.trackingNumber) {
            await this.loadTracking();
        }
        
        this.attachEvents();
    }

    getTrackingNumberFromHash() {
        const hash = window.location.hash;
        const match = hash.match(/tracking\/(.+)/);
        return match ? decodeURIComponent(match[1]) : null;
    }

    render() {
        const trackingScreen = document.getElementById('trackingScreen');
        trackingScreen.innerHTML = `
            <div class="tracking-container">
                <div class="tracking-header">
                    <h1>Track Shipment</h1>
                    <div class="tracking-search">
                        <input type="text" class="form-input" id="trackingInput" 
                               placeholder="Enter tracking number" 
                               value="${this.trackingNumber || ''}">
                        <button class="btn-primary" id="trackButton">Track</button>
                    </div>
                </div>

                <div id="trackingResults" style="display: none;">
                    ${this.renderTrackingDetails()}
                </div>

                <div id="trackingError" class="auth-error" style="display: none;"></div>
            </div>
        `;
    }

    renderTrackingDetails() {
        if (!this.trackingData) {
            return '';
        }

        const data = this.trackingData.result || this.trackingData;
        const status = data.status || data.Status || 'Unknown';
        const trackingNumber = data.trackingNumber || data.TrackingNumber || this.trackingNumber;
        const history = data.history || data.History || [];

        return `
            <div class="tracking-card">
                <div class="tracking-status-card">
                    <div class="tracking-status-header">
                        <h2>Tracking Number</h2>
                        <code class="tracking-number-display">${formatTrackingNumber(trackingNumber)}</code>
                        <button class="btn-text" onclick="copyToClipboard('${trackingNumber}')" style="margin-top: 8px;">
                            Copy
                        </button>
                    </div>
                    <div class="tracking-current-status">
                        <span class="status-badge ${getStatusBadgeClass(status)}" style="font-size: 14px; padding: 8px 16px;">
                            ${status}
                        </span>
                    </div>
                </div>

                <div class="tracking-timeline-section">
                    <h3 class="section-title">Tracking History</h3>
                    <div class="timeline">
                        ${this.renderTimeline(history)}
                    </div>
                </div>
            </div>
        `;
    }

    renderTimeline(history) {
        if (!history || history.length === 0) {
            return '<p style="text-align: center; color: var(--text-secondary); padding: 32px;">No tracking history available</p>';
        }

        // Sort by timestamp (newest first)
        const sorted = [...history].sort((a, b) => {
            const timeA = new Date(a.timestamp || a.Timestamp || 0);
            const timeB = new Date(b.timestamp || b.Timestamp || 0);
            return timeB - timeA;
        });

        return sorted.map((item, index) => {
            const isActive = index === 0;
            const isCompleted = index > 0;
            const status = item.status || item.Status || 'Unknown';
            const timestamp = formatDate(item.timestamp || item.Timestamp);
            const location = item.location || item.Location || 'N/A';
            const description = item.description || item.Description || `Status: ${status}`;

            return `
                <div class="timeline-item ${isActive ? 'active' : isCompleted ? 'completed' : ''}">
                    <div class="timeline-content">
                        <div class="timeline-status">${status}</div>
                        <div class="timeline-time">${timestamp}</div>
                        <div class="timeline-location">${location}</div>
                        <div class="timeline-description" style="margin-top: 8px; color: var(--text-secondary); font-size: 12px;">
                            ${description}
                        </div>
                    </div>
                </div>
            `;
        }).join('');
    }

    attachEvents() {
        const trackButton = document.getElementById('trackButton');
        const trackingInput = document.getElementById('trackingInput');

        if (trackButton) {
            trackButton.addEventListener('click', () => this.handleTrack());
        }

        if (trackingInput) {
            trackingInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    this.handleTrack();
                }
            });
        }
    }

    async handleTrack() {
        const trackingInput = document.getElementById('trackingInput');
        const trackingNumber = trackingInput.value.trim();
        const errorDiv = document.getElementById('trackingError');
        const resultsDiv = document.getElementById('trackingResults');

        if (!trackingNumber) {
            errorDiv.textContent = 'Please enter a tracking number';
            errorDiv.style.display = 'block';
            return;
        }

        errorDiv.style.display = 'none';
        showLoading('Tracking shipment...');

        try {
            const token = shipexAPI.getAuthToken();
            const isDemoToken = token && token.startsWith('demo-token-for-testing');

            // In demo mode, use mock tracking data
            if (isDemoToken) {
                setTimeout(() => {
                    this.trackingData = {
                        result: {
                            trackingNumber: trackingNumber,
                            status: 'InTransit',
                            currentLocation: 'Chicago, IL',
                            estimatedDelivery: new Date(Date.now() + 2 * 24 * 60 * 60 * 1000).toISOString(),
                            history: [
                                {
                                    status: 'Delivered',
                                    timestamp: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000).toISOString(),
                                    location: 'Los Angeles, CA',
                                    description: 'Package delivered to recipient'
                                },
                                {
                                    status: 'InTransit',
                                    timestamp: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
                                    location: 'Chicago, IL',
                                    description: 'Package in transit'
                                },
                                {
                                    status: 'ShipmentCreated',
                                    timestamp: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(),
                                    location: 'New York, NY',
                                    description: 'Shipment created and label generated'
                                }
                            ]
                        }
                    };
                    hideLoading();
                    resultsDiv.style.display = 'block';
                    resultsDiv.innerHTML = this.renderTrackingDetails();
                    router.navigate(`tracking/${trackingNumber}`);
                }, 800); // Simulate API delay
                return;
            }

            const result = await shipexAPI.trackShipment(trackingNumber);
            this.trackingData = result;
            
            hideLoading();
            resultsDiv.style.display = 'block';
            resultsDiv.innerHTML = this.renderTrackingDetails();
            
            // Update URL hash
            router.navigate(`tracking/${trackingNumber}`);
        } catch (error) {
            hideLoading();
            errorDiv.textContent = error.message || 'Failed to track shipment';
            errorDiv.style.display = 'block';
            resultsDiv.style.display = 'none';
        }
    }

    async loadTracking() {
        if (!this.trackingNumber) return;

        showLoading('Loading tracking information...');

        try {
            const result = await shipexAPI.trackShipment(this.trackingNumber);
            this.trackingData = result;
            
            hideLoading();
            const resultsDiv = document.getElementById('trackingResults');
            if (resultsDiv) {
                resultsDiv.style.display = 'block';
                resultsDiv.innerHTML = this.renderTrackingDetails();
            }
        } catch (error) {
            hideLoading();
            const errorDiv = document.getElementById('trackingError');
            if (errorDiv) {
                errorDiv.textContent = error.message || 'Failed to load tracking information';
                errorDiv.style.display = 'block';
            }
        }
    }
}

let trackingComponent = null;
