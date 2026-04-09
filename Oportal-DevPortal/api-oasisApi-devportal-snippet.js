/**
 * Add these methods to your api/oasisApi.js (inside the oasisAPI object)
 * so the Dev Portal can fetch live stats and resources from the STAR/OASIS API.
 * If the backend does not expose these endpoints, dev-portal.js will use built-in demo data.
 */

// ============================================
// STAR Dev Portal API Methods
// ============================================

/**
 * Get dev portal stats (total resources, downloads, active developers, average rating).
 * Endpoint: GET /api/star/dev-portal/stats (or /star/dev-portal/stats depending on your API)
 * @returns {Promise<{result?: object, isError?: boolean}>}
 */
async getDevPortalStats() {
  return this.request('/api/star/dev-portal/stats');
},

/**
 * Get dev portal resources (CLI, SDKs, docs, tutorials, etc.).
 * Endpoint: GET /api/star/dev-portal/resources
 * @returns {Promise<{result?: array, isError?: boolean}>}
 */
async getDevPortalResources() {
  return this.request('/api/star/dev-portal/resources');
},
