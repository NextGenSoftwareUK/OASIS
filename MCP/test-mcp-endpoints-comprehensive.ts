/**
 * Comprehensive MCP Endpoint Test Script
 * 
 * Tests all available OASIS MCP endpoints based on ENDPOINT_INVENTORY.md
 * Run this script to validate MCP endpoint functionality.
 * 
 * Usage:
 *   npx tsx test-mcp-endpoints-comprehensive.ts
 */

import { OASISClient } from './src/clients/oasisClient.js';
import { handleOASISTool } from './src/tools/oasisTools.js';

const oasisClient = new OASISClient();

interface TestResult {
  endpoint: string;
  success: boolean;
  error?: string;
  result?: any;
  duration: number;
  hasError?: boolean; // Track if result contains isError: true
}

const testResults: TestResult[] = [];

async function testEndpoint(
  name: string,
  args: any,
  description: string,
  skipIfNoAuth = false
): Promise<TestResult> {
  const startTime = Date.now();
  console.log(`\n🧪 Testing: ${name}`);
  console.log(`   Description: ${description}`);

  try {
    const result = await handleOASISTool(name, args);
    const duration = Date.now() - startTime;

    // Check if result indicates an error
    const hasError = result?.isError === true || result?.result?.isError === true;
    const errorMessage = result?.message || result?.result?.message;

    if (hasError && errorMessage) {
      console.log(`   ⚠️  API Error (${duration}ms): ${errorMessage}`);
    } else {
      console.log(`   ✅ Success (${duration}ms)`);
    }

    // Show truncated result
    const resultStr = JSON.stringify(result, null, 2);
    const preview = resultStr.length > 300 ? resultStr.substring(0, 300) + '...' : resultStr;
    console.log(`   Result: ${preview}`);

    return {
      endpoint: name,
      success: !hasError, // Consider it successful if no error flag
      result,
      duration,
      hasError,
    };
  } catch (error: any) {
    const duration = Date.now() - startTime;
    const errorMessage = error.message || String(error);

    console.log(`   ❌ Failed (${duration}ms)`);
    console.log(`   Error: ${errorMessage}`);

    return {
      endpoint: name,
      success: false,
      error: errorMessage,
      duration,
    };
  }
}

async function runTests() {
  console.log('🚀 Starting Comprehensive MCP Endpoint Tests\n');
  console.log('='.repeat(60));

  const testAvatarId = process.env.TEST_AVATAR_ID || '00000000-0000-0000-0000-000000000000';
  const hasAvatarId = testAvatarId !== '00000000-0000-0000-0000-000000000000';
  
  // Authenticate first if credentials are available
  let isAuthenticated = false;
  const testUsername = process.env.OASIS_USERNAME || 'OASIS_ADMIN';
  const testPassword = process.env.OASIS_PASSWORD || process.env.TEST_PASSWORD;
  
  if (testPassword) {
    console.log('\n🔐 AUTHENTICATION');
    console.log('-'.repeat(60));
    try {
      const authResult = await testEndpoint(
        'oasis_authenticate_avatar',
        { username: testUsername, password: testPassword },
        'Authenticate avatar to get JWT token'
      );
      if (authResult.success && !authResult.hasError) {
        isAuthenticated = true;
        console.log('   ✅ Authentication successful - write operations enabled');
      }
    } catch (error) {
      console.log('   ⚠️  Authentication failed - write operations may fail');
    }
  }
  
  // Authenticate first if credentials are available
  let isAuthenticated = false;
  const testUsername = process.env.OASIS_USERNAME || 'OASIS_ADMIN';
  const testPassword = process.env.OASIS_PASSWORD || process.env.TEST_PASSWORD;
  
  if (testPassword) {
    console.log('\n🔐 AUTHENTICATION');
    console.log('-'.repeat(60));
    try {
      const authResult = await testEndpoint(
        'oasis_authenticate_avatar',
        { username: testUsername, password: testPassword },
        'Authenticate avatar to get JWT token'
      );
      if (authResult.success && !authResult.hasError) {
        isAuthenticated = true;
        console.log('   ✅ Authentication successful - write operations enabled');
      }
    } catch (error) {
      console.log('   ⚠️  Authentication failed - write operations may fail');
    }
  }

  // ============================================================
  // UTILITY TESTS (No auth required)
  // ============================================================
  console.log('\n📋 UTILITY TESTS (No Authentication Required)');
  console.log('-'.repeat(60));

  testResults.push(
    await testEndpoint('oasis_health_check', {}, 'Check OASIS API health status')
  );

  testResults.push(
    await testEndpoint('oasis_get_supported_chains', {}, 'Get list of supported blockchain chains')
  );

  if (!hasAvatarId) {
    console.log('\n⚠️  Skipping avatar-specific tests (TEST_AVATAR_ID not set)');
    console.log('   Set TEST_AVATAR_ID environment variable to test avatar endpoints');
    console.log('\n💡 Available endpoints not tested:');
    console.log('   - Write operations (create, mint, save, update, etc.)');
    console.log('   - See WRITE_ENDPOINTS_LIST.md for complete list');
    printSummary();
    return;
  }

  // ============================================================
  // AVATAR TESTS
  // ============================================================
  console.log('\n👤 AVATAR TESTS');
  console.log('-'.repeat(60));

  testResults.push(
    await testEndpoint('oasis_get_avatar', { avatarId: testAvatarId }, 'Get avatar by ID')
  );

  testResults.push(
    await testEndpoint('oasis_get_avatar', { username: 'OASIS_ADMIN' }, 'Get avatar by username')
  );

  testResults.push(
    await testEndpoint('oasis_get_avatar_detail', { avatarId: testAvatarId }, 'Get detailed avatar information by ID')
  );

  testResults.push(
    await testEndpoint('oasis_get_all_avatar_names', { includeUsernames: true, includeIds: true }, 'Get all avatar names')
  );

  testResults.push(
    await testEndpoint('oasis_get_avatar_portrait', { avatarId: testAvatarId }, 'Get avatar portrait by ID')
  );

  // ============================================================
  // KARMA TESTS
  // ============================================================
  console.log('\n⭐ KARMA TESTS');
  console.log('-'.repeat(60));

  testResults.push(
    await testEndpoint('oasis_get_karma', { avatarId: testAvatarId }, 'Get karma score for avatar')
  );

  testResults.push(
    await testEndpoint('oasis_get_karma_stats', { avatarId: testAvatarId }, 'Get karma statistics for avatar')
  );

  testResults.push(
    await testEndpoint('oasis_get_karma_history', { avatarId: testAvatarId, limit: 10 }, 'Get karma history for avatar')
  );

  testResults.push(
    await testEndpoint('oasis_get_karma_akashic_records', { avatarId: testAvatarId }, 'Get karma akashic records for avatar')
  );

  // ============================================================
  // NFT TESTS
  // ============================================================
  console.log('\n🖼️  NFT TESTS');
  console.log('-'.repeat(60));

  testResults.push(
    await testEndpoint('oasis_get_nfts', { avatarId: testAvatarId }, 'Get all NFTs for avatar')
  );

  testResults.push(
    await testEndpoint('oasis_get_geo_nfts', { avatarId: testAvatarId }, 'Get all GeoNFTs for avatar')
  );

  // Note: These require specific IDs/hashes, so they may fail if not available
  // testResults.push(
  //   await testEndpoint('oasis_get_nft', { nftId: 'some-nft-id' }, 'Get NFT details by ID')
  // );

  // ============================================================
  // WALLET TESTS
  // ============================================================
  console.log('\n💰 WALLET TESTS');
  console.log('-'.repeat(60));

  testResults.push(
    await testEndpoint('oasis_get_wallet', { avatarId: testAvatarId }, 'Get wallet information for avatar')
  );

  testResults.push(
    await testEndpoint('oasis_get_provider_wallets', { avatarId: testAvatarId }, 'Get provider wallets for avatar')
  );

  testResults.push(
    await testEndpoint('oasis_get_provider_wallets', { avatarId: testAvatarId, providerType: 'SolanaOASIS' }, 'Get Solana provider wallets')
  );

  testResults.push(
    await testEndpoint('oasis_get_default_wallet', { avatarId: testAvatarId, providerType: 'SolanaOASIS' }, 'Get default Solana wallet')
  );

  testResults.push(
    await testEndpoint('oasis_get_wallets_by_chain', { avatarId: testAvatarId, chain: 'solana' }, 'Get wallets by chain (Solana)')
  );

  testResults.push(
    await testEndpoint('oasis_get_portfolio_value', { avatarId: testAvatarId }, 'Get total portfolio value for avatar')
  );

  // These require wallet IDs - will test if we can get them from previous calls
  // testResults.push(
  //   await testEndpoint('oasis_get_wallet_analytics', { avatarId: testAvatarId, walletId: 'wallet-id' }, 'Get wallet analytics')
  // );

  // testResults.push(
  //   await testEndpoint('oasis_get_wallet_tokens', { avatarId: testAvatarId, walletId: 'wallet-id' }, 'Get tokens in wallet')
  // );

  // ============================================================
  // HOLON/DATA TESTS
  // ============================================================
  console.log('\n📦 HOLON/DATA TESTS');
  console.log('-'.repeat(60));

  // Note: These require specific holon IDs, so they may fail
  // testResults.push(
  //   await testEndpoint('oasis_get_holon', { holonId: 'some-holon-id' }, 'Get holon by ID')
  // );

  testResults.push(
    await testEndpoint('oasis_load_holons_for_parent', { parentId: testAvatarId }, 'Load holons for parent (avatar)')
  );

  // ============================================================
  // SEARCH TESTS
  // ============================================================
  console.log('\n🔍 SEARCH TESTS');
  console.log('-'.repeat(60));

  testResults.push(
    await testEndpoint('oasis_search_avatars', { searchQuery: 'OASIS', limit: 5 }, 'Search avatars')
  );

  testResults.push(
    await testEndpoint('oasis_search_nfts', { searchQuery: 'test', limit: 5 }, 'Search NFTs')
  );

  testResults.push(
    await testEndpoint('oasis_search_holons', { searchQuery: 'test', limit: 5 }, 'Search holons')
  );

  testResults.push(
    await testEndpoint('oasis_basic_search', { searchQuery: 'OASIS', limit: 10 }, 'Basic search across OASIS')
  );

  // ============================================================
  // WRITE/CREATE OPERATIONS (Requires Authentication)
  // ============================================================
  console.log('\n✏️  WRITE/CREATE OPERATIONS');
  console.log('-'.repeat(60));

  if (isAuthenticated && hasAvatarId) {
    // Wallet Creation Tests
    console.log('\n💰 Wallet Creation:');
    testResults.push(
      await testEndpoint(
        'oasis_create_solana_wallet',
        { avatarId: testAvatarId, setAsDefault: false },
        'Create Solana wallet (new endpoint with correct order)'
      )
    );

    // Note: These create operations are commented out to avoid creating test data
    // Uncomment to test actual creation:
    
    // testResults.push(
    //   await testEndpoint(
    //     'oasis_create_wallet_full',
    //     { avatarId: testAvatarId, WalletProviderType: 'SolanaOASIS', GenerateKeyPair: true },
    //     'Create wallet with full options'
    //   )
    // );

    // Holon Creation Test
    console.log('\n📦 Holon Operations:');
    testResults.push(
      await testEndpoint(
        'oasis_save_holon',
        {
          holon: {
            name: 'Test Holon',
            description: 'Test holon created by MCP test script',
            holonType: 0, // Generic holon
          },
        },
        'Save/create a test holon'
      )
    );

    // Karma Operations
    console.log('\n⭐ Karma Operations:');
    // Note: These modify karma, so commented out by default
    // testResults.push(
    //   await testEndpoint(
    //     'oasis_add_karma',
    //     { avatarId: testAvatarId, KarmaType: 'HelpOtherPerson', karmaSourceType: 'App' },
    //     'Add positive karma to avatar'
    //   )
    // );

    // Wallet Operations
    console.log('\n💼 Wallet Management:');
    // Get a wallet ID first if available
    // testResults.push(
    //   await testEndpoint(
    //     'oasis_set_default_wallet',
    //     { avatarId: testAvatarId, walletId: 'wallet-id', providerType: 'SolanaOASIS' },
    //     'Set default wallet'
    //   )
    // );
  } else {
    console.log('⚠️  Skipping write operations (authentication required)');
    console.log('   Set OASIS_USERNAME and OASIS_PASSWORD to test write operations');
  }

  // ============================================================
  // WRITE/CREATE OPERATIONS (Requires Authentication)
  // ============================================================
  console.log('\n✏️  WRITE/CREATE OPERATIONS');
  console.log('-'.repeat(60));

  if (isAuthenticated && hasAvatarId) {
    // Wallet Creation Tests
    console.log('\n💰 Wallet Creation:');
    testResults.push(
      await testEndpoint(
        'oasis_create_solana_wallet',
        { avatarId: testAvatarId, setAsDefault: false },
        'Create Solana wallet (new endpoint with correct order)'
      )
    );

    // Note: These create operations are commented out to avoid creating test data
    // Uncomment to test actual creation:
    
    // testResults.push(
    //   await testEndpoint(
    //     'oasis_create_wallet_full',
    //     { avatarId: testAvatarId, WalletProviderType: 'SolanaOASIS', GenerateKeyPair: true },
    //     'Create wallet with full options'
    //   )
    // );

    // Holon Creation Test
    console.log('\n📦 Holon Operations:');
    testResults.push(
      await testEndpoint(
        'oasis_save_holon',
        {
          holon: {
            name: 'Test Holon',
            description: 'Test holon created by MCP test script',
            holonType: 0, // Generic holon
          },
        },
        'Save/create a test holon'
      )
    );

    // Karma Operations
    console.log('\n⭐ Karma Operations:');
    // Note: These modify karma, so commented out by default
    // testResults.push(
    //   await testEndpoint(
    //     'oasis_add_karma',
    //     { avatarId: testAvatarId, KarmaType: 'HelpOtherPerson', karmaSourceType: 'App' },
    //     'Add positive karma to avatar'
    //   )
    // );

    // Wallet Operations
    console.log('\n💼 Wallet Management:');
    // Get a wallet ID first if available
    // testResults.push(
    //   await testEndpoint(
    //     'oasis_set_default_wallet',
    //     { avatarId: testAvatarId, walletId: 'wallet-id', providerType: 'SolanaOASIS' },
    //     'Set default wallet'
    //   )
    // );
  } else {
    console.log('⚠️  Skipping write operations (authentication required)');
    console.log('   Set OASIS_USERNAME and OASIS_PASSWORD to test write operations');
    console.log('   See WRITE_ENDPOINTS_LIST.md for all available write endpoints');
    console.log('   Run: npx tsx test-all-write-endpoints.ts for dedicated write tests');
  }

  // ============================================================
  // A2A TESTS (if authenticated as agent)
  // ============================================================
  console.log('\n🤖 A2A TESTS');
  console.log('-'.repeat(60));

  testResults.push(
    await testEndpoint('oasis_get_all_agents', {}, 'Get all A2A agents', true)
  );

  testResults.push(
    await testEndpoint('oasis_discover_agents_via_serv', {}, 'Discover agents via SERV', true)
  );

  testResults.push(
    await testEndpoint('oasis_get_agents_by_service', { serviceName: 'data-analysis' }, 'Get agents by service name', true)
  );

  if (isAuthenticated) {
    testResults.push(
      await testEndpoint('oasis_get_my_agents', {}, 'Get my agents (authenticated user)', true)
    );
  }

  testResults.push(
    await testEndpoint('oasis_get_agents_by_service', { serviceName: 'data-analysis' }, 'Get agents by service name', true)
  );

  if (isAuthenticated) {
    testResults.push(
      await testEndpoint('oasis_get_my_agents', {}, 'Get my agents (authenticated user)', true)
    );
  }

  // ============================================================
  // PRINT SUMMARY
  // ============================================================
  printSummary();
}

function printSummary() {
  console.log('\n' + '='.repeat(60));
  console.log('📊 Test Summary\n');

  const successful = testResults.filter((r) => r.success).length;
  const withApiErrors = testResults.filter((r) => r.hasError).length;
  const failed = testResults.filter((r) => !r.success && !r.hasError).length;
  const totalDuration = testResults.reduce((sum, r) => sum + r.duration, 0);
  const avgDuration = testResults.length > 0 ? totalDuration / testResults.length : 0;

  console.log(`Total Tests: ${testResults.length}`);
  console.log(`✅ Successful: ${successful}`);
  if (withApiErrors > 0) {
    console.log(`⚠️  API Errors (endpoint worked but returned error): ${withApiErrors}`);
  }
  console.log(`❌ Failed: ${failed}`);
  console.log(`⏱️  Total Duration: ${totalDuration}ms`);
  console.log(`⏱️  Average Duration: ${Math.round(avgDuration)}ms\n`);

  if (withApiErrors > 0) {
    console.log('⚠️  Endpoints with API Errors (endpoint works but returned error response):');
    testResults
      .filter((r) => r.hasError)
      .forEach((r) => {
        const errorMsg = r.result?.message || r.result?.result?.message || 'Unknown error';
        console.log(`   - ${r.endpoint}: ${errorMsg}`);
      });
    console.log('');
  }

  if (failed > 0) {
    console.log('❌ Failed Tests:');
    testResults
      .filter((r) => !r.success && !r.hasError)
      .forEach((r) => {
        console.log(`   - ${r.endpoint}: ${r.error}`);
      });
    console.log('');
  }

  // Group by category
  console.log('📋 Detailed Results by Category:\n');
  
  const categories: { [key: string]: TestResult[] } = {
    'Utility': testResults.filter(r => r.endpoint.includes('health') || r.endpoint.includes('supported')),
    'Authentication': testResults.filter(r => r.endpoint.includes('authenticate')),
    'Avatar': testResults.filter(r => r.endpoint.includes('avatar') && !r.endpoint.includes('search') && !r.endpoint.includes('authenticate')),
    'Karma': testResults.filter(r => r.endpoint.includes('karma')),
    'NFT': testResults.filter(r => r.endpoint.includes('nft')),
    'Wallet': testResults.filter(r => r.endpoint.includes('wallet') || r.endpoint.includes('portfolio')),
    'Holon/Data': testResults.filter(r => r.endpoint.includes('holon') || r.endpoint.includes('load')),
    'Search': testResults.filter(r => r.endpoint.includes('search')),
    'Write/Create': testResults.filter(r => 
      r.endpoint.includes('create') || 
      r.endpoint.includes('mint') || 
      r.endpoint.includes('save') || 
      r.endpoint.includes('add') || 
      r.endpoint.includes('send') || 
      r.endpoint.includes('set') || 
      r.endpoint.includes('import') ||
      r.endpoint.includes('register') ||
      r.endpoint.includes('update') ||
      r.endpoint.includes('delete') ||
      r.endpoint.includes('vote')
    ),
    'A2A': testResults.filter(r => r.endpoint.includes('agent') || r.endpoint.includes('a2a')),
  };

  for (const [category, results] of Object.entries(categories)) {
    if (results.length > 0) {
      console.log(`\n${category}:`);
      results.forEach((result) => {
        const icon = result.success ? '✅' : result.hasError ? '⚠️' : '❌';
        console.log(`   ${icon} ${result.endpoint} (${result.duration}ms)`);
      });
    }
  }

  // Exit with appropriate code
  process.exit(failed > 0 ? 1 : 0);
}

// Run tests
runTests().catch((error) => {
  console.error('💥 Fatal error running tests:', error);
  process.exit(1);
});
