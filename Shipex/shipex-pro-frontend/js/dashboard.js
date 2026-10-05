/**
 * Dashboard Component
 * Main merchant dashboard with stats and shipments
 */

class DashboardComponent {
    constructor() {
        this.shipments = [];
        this.filters = {
            status: 'all',
            search: ''
        };
    }

    async init() {
        await this.loadData();
        this.render();
        this.attachEvents();
    }

    async loadData() {
        const merchant = shipexAPI.getMerchant();
        if (!merchant) {
            router.navigate('auth');
            return;
        }

        showLoading('Loading dashboard...');

        try {
            const merchantId = merchant.merchantId || merchant.MerchantId;
            const token = shipexAPI.getAuthToken();
            const isDemoToken = token && token.startsWith('demo-token-for-testing');

            // In demo mode, use mock data instead of API call
            if (isDemoToken) {
                // Mock shipments for testing
                this.shipments = [
                    {
                        shipmentId: '00000000-0000-0000-0000-000000000001',
                        trackingNumber: '1Z999AA10123456784',
                        status: 'InTransit',
                        carrier: 'UPS',
                        origin: 'New York, NY',
                        destination: 'Los Angeles, CA',
                        createdAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
                        amountCharged: 45.99
                    },
                    {
                        shipmentId: '00000000-0000-0000-0000-000000000002',
                        trackingNumber: '9400111899223197428490',
                        status: 'Delivered',
                        carrier: 'USPS',
                        origin: 'Chicago, IL',
                        destination: 'Miami, FL',
                        createdAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
                        amountCharged: 28.50
                    }
                ];
                hideLoading();
                return;
            }

            // Load shipments from API
            const shipmentsResult = await shipexAPI.getShipments(merchantId, {
                limit: 50,
                offset: 0
            });

            this.shipments = shipmentsResult.result || shipmentsResult || [];
            hideLoading();
        } catch (error) {
            hideLoading();
            console.error('Failed to load dashboard data:', error);
            showToast('Failed to load dashboard data', 'error');
        }
    }

    render() {
        const dashboardScreen = document.getElementById('dashboardScreen');
        const merchant = shipexAPI.getMerchant();

        if (!merchant) {
            dashboardScreen.innerHTML = '<p>Please log in to view dashboard</p>';
            return;
        }

        // Calculate stats
        const stats = this.calculateStats();

        dashboardScreen.innerHTML = `
            <div class="dashboard-header">
                <div>
                    <h1>Dashboard</h1>
                    <p class="dashboard-subtitle">Welcome back, ${merchant.companyName || merchant.CompanyName || 'Merchant'}</p>
                </div>
                <a href="#quote" class="btn-primary">New Shipment</a>
            </div>

            <!-- Testing Mode Indicator -->
            ${shipexAPI.getAuthToken()?.startsWith('demo-token-for-testing') ? `
                <div style="background: rgba(59, 130, 246, 0.1); border: 1px solid var(--status-in-transit); border-radius: var(--radius-sm); padding: 12px; margin-bottom: 24px; display: flex; align-items: center; gap: 8px;">
                    <span style="font-size: 14px;">⚡</span>
                    <span style="font-size: 14px; color: var(--status-in-transit);">Testing Mode Active - Using demo data</span>
                </div>
            ` : ''}

            <!-- Stats Cards -->
            <div class="stats-grid">
                <div class="stat-card">
                    <div class="stat-value">${stats.total}</div>
                    <div class="stat-label">Total Shipments</div>
                </div>
                <div class="stat-card">
                    <div class="stat-value">${stats.active}</div>
                    <div class="stat-label">Active Shipments</div>
                </div>
                <div class="stat-card">
                    <div class="stat-value">${stats.completed}</div>
                    <div class="stat-label">Completed</div>
                </div>
                <div class="stat-card">
                    <div class="stat-value">${formatCurrency(stats.revenue)}</div>
                    <div class="stat-label">Total Revenue</div>
                </div>
            </div>

            <!-- Shipments Section -->
            <div class="card">
                <div class="card-header">
                    <h2 class="card-title">Recent Shipments</h2>
                    <div class="shipments-filters">
                        <input type="text" class="form-input" id="searchInput" placeholder="Search by tracking number..." 
                               style="width: 250px; margin-right: 16px;">
                        <select class="form-select" id="statusFilter" style="width: 150px;">
                            <option value="all">All Status</option>
                            <option value="QuoteRequested">Quote Requested</option>
                            <option value="InTransit">In Transit</option>
                            <option value="Delivered">Delivered</option>
                            <option value="Error">Error</option>
                        </select>
                    </div>
                </div>

                <div class="table-container">
                    <table class="table">
                        <thead>
                            <tr>
                                <th>Tracking #</th>
                                <th>Status</th>
                                <th>Carrier</th>
                                <th>Origin → Destination</th>
                                <th>Date</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody id="shipmentsTableBody">
                            ${this.renderShipmentsTable()}
                        </tbody>
                    </table>
                </div>

                ${this.shipments.length === 0 ? `
                    <div style="text-align: center; padding: 48px; color: var(--text-secondary);">
                        <p>No shipments yet. Create your first shipment to get started.</p>
                        <a href="#quote" class="btn-primary" style="margin-top: 16px;">Request Quote</a>
                    </div>
                ` : ''}
            </div>
        `;
    }

    renderShipmentsTable() {
        if (this.shipments.length === 0) {
            return '<tr><td colspan="6" style="text-align: center; padding: 32px; color: var(--text-secondary);">No shipments found</td></tr>';
        }

        const filtered = this.getFilteredShipments();

        return filtered.map(shipment => {
            const status = shipment.status || shipment.Status || 'Unknown';
            const trackingNumber = shipment.trackingNumber || shipment.TrackingNumber || 'N/A';
            const createdAt = formatDate(shipment.createdAt || shipment.CreatedAt);
            const statusClass = getStatusBadgeClass(status);

            return `
                <tr>
                    <td>
                        <code style="font-size: 12px;">${formatTrackingNumber(trackingNumber)}</code>
                    </td>
                    <td>
                        <span class="status-badge ${statusClass}">${status}</span>
                    </td>
                    <td>${shipment.carrier || shipment.Carrier || 'N/A'}</td>
                    <td style="font-size: 12px; color: var(--text-secondary);">
                        ${shipment.origin || 'N/A'} → ${shipment.destination || 'N/A'}
                    </td>
                    <td>${createdAt}</td>
                    <td>
                        <button class="btn-text" onclick="router.navigate('tracking/${trackingNumber}')">
                            Track
                        </button>
                    </td>
                </tr>
            `;
        }).join('');
    }

    calculateStats() {
        const total = this.shipments.length;
        const active = this.shipments.filter(s => {
            const status = (s.status || s.Status || '').toLowerCase();
            return status.includes('transit') || status.includes('quote');
        }).length;
        const completed = this.shipments.filter(s => {
            const status = (s.status || s.Status || '').toLowerCase();
            return status.includes('delivered');
        }).length;
        const revenue = this.shipments.reduce((sum, s) => {
            return sum + (parseFloat(s.amountCharged || s.AmountCharged || 0));
        }, 0);

        return { total, active, completed, revenue };
    }

    getFilteredShipments() {
        let filtered = [...this.shipments];

        // Filter by status
        if (this.filters.status !== 'all') {
            filtered = filtered.filter(s => {
                const status = (s.status || s.Status || '').toLowerCase();
                return status.includes(this.filters.status.toLowerCase());
            });
        }

        // Filter by search
        if (this.filters.search) {
            const searchLower = this.filters.search.toLowerCase();
            filtered = filtered.filter(s => {
                const tracking = (s.trackingNumber || s.TrackingNumber || '').toLowerCase();
                return tracking.includes(searchLower);
            });
        }

        return filtered;
    }

    attachEvents() {
        // Search input
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('input', debounce((e) => {
                this.filters.search = e.target.value;
                this.updateTable();
            }, 300));
        }

        // Status filter
        const statusFilter = document.getElementById('statusFilter');
        if (statusFilter) {
            statusFilter.addEventListener('change', (e) => {
                this.filters.status = e.target.value;
                this.updateTable();
            });
        }
    }

    updateTable() {
        const tbody = document.getElementById('shipmentsTableBody');
        if (tbody) {
            tbody.innerHTML = this.renderShipmentsTable();
        }
    }
}

// Initialize dashboard component
let dashboardComponent = null;
