/**
 * Quote Request Component
 */

class QuoteComponent {
    constructor() {
        this.quotes = [];
        this.selectedQuote = null;
    }

    async init() {
        this.render();
        this.attachEvents();
    }

    render() {
        const quoteScreen = document.getElementById('quoteScreen');
        quoteScreen.innerHTML = `
            <div class="quote-container">
                <div class="quote-header">
                    <h1>Request Shipping Quote</h1>
                    <p class="quote-subtitle">Get rates from multiple carriers</p>
                </div>

                <form id="quoteForm" class="quote-form">
                    <div class="form-row">
                        <!-- Package Details -->
                        <div class="form-section">
                            <h3 class="section-title">Package Details</h3>
                            
                            <div class="form-group">
                                <label class="form-label">Dimensions</label>
                                <div class="dimensions-inputs">
                                    <input type="number" class="form-input" id="length" placeholder="Length" step="0.01" min="0" required>
                                    <span>×</span>
                                    <input type="number" class="form-input" id="width" placeholder="Width" step="0.01" min="0" required>
                                    <span>×</span>
                                    <input type="number" class="form-input" id="height" placeholder="Height" step="0.01" min="0" required>
                                </div>
                                <select class="form-select" id="dimensionUnit" style="margin-top: 8px;">
                                    <option value="inches">Inches</option>
                                    <option value="cm">Centimeters</option>
                                </select>
                            </div>

                            <div class="form-group">
                                <label class="form-label">Weight</label>
                                <div class="weight-input">
                                    <input type="number" class="form-input" id="weight" placeholder="Weight" step="0.01" min="0" required>
                                    <select class="form-select" id="weightUnit" style="width: 100px;">
                                        <option value="lbs">lbs</option>
                                        <option value="kg">kg</option>
                                    </select>
                                </div>
                            </div>

                            <div class="form-group">
                                <label class="form-label">Service Level</label>
                                <select class="form-select" id="serviceLevel" required>
                                    <option value="standard">Standard</option>
                                    <option value="express">Express</option>
                                    <option value="overnight">Overnight</option>
                                </select>
                            </div>
                        </div>

                        <!-- Addresses -->
                        <div class="form-section">
                            <h3 class="section-title">Addresses</h3>
                            
                            <div class="form-group">
                                <label class="form-label">Origin Address</label>
                                <input type="text" class="form-input" id="originStreet" placeholder="Street" required>
                                <div class="form-row" style="margin-top: 8px;">
                                    <input type="text" class="form-input" id="originCity" placeholder="City" required>
                                    <input type="text" class="form-input" id="originState" placeholder="State" required>
                                </div>
                                <div class="form-row" style="margin-top: 8px;">
                                    <input type="text" class="form-input" id="originPostalCode" placeholder="Postal Code" required>
                                    <input type="text" class="form-input" id="originCountry" placeholder="Country" value="US" required>
                                </div>
                            </div>

                            <div class="form-group">
                                <label class="form-label">Destination Address</label>
                                <input type="text" class="form-input" id="destStreet" placeholder="Street" required>
                                <div class="form-row" style="margin-top: 8px;">
                                    <input type="text" class="form-input" id="destCity" placeholder="City" required>
                                    <input type="text" class="form-input" id="destState" placeholder="State" required>
                                </div>
                                <div class="form-row" style="margin-top: 8px;">
                                    <input type="text" class="form-input" id="destPostalCode" placeholder="Postal Code" required>
                                    <input type="text" class="form-input" id="destCountry" placeholder="Country" value="US" required>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div id="quoteError" class="auth-error" style="display: none;"></div>

                    <button type="submit" class="btn-primary" id="quoteSubmit" style="width: 100%; margin-top: 24px;">
                        Get Quotes
                    </button>
                </form>

                <!-- Quote Results -->
                <div id="quoteResults" style="display: none; margin-top: 32px;">
                    <h2 class="section-title">Available Quotes</h2>
                    <div id="quotesList" class="quotes-list"></div>
                </div>
            </div>
        `;
    }

    attachEvents() {
        const form = document.getElementById('quoteForm');
        if (form) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                this.handleSubmit();
            });
        }
    }

    async handleSubmit() {
        const errorDiv = document.getElementById('quoteError');
        const submitBtn = document.getElementById('quoteSubmit');
        
        errorDiv.style.display = 'none';
        submitBtn.disabled = true;
        submitBtn.textContent = 'Requesting quotes...';

        try {
            const merchant = shipexAPI.getMerchant();
            if (!merchant) {
                throw new Error('Please log in');
            }

            const rateRequest = {
                merchantId: merchant.merchantId || merchant.MerchantId,
                dimensions: {
                    length: parseFloat(document.getElementById('length').value),
                    width: parseFloat(document.getElementById('width').value),
                    height: parseFloat(document.getElementById('height').value),
                    unit: document.getElementById('dimensionUnit').value
                },
                weight: parseFloat(document.getElementById('weight').value),
                origin: {
                    street: document.getElementById('originStreet').value,
                    city: document.getElementById('originCity').value,
                    state: document.getElementById('originState').value,
                    postalCode: document.getElementById('originPostalCode').value,
                    country: document.getElementById('originCountry').value
                },
                destination: {
                    street: document.getElementById('destStreet').value,
                    city: document.getElementById('destCity').value,
                    state: document.getElementById('destState').value,
                    postalCode: document.getElementById('destPostalCode').value,
                    country: document.getElementById('destCountry').value
                },
                serviceLevel: document.getElementById('serviceLevel').value
            };

            showLoading('Fetching quotes...');

            const token = shipexAPI.getAuthToken();
            const isDemoToken = token && token.startsWith('demo-token-for-testing');

            // In demo mode, use mock quotes
            if (isDemoToken) {
                setTimeout(() => {
                    this.quotes = [
                        {
                            carrier: 'UPS',
                            serviceName: 'Ground',
                            carrierRate: 25.00,
                            clientPrice: 30.00,
                            markupAmount: 5.00,
                            estimatedDays: 5
                        },
                        {
                            carrier: 'FedEx',
                            serviceName: 'Home Delivery',
                            carrierRate: 28.00,
                            clientPrice: 33.60,
                            markupAmount: 5.60,
                            estimatedDays: 4
                        },
                        {
                            carrier: 'USPS',
                            serviceName: 'Priority Mail',
                            carrierRate: 22.00,
                            clientPrice: 26.40,
                            markupAmount: 4.40,
                            estimatedDays: 6
                        }
                    ];
                    this.quoteData = {
                        quoteId: 'demo-quote-' + Date.now(),
                        quotes: this.quotes,
                        expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
                    };
                    hideLoading();
                    this.displayQuotes();
                }, 1000); // Simulate API delay
                return;
            }

            const result = await shipexAPI.requestQuote(rateRequest);
            
            hideLoading();
            this.quotes = result.result?.quotes || result.quotes || [];
            this.quoteData = result.result || result; // Store full quote response
            this.displayQuotes();

            submitBtn.disabled = false;
            submitBtn.textContent = 'Get Quotes';
        } catch (error) {
            hideLoading();
            errorDiv.textContent = error.message || 'Failed to get quotes';
            errorDiv.style.display = 'block';
            submitBtn.disabled = false;
            submitBtn.textContent = 'Get Quotes';
        }
    }

    displayQuotes() {
        const resultsDiv = document.getElementById('quoteResults');
        const quotesList = document.getElementById('quotesList');

        if (this.quotes.length === 0) {
            quotesList.innerHTML = '<p style="text-align: center; color: var(--text-secondary); padding: 32px;">No quotes available</p>';
            resultsDiv.style.display = 'block';
            return;
        }

        quotesList.innerHTML = this.quotes.map((quote, index) => `
            <div class="quote-card" data-index="${index}">
                <div class="quote-card-header">
                    <div>
                        <h4>${quote.carrier || quote.Carrier || 'Carrier'}</h4>
                        <p class="quote-service">${quote.serviceName || quote.ServiceName || 'Standard Service'}</p>
                    </div>
                    <div class="quote-price">
                        ${formatCurrency(quote.clientPrice || quote.ClientPrice || 0)}
                    </div>
                </div>
                <div class="quote-card-body">
                    <div class="quote-details">
                        <div class="quote-detail">
                            <span class="quote-detail-label">Carrier Rate:</span>
                            <span>${formatCurrency(quote.carrierRate || quote.CarrierRate || 0)}</span>
                        </div>
                        <div class="quote-detail">
                            <span class="quote-detail-label">Markup:</span>
                            <span>${formatCurrency(quote.markupAmount || quote.MarkupAmount || 0)}</span>
                        </div>
                        <div class="quote-detail">
                            <span class="quote-detail-label">Est. Delivery:</span>
                            <span>${quote.estimatedDays || quote.EstimatedDays || 'N/A'} days</span>
                        </div>
                    </div>
                    <button class="btn-primary" onclick="quoteComponent.selectQuote(${index})" style="width: 100%; margin-top: 16px;">
                        Select Quote
                    </button>
                </div>
            </div>
        `).join('');

        resultsDiv.style.display = 'block';
    }

    selectQuote(index) {
        this.selectedQuote = this.quotes[index];
        
        // Store selected quote and quote data in session
        sessionStorage.setItem('selectedQuote', JSON.stringify(this.selectedQuote));
        if (this.quoteData) {
            sessionStorage.setItem('quoteData', JSON.stringify(this.quoteData));
        }
        
        showToast('Quote selected', 'success');
        // Navigate to confirmation screen
        router.navigate('confirm');
    }
}

let quoteComponent = null;
