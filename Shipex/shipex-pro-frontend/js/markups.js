/**
 * Markup Management Component
 */

class MarkupsComponent {
    constructor() {
        this.markups = [];
        this.editingMarkup = null;
    }

    async init() {
        await this.loadMarkups();
        this.render();
        this.attachEvents();
    }

    async loadMarkups() {
        try {
            const merchant = shipexAPI.getMerchant();
            if (!merchant) return;

            const token = shipexAPI.getAuthToken();
            const isDemoToken = token && token.startsWith('demo-token-for-testing');

            // In demo mode, use mock markups
            if (isDemoToken) {
                this.markups = [
                    {
                        markupId: 'demo-markup-1',
                        name: 'Standard Markup',
                        markupType: 'Percentage',
                        value: 20,
                        isActive: true
                    },
                    {
                        markupId: 'demo-markup-2',
                        name: 'Express Premium',
                        markupType: 'Fixed',
                        value: 5.00,
                        isActive: true
                    }
                ];
                return;
            }

            showLoading('Loading markups...');
            const result = await shipexAPI.getMarkups(merchant.merchantId || merchant.MerchantId);
            hideLoading();

            this.markups = result.result || result || [];
        } catch (error) {
            hideLoading();
            console.error('Failed to load markups:', error);
            showToast('Failed to load markups', 'error');
        }
    }

    render() {
        const markupsScreen = document.getElementById('markupsScreen');
        markupsScreen.innerHTML = `
            <div class="markups-container">
                <div class="markups-header">
                    <div>
                        <h1>Markup Management</h1>
                        <p class="markups-subtitle">Configure pricing markups for your shipments</p>
                    </div>
                    <button class="btn-primary" id="createMarkupBtn">Create Markup</button>
                </div>

                <div class="card">
                    <div class="table-container">
                        <table class="table">
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    <th>Type</th>
                                    <th>Value</th>
                                    <th>Status</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody id="markupsTableBody">
                                ${this.renderMarkupsTable()}
                            </tbody>
                        </table>
                    </div>

                    ${this.markups.length === 0 ? `
                        <div style="text-align: center; padding: 48px; color: var(--text-secondary);">
                            <p>No markups configured. Create your first markup to customize pricing.</p>
                        </div>
                    ` : ''}
                </div>
            </div>

            <!-- Markup Modal -->
            <div id="markupModal" class="modal" style="display: none;">
                <div class="modal-content">
                    <div class="modal-header">
                        <h2 id="modalTitle">Create Markup</h2>
                        <button class="modal-close" onclick="this.closest('.modal').style.display='none'">×</button>
                    </div>
                    <form id="markupForm" class="modal-body">
                        <div class="form-group">
                            <label class="form-label">Markup Name</label>
                            <input type="text" class="form-input" id="markupName" required>
                        </div>
                        <div class="form-group">
                            <label class="form-label">Type</label>
                            <select class="form-select" id="markupType" required>
                                <option value="Percentage">Percentage</option>
                                <option value="Fixed">Fixed Amount</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label class="form-label">Value</label>
                            <input type="number" class="form-input" id="markupValue" step="0.01" min="0" required>
                        </div>
                        <div class="form-group">
                            <label class="form-label">Carriers (comma-separated, leave empty for all)</label>
                            <input type="text" class="form-input" id="markupCarriers" 
                                   placeholder="UPS, FedEx, USPS">
                        </div>
                        <div id="markupError" class="auth-error" style="display: none;"></div>
                        <div class="modal-actions">
                            <button type="button" class="btn-secondary" onclick="document.getElementById('markupModal').style.display='none'">
                                Cancel
                            </button>
                            <button type="submit" class="btn-primary" id="markupSubmit">
                                Save Markup
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        `;
    }

    renderMarkupsTable() {
        if (this.markups.length === 0) {
            return '<tr><td colspan="5" style="text-align: center; padding: 32px; color: var(--text-secondary);">No markups found</td></tr>';
        }

        return this.markups.map(markup => {
            const markupId = markup.markupId || markup.MarkupId;
            const name = markup.name || markup.Name || 'Unnamed';
            const type = markup.markupType || markup.MarkupType || 'Percentage';
            const value = markup.value || markup.Value || 0;
            const isActive = markup.isActive !== false && markup.IsActive !== false;

            return `
                <tr>
                    <td>${name}</td>
                    <td>${type}</td>
                    <td>${type === 'Percentage' ? value + '%' : formatCurrency(value)}</td>
                    <td>
                        <span class="status-badge ${isActive ? 'delivered' : 'quote-requested'}">
                            ${isActive ? 'Active' : 'Inactive'}
                        </span>
                    </td>
                    <td>
                        <button class="btn-text" onclick="markupsComponent.editMarkup('${markupId}')">Edit</button>
                        <button class="btn-text" onclick="markupsComponent.deleteMarkup('${markupId}')" style="color: var(--status-error);">Delete</button>
                    </td>
                </tr>
            `;
        }).join('');
    }

    attachEvents() {
        const createBtn = document.getElementById('createMarkupBtn');
        if (createBtn) {
            createBtn.addEventListener('click', () => this.showCreateModal());
        }

        const form = document.getElementById('markupForm');
        if (form) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                this.handleSave();
            });
        }
    }

    showCreateModal() {
        this.editingMarkup = null;
        document.getElementById('modalTitle').textContent = 'Create Markup';
        document.getElementById('markupForm').reset();
        document.getElementById('markupModal').style.display = 'flex';
    }

    async editMarkup(markupId) {
        try {
            showLoading('Loading markup...');
            const result = await shipexAPI.getMarkup(markupId);
            hideLoading();

            this.editingMarkup = result.result || result;
            this.showEditModal();
        } catch (error) {
            hideLoading();
            showToast('Failed to load markup', 'error');
        }
    }

    showEditModal() {
        if (!this.editingMarkup) return;

        document.getElementById('modalTitle').textContent = 'Edit Markup';
        document.getElementById('markupName').value = this.editingMarkup.name || this.editingMarkup.Name || '';
        document.getElementById('markupType').value = this.editingMarkup.markupType || this.editingMarkup.MarkupType || 'Percentage';
        document.getElementById('markupValue').value = this.editingMarkup.value || this.editingMarkup.Value || 0;
        document.getElementById('markupCarriers').value = (this.editingMarkup.carriers || this.editingMarkup.Carriers || []).join(', ');
        document.getElementById('markupModal').style.display = 'flex';
    }

    async handleSave() {
        const errorDiv = document.getElementById('markupError');
        const submitBtn = document.getElementById('markupSubmit');
        
        errorDiv.style.display = 'none';
        submitBtn.disabled = true;
        submitBtn.textContent = 'Saving...';

        try {
            const merchant = shipexAPI.getMerchant();
            if (!merchant) {
                throw new Error('Please log in');
            }

            const carriers = document.getElementById('markupCarriers').value
                .split(',')
                .map(c => c.trim())
                .filter(c => c.length > 0);

            const markup = {
                markupId: this.editingMarkup?.markupId || this.editingMarkup?.MarkupId || this.generateGuid(),
                merchantId: merchant.merchantId || merchant.MerchantId,
                name: document.getElementById('markupName').value.trim(),
                markupType: document.getElementById('markupType').value,
                value: parseFloat(document.getElementById('markupValue').value),
                carriers: carriers.length > 0 ? carriers : null,
                isActive: true
            };

            showLoading('Saving markup...');

            if (this.editingMarkup) {
                await shipexAPI.updateMarkup(markup.markupId, markup);
            } else {
                await shipexAPI.createMarkup(markup);
            }

            hideLoading();
            document.getElementById('markupModal').style.display = 'none';
            showToast('Markup saved successfully', 'success');
            
            await this.loadMarkups();
            this.render();
            this.attachEvents();
        } catch (error) {
            hideLoading();
            errorDiv.textContent = error.message || 'Failed to save markup';
            errorDiv.style.display = 'block';
            submitBtn.disabled = false;
            submitBtn.textContent = 'Save Markup';
        }
    }

    async deleteMarkup(markupId) {
        if (!confirm('Are you sure you want to delete this markup?')) {
            return;
        }

        try {
            const token = shipexAPI.getAuthToken();
            const isDemoToken = token && token.startsWith('demo-token-for-testing');

            // In demo mode, skip API call
            if (isDemoToken) {
                showToast('Markup deleted (Demo mode - not persisted)', 'info');
                await this.loadMarkups();
                this.render();
                this.attachEvents();
                return;
            }

            showLoading('Deleting markup...');
            await shipexAPI.deleteMarkup(markupId);
            hideLoading();
            showToast('Markup deleted', 'success');
            await this.loadMarkups();
            this.render();
            this.attachEvents();
        } catch (error) {
            hideLoading();
            showToast('Failed to delete markup', 'error');
        }
    }

    generateGuid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }
}

let markupsComponent = null;
