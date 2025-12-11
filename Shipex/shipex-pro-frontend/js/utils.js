/**
 * Utility Functions
 */

/**
 * Show loading overlay
 */
function showLoading(message = 'Loading...') {
    const overlay = document.getElementById('loadingOverlay');
    const text = overlay.querySelector('.loading-text');
    if (text) text.textContent = message;
    overlay.style.display = 'flex';
}

/**
 * Hide loading overlay
 */
function hideLoading() {
    const overlay = document.getElementById('loadingOverlay');
    overlay.style.display = 'none';
}

/**
 * Show toast notification
 */
function showToast(message, type = 'info', duration = 3000) {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;
    
    container.appendChild(toast);
    
    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease-out';
        setTimeout(() => {
            container.removeChild(toast);
        }, 300);
    }, duration);
}

/**
 * Format date
 */
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

/**
 * Format currency
 */
function formatCurrency(amount, currency = 'USD') {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: currency
    }).format(amount);
}

/**
 * Get status badge class
 */
function getStatusBadgeClass(status) {
    const statusLower = status.toLowerCase();
    if (statusLower.includes('quote')) return 'quote-requested';
    if (statusLower.includes('transit')) return 'in-transit';
    if (statusLower.includes('delivered')) return 'delivered';
    if (statusLower.includes('error') || statusLower.includes('cancelled')) return 'error';
    return 'quote-requested';
}

/**
 * Copy to clipboard
 */
async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        showToast('Copied to clipboard', 'success');
    } catch (err) {
        console.error('Failed to copy:', err);
        showToast('Failed to copy', 'error');
    }
}

/**
 * Debounce function
 */
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

/**
 * Format tracking number (add spaces for readability)
 */
function formatTrackingNumber(trackingNumber) {
    if (!trackingNumber) return '';
    // Add space every 4 characters
    return trackingNumber.replace(/(.{4})/g, '$1 ').trim();
}
