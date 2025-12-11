/**
 * Shipment Confirmation Component
 * Confirms and creates shipment after quote selection
 */

class ConfirmComponent {
    constructor() {
        this.selectedQuote = null;
        this.quoteData = null;
    }

    async init() {
        // Get selected quote from session/localStorage
        const storedQuote = sessionStorage.getItem('selectedQuote');
        if (storedQuote) {
            this.selectedQuote = JSON.parse(storedQuote);
        }

        // Get quote data if available
        const storedQuoteData = sessionStorage.getItem('quoteData');
        if (storedQuoteData) {
            this.quoteData = JSON.parse(storedQuoteData);
        }

        this.render();
        this.attachEvents();
    }

    render() {
        const confirmScreen = document.getElementById('confirmScreen');
        
        if (!this.selectedQuote) {
            confirmScreen.innerHTML = `
                <div class="confirm-container">
                    <div class="card">
                        <h1>No Quote Selected</h1>
                        <p style="color: var(--text-secondary); margin: 16px 0;">Please select a quote first.</p>
                        <a href="#quote" class="btn-primary">Request Quote</a>
                    </div>
                </div>
            `;
            return;
        }

        confirmScreen.innerHTML = `
            <div class="confirm-container">
                <div class="confirm-header">
                    <h1>Confirm Shipment</h1>
                    <p class="confirm-subtitle">Review and confirm your shipment details</p>
                </div>

                <div class="confirm-content">
                    <!-- Selected Quote Summary -->
                    <div class="card" style="margin-bottom: 24px;">
                        <h2 class="card-title">Selected Quote</h2>
                        <div class="quote-summary">
                            <div class="quote-summary-item">
                                <span class="quote-summary-label">Carrier:</span>
                                <span class="quote-summary-value">${this.selectedQuote.carrier || this.selectedQuote.Carrier || 'N/A'}</span>
                            </div>
                            <div class="quote-summary-item">
                                <span class="quote-summary-label">Service:</span>
                                <span class="quote-summary-value">${this.selectedQuote.serviceName || this.selectedQuote.ServiceName || 'Standard'}</span>
                            </div>
                            <div class="quote-summary-item">
                                <span class="quote-summary-label">Price:</span>
                                <span class="quote-summary-value">${formatCurrency(this.selectedQuote.clientPrice || this.selectedQuote.ClientPrice || 0)}</span>
                            </div>
                            <div class="quote-summary-item">
                                <span class="quote-summary-label">Est. Delivery:</span>
                                <span class="quote-summary-value">${this.selectedQuote.estimatedDays || this.selectedQuote.EstimatedDays || 'N/A'} days</span>
                            </div>
                        </div>
                    </div>

                    <!-- Customer Information Form -->
                    <div class="card">
                        <h2 class="card-title">Customer Information</h2>
                        <form id="confirmForm" class="confirm-form">
                            <div class="form-group">
                                <label class="form-label">Customer Name *</label>
                                <input type="text" class="form-input" id="customerName" required>
                            </div>
                            <div class="form-group">
                                <label class="form-label">Email *</label>
                                <input type="email" class="form-input" id="customerEmail" required>
                            </div>
                            <div class="form-group">
                                <label class="form-label">Phone</label>
                                <input type="tel" class="form-input" id="customerPhone">
                            </div>
                            <div class="form-group">
                                <label class="form-label">Delivery Instructions</label>
                                <textarea class="form-textarea" id="deliveryInstructions" rows="3" 
                                          placeholder="Optional delivery instructions..."></textarea>
                            </div>

                            <div id="confirmError" class="auth-error" style="display: none;"></div>

                            <div class="confirm-actions">
                                <button type="button" class="btn-secondary" onclick="router.navigate('quote')">
                                    Back to Quotes
                                </button>
                                <button type="submit" class="btn-primary" id="confirmSubmit">
                                    Confirm & Create Shipment
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        `;
    }

    attachEvents() {
        const form = document.getElementById('confirmForm');
        if (form) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                this.handleConfirm();
            });
        }
    }

    async handleConfirm() {
        const errorDiv = document.getElementById('confirmError');
        const submitBtn = document.getElementById('confirmSubmit');
        
        errorDiv.style.display = 'none';
        submitBtn.disabled = true;
        submitBtn.textContent = 'Creating shipment...';

        try {
            const merchant = shipexAPI.getMerchant();
            if (!merchant) {
                throw new Error('Please log in');
            }

            // Get quote ID from stored data
            const quoteId = this.quoteData?.quoteId || this.quoteData?.QuoteId || this.generateGuid();

            const orderRequest = {
                quoteId: quoteId,
                selectedCarrier: this.selectedQuote.carrier || this.selectedQuote.Carrier,
                customerInfo: {
                    name: document.getElementById('customerName').value.trim(),
                    email: document.getElementById('customerEmail').value.trim(),
                    phone: document.getElementById('customerPhone').value.trim() || null,
                    deliveryInstructions: document.getElementById('deliveryInstructions').value.trim() || null
                }
            };

            showLoading('Creating shipment...');

            const token = shipexAPI.getAuthToken();
            const isDemoToken = token && token.startsWith('demo-token-for-testing');

            // In demo mode, use mock shipment creation
            if (isDemoToken) {
                setTimeout(() => {
                    const mockShipment = {
                        shipmentId: 'demo-shipment-' + Date.now(),
                        trackingNumber: 'DEMO' + Math.random().toString(36).substring(2, 11).toUpperCase(),
                        status: 'LabelGenerated',
                        label: {
                            pdfUrl: null,
                            pdfBase64: null
                        }
                    };
                    hideLoading();
                    this.showSuccess(mockShipment);
                }, 1000); // Simulate API delay
                return;
            }

            const result = await shipexAPI.confirmShipment(orderRequest);
            
            hideLoading();

            // Show success screen
            this.showSuccess(result.result || result);
        } catch (error) {
            hideLoading();
            errorDiv.textContent = error.message || 'Failed to create shipment';
            errorDiv.style.display = 'block';
            submitBtn.disabled = false;
            submitBtn.textContent = 'Confirm & Create Shipment';
        }
    }

    showSuccess(shipmentData) {
        const confirmScreen = document.getElementById('confirmScreen');
        const trackingNumber = shipmentData.trackingNumber || shipmentData.TrackingNumber || 'N/A';
        const shipmentId = shipmentData.shipmentId || shipmentData.ShipmentId;

        confirmScreen.innerHTML = `
            <div class="confirm-container">
                <div class="success-card">
                    <div class="success-icon">✓</div>
                    <h1>Shipment Created Successfully!</h1>
                    <p class="success-message">Your shipment has been created and is ready to ship.</p>

                    <div class="success-details">
                        <div class="success-detail-item">
                            <span class="success-detail-label">Tracking Number:</span>
                            <code class="tracking-number-display" style="font-size: 20px; margin-top: 8px;">
                                ${formatTrackingNumber(trackingNumber)}
                            </code>
                            <button class="btn-text" onclick="copyToClipboard('${trackingNumber}')" style="margin-top: 8px;">
                                Copy Tracking Number
                            </button>
                        </div>
                    </div>

                    <div class="success-actions">
                        <button class="btn-primary" onclick="router.navigate('tracking/${trackingNumber}')">
                            Track Shipment
                        </button>
                        <button class="btn-secondary" onclick="router.navigate('dashboard')">
                            View Dashboard
                        </button>
                        <button class="btn-secondary" onclick="router.navigate('quote'); sessionStorage.removeItem('selectedQuote'); sessionStorage.removeItem('quoteData');">
                            Create Another
                        </button>
                    </div>
                </div>
            </div>
        `;

        // Clear stored quote data
        sessionStorage.removeItem('selectedQuote');
        sessionStorage.removeItem('quoteData');
    }

    generateGuid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }
}

let confirmComponent = null;
