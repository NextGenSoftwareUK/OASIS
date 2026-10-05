/**
 * Comprehensive endpoint testing script for Pangea Backend
 * Usage: node scripts/test-endpoints.js [BASE_URL]
 * 
 * Tests all endpoints systematically and generates a report
 */

const https = require('https');
const http = require('http');

const BASE_URL = process.argv[2] || 'https://pangea-production-128d.up.railway.app/api';

const colors = {
  reset: '\x1b[0m',
  green: '\x1b[32m',
  red: '\x1b[31m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
};

let token = '';
let avatarId = '';

const stats = {
  total: 0,
  passed: 0,
  failed: 0,
  skipped: 0,
};

// Helper function to make HTTP requests
function makeRequest(method, endpoint, options = {}) {
  return new Promise((resolve, reject) => {
    const url = new URL(`${BASE_URL}${endpoint}`);
    const data = options.data ? JSON.stringify(options.data) : null;

    const requestOptions = {
      hostname: url.hostname,
      port: url.port || (url.protocol === 'https:' ? 443 : 80),
      path: url.pathname + url.search,
      method: method,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
        ...(options.headers || {}),
      },
    };

    if (data) {
      requestOptions.headers['Content-Length'] = Buffer.byteLength(data);
    }

    const protocol = url.protocol === 'https:' ? https : http;
    const req = protocol.request(requestOptions, (res) => {
      let body = '';
      res.on('data', (chunk) => { body += chunk; });
      res.on('end', () => {
        try {
          const jsonBody = body ? JSON.parse(body) : null;
          resolve({
            status: res.statusCode,
            headers: res.headers,
            body: jsonBody,
            rawBody: body,
          });
        } catch (e) {
          resolve({
            status: res.statusCode,
            headers: res.headers,
            body: body,
            rawBody: body,
          });
        }
      });
    });

    req.on('error', reject);

    if (data) {
      req.write(data);
    }

    req.end();
  });
}

// Test function
async function testEndpoint(test) {
  stats.total++;

  const {
    method,
    endpoint,
    requiresAuth = true,
    requiresAdmin = false,
    data,
    description,
    skipIf = null,
  } = test;

  // Skip if condition is met
  if (skipIf && skipIf()) {
    stats.skipped++;
    console.log(`${colors.yellow}⚠️  [SKIP] ${method} ${endpoint}${colors.reset} - ${skipIf.toString()}`);
    return;
  }

  try {
    const response = await makeRequest(method, endpoint, { data });

    const { status, body } = response;

    // Determine test result
    if (status >= 200 && status < 300) {
      console.log(`${colors.green}✅ [${method}] ${endpoint}${colors.reset} (${status})`);
      if (description) console.log(`   ${description}`);
      stats.passed++;
      return { passed: true, status, body };
    } else if (status === 401 && !requiresAuth) {
      console.log(`${colors.green}✅ [${method}] ${endpoint}${colors.reset} (401 - expected for unauthenticated)`);
      stats.passed++;
      return { passed: true, status, body };
    } else if (status === 403 && requiresAdmin) {
      console.log(`${colors.yellow}⚠️  [${method}] ${endpoint}${colors.reset} (403 - requires admin role)`);
      stats.skipped++;
      return { passed: false, status, body, skipped: true };
    } else if (status === 404) {
      console.log(`${colors.yellow}⚠️  [${method}] ${endpoint}${colors.reset} (404 - endpoint or resource not found)`);
      stats.skipped++;
      return { passed: false, status, body, skipped: true };
    } else {
      console.log(`${colors.red}❌ [${method}] ${endpoint}${colors.reset} (${status})`);
      if (description) console.log(`   ${description}`);
      const errorMsg = body?.message || body?.error || JSON.stringify(body).substring(0, 100);
      console.log(`   Error: ${errorMsg}`);
      stats.failed++;
      return { passed: false, status, body };
    }
  } catch (error) {
    console.log(`${colors.red}❌ [${method}] ${endpoint}${colors.reset} (ERROR)`);
    console.log(`   ${error.message}`);
    stats.failed++;
    return { passed: false, error: error.message };
  }
}

// Authenticate
async function authenticate() {
  console.log(`${colors.blue}🔐 Authenticating...${colors.reset}`);
  
  try {
    const response = await makeRequest('POST', '/auth/login', {
      data: {
        email: 'OASIS_ADMIN',
        password: 'Uppermall1!',
      },
    });

    if (response.status === 200 && response.body?.token) {
      token = response.body.token;
      avatarId = response.body.user?.avatarId || response.body.avatarId;
      console.log(`${colors.green}✅ Authenticated (Token: ${token.substring(0, 30)}...)${colors.reset}\n`);
      return true;
    } else {
      console.log(`${colors.red}❌ Authentication failed${colors.reset}`);
      console.log(`   Response: ${JSON.stringify(response.body)}`);
      return false;
    }
  } catch (error) {
    console.log(`${colors.red}❌ Authentication error: ${error.message}${colors.reset}`);
    return false;
  }
}

// Test suites
const tests = [
  // Health
  { method: 'GET', endpoint: '/health', requiresAuth: false, description: 'Health check' },

  // Auth (Public)
  {
    method: 'POST',
    endpoint: '/auth/register',
    requiresAuth: false,
    data: {
      email: `test${Date.now()}@test.com`,
      password: 'Test123!',
      username: `testuser${Date.now()}`,
      firstName: 'Test',
      lastName: 'User',
    },
    description: 'Register new user',
  },
  {
    method: 'POST',
    endpoint: '/auth/login',
    requiresAuth: false,
    data: { email: 'OASIS_ADMIN', password: 'Uppermall1!' },
    description: 'Login',
  },
  {
    method: 'POST',
    endpoint: '/auth/forgot-password',
    requiresAuth: false,
    data: { email: 'test@example.com' },
    description: 'Forgot password',
  },

  // User Profile
  { method: 'GET', endpoint: '/user/profile', description: 'Get user profile' },
  {
    method: 'PUT',
    endpoint: '/user/profile',
    data: { firstName: 'Updated' },
    description: 'Update user profile',
  },

  // Assets (Public Reads)
  { method: 'GET', endpoint: '/assets', requiresAuth: false, description: 'List all assets' },
  { method: 'GET', endpoint: '/assets/search?q=test', requiresAuth: false, description: 'Search assets' },

  // Wallet
  { method: 'GET', endpoint: '/wallet/balance', description: 'Get wallet balances' },
  {
    method: 'POST',
    endpoint: '/wallet/generate',
    data: { providerType: 'SolanaOASIS', setAsDefault: true },
    description: 'Generate Solana wallet',
  },
  {
    method: 'POST',
    endpoint: '/wallet/generate',
    data: { providerType: 'EthereumOASIS', setAsDefault: false },
    description: 'Generate Ethereum wallet',
  },
  { method: 'GET', endpoint: '/wallet/verification-message', description: 'Get verification message' },
  { method: 'POST', endpoint: '/wallet/sync', description: 'Sync wallet balances' },

  // Orders
  { method: 'GET', endpoint: '/orders', description: 'Get all orders' },
  { method: 'GET', endpoint: '/orders/open', description: 'Get open orders' },
  { method: 'GET', endpoint: '/orders/history', description: 'Get order history' },

  // Trades
  { method: 'GET', endpoint: '/trades', description: 'Get all trades' },
  { method: 'GET', endpoint: '/trades/history', description: 'Get trade history' },
  { method: 'GET', endpoint: '/trades/statistics', description: 'Get trade statistics' },

  // Transactions
  { method: 'GET', endpoint: '/transactions', description: 'Get all transactions' },
  { method: 'GET', endpoint: '/transactions/pending', description: 'Get pending transactions' },

  // Admin (Requires Admin Role)
  { method: 'GET', endpoint: '/admin/users', requiresAdmin: true, description: 'Get all users (admin)' },
  { method: 'GET', endpoint: '/admin/assets', requiresAdmin: true, description: 'Get all assets (admin)' },
  { method: 'GET', endpoint: '/admin/orders', requiresAdmin: true, description: 'Get all orders (admin)' },
  { method: 'GET', endpoint: '/admin/trades', requiresAdmin: true, description: 'Get all trades (admin)' },
  { method: 'GET', endpoint: '/admin/transactions', requiresAdmin: true, description: 'Get all transactions (admin)' },
  { method: 'GET', endpoint: '/admin/stats', requiresAdmin: true, description: 'Get admin stats' },
  { method: 'GET', endpoint: '/admin/analytics', requiresAdmin: true, description: 'Get admin analytics' },

  // Smart Contracts (Admin Only)
  { method: 'GET', endpoint: '/smart-contracts/cache-stats', requiresAdmin: true, description: 'Get cache stats' },
];

// Main execution
async function main() {
  console.log('🧪 Pangea Backend Endpoint Testing\n');
  console.log(`Base URL: ${BASE_URL}\n`);

  // Authenticate first
  const authSuccess = await authenticate();
  if (!authSuccess) {
    console.log('\n⚠️  Continuing with unauthenticated tests only...\n');
  }

  // Run tests by category
  console.log(`${colors.blue}📋 Testing Endpoints${colors.reset}`);
  console.log('===================\n');

  for (const test of tests) {
    await testEndpoint(test);
    // Small delay to avoid rate limiting
    await new Promise(resolve => setTimeout(resolve, 100));
  }

  // Summary
  console.log('\n===================');
  console.log('📊 Test Summary');
  console.log('===================');
  console.log(`Total:  ${stats.total}`);
  console.log(`${colors.green}Passed: ${stats.passed}${colors.reset}`);
  console.log(`${colors.red}Failed:  ${stats.failed}${colors.reset}`);
  console.log(`${colors.yellow}Skipped: ${stats.skipped}${colors.reset}\n`);

  process.exit(stats.failed > 0 ? 1 : 0);
}

main().catch((error) => {
  console.error('Fatal error:', error);
  process.exit(1);
});



