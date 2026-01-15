/**
 * MCP Endpoint Test Script (Basic)
 * 
 * Tests basic OASIS MCP endpoints to verify they're working correctly.
 * For comprehensive testing of all ~75 endpoints, use:
 *   npx tsx test-mcp-endpoints-comprehensive.ts
 * 
 * Usage:
 *   npx tsx test-mcp-endpoints.ts
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
}

const testResults: TestResult[] = [];

async function testEndpoint(
  name: string,
  args: any,
  description: string
): Promise<TestResult> {
  const startTime = Date.now();
  console.log(`\n🧪 Testing: ${name}`);
  console.log(`   Description: ${description}`);
  console.log(`   Args: ${JSON.stringify(args, null, 2)}`);

  try {
    const result = await handleOASISTool(name, args);
    const duration = Date.now() - startTime;

    console.log(`   ✅ Success (${duration}ms)`);
    console.log(`   Result: ${JSON.stringify(result, null, 2).substring(0, 200)}...`);

    return {
      endpoint: name,
      success: true,
      result,
      duration,
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
  console.log('🚀 Starting MCP Endpoint Tests\n');
  console.log('='.repeat(60));

  // Test 1: Health Check (no auth required)
  testResults.push(
    await testEndpoint('oasis_health_check', {}, 'Check OASIS API health status')
  );

  // Test 2: Get Avatar (requires valid avatar ID - using a test ID)
  // Note: Replace with a valid avatar ID from your system
  const testAvatarId = process.env.TEST_AVATAR_ID || '00000000-0000-0000-0000-000000000000';
  
  if (testAvatarId !== '00000000-0000-0000-0000-000000000000') {
    testResults.push(
      await testEndpoint(
        'oasis_get_avatar',
        { avatarId: testAvatarId },
        'Get avatar by ID'
      )
    );

    // Test 3: Get Karma
    testResults.push(
      await testEndpoint(
        'oasis_get_karma',
        { avatarId: testAvatarId },
        'Get karma score for avatar'
      )
    );

    // Test 4: Get NFTs
    testResults.push(
      await testEndpoint(
        'oasis_get_nfts',
        { avatarId: testAvatarId },
        'Get all NFTs for avatar'
      )
    );

    // Test 5: Get Wallet
    testResults.push(
      await testEndpoint(
        'oasis_get_wallet',
        { avatarId: testAvatarId },
        'Get wallet information for avatar'
      )
    );

    // Test 6: Get Provider Wallets
    testResults.push(
      await testEndpoint(
        'oasis_get_provider_wallets',
        { avatarId: testAvatarId },
        'Get provider wallets for avatar'
      )
    );

    // Test 7: Get Portfolio Value
    testResults.push(
      await testEndpoint(
        'oasis_get_portfolio_value',
        { avatarId: testAvatarId },
        'Get total portfolio value for avatar'
      )
    );

    // Test 8: Get Karma Stats
    testResults.push(
      await testEndpoint(
        'oasis_get_karma_stats',
        { avatarId: testAvatarId },
        'Get karma statistics for avatar'
      )
    );

    // Test 9: Get Supported Chains
    testResults.push(
      await testEndpoint(
        'oasis_get_supported_chains',
        {},
        'Get list of supported blockchain chains'
      )
    );

    // Test 10: Search Avatars
    testResults.push(
      await testEndpoint(
        'oasis_search_avatars',
        { searchQuery: 'test', limit: 5 },
        'Search avatars'
      )
    );

    // Test 11: Search NFTs
    testResults.push(
      await testEndpoint(
        'oasis_search_nfts',
        { searchQuery: 'test', limit: 5 },
        'Search NFTs'
      )
    );

    // Test 12: Search Holons
    testResults.push(
      await testEndpoint(
        'oasis_search_holons',
        { searchQuery: 'test', limit: 5 },
        'Search holons'
      )
    );

    // Test 13: Get Avatar Detail
    testResults.push(
      await testEndpoint(
        'oasis_get_avatar_detail',
        { avatarId: testAvatarId },
        'Get detailed avatar information'
      )
    );

    // Test 14: Get Avatar Portrait
    testResults.push(
      await testEndpoint(
        'oasis_get_avatar_portrait',
        { avatarId: testAvatarId },
        'Get avatar portrait'
      )
    );

    // Test 15: Get All Avatar Names
    testResults.push(
      await testEndpoint(
        'oasis_get_all_avatar_names',
        { includeUsernames: true, includeIds: true },
        'Get all avatar names'
      )
    );

    // Test 16: Get GeoNFTs
    testResults.push(
      await testEndpoint(
        'oasis_get_geo_nfts',
        { avatarId: testAvatarId },
        'Get all GeoNFTs for avatar'
      )
    );

    // Test 17: Get Karma History
    testResults.push(
      await testEndpoint(
        'oasis_get_karma_history',
        { avatarId: testAvatarId, limit: 10 },
        'Get karma history for avatar'
      )
    );

    // Test 18: Get Karma Akashic Records
    testResults.push(
      await testEndpoint(
        'oasis_get_karma_akashic_records',
        { avatarId: testAvatarId },
        'Get karma akashic records for avatar'
      )
    );

    // Test 19: Get Default Wallet
    testResults.push(
      await testEndpoint(
        'oasis_get_default_wallet',
        { avatarId: testAvatarId, providerType: 'SolanaOASIS' },
        'Get default Solana wallet'
      )
    );

    // Test 20: Get Wallets by Chain
    testResults.push(
      await testEndpoint(
        'oasis_get_wallets_by_chain',
        { avatarId: testAvatarId, chain: 'solana' },
        'Get wallets by chain (Solana)'
      )
    );

    // Test 21: Load Holons for Parent
    testResults.push(
      await testEndpoint(
        'oasis_load_holons_for_parent',
        { parentId: testAvatarId },
        'Load holons for parent (avatar)'
      )
    );

    // Test 22: Basic Search
    testResults.push(
      await testEndpoint(
        'oasis_basic_search',
        { searchQuery: 'OASIS', limit: 10 },
        'Basic search across OASIS'
      )
    );

    // Test 23: Get All Agents
    testResults.push(
      await testEndpoint(
        'oasis_get_all_agents',
        {},
        'Get all A2A agents'
      )
    );

    // Test 24: Discover Agents via SERV
    testResults.push(
      await testEndpoint(
        'oasis_discover_agents_via_serv',
        {},
        'Discover agents via SERV'
      )
    );
  } else {
    console.log('\n⚠️  Skipping avatar-specific tests (TEST_AVATAR_ID not set)');
    console.log('   Set TEST_AVATAR_ID environment variable to test avatar endpoints');
  }

  console.log('\n💡 Tip: For comprehensive testing of all ~75 endpoints, run:');
  console.log('   npx tsx test-mcp-endpoints-comprehensive.ts');

  // Print Summary
  console.log('\n' + '='.repeat(60));
  console.log('📊 Test Summary\n');

  const successful = testResults.filter((r) => r.success).length;
  const failed = testResults.filter((r) => !r.success).length;
  const totalDuration = testResults.reduce((sum, r) => sum + r.duration, 0);
  const avgDuration = testResults.length > 0 ? totalDuration / testResults.length : 0;

  console.log(`Total Tests: ${testResults.length}`);
  console.log(`✅ Successful: ${successful}`);
  console.log(`❌ Failed: ${failed}`);
  console.log(`⏱️  Total Duration: ${totalDuration}ms`);
  console.log(`⏱️  Average Duration: ${Math.round(avgDuration)}ms\n`);

  if (failed > 0) {
    console.log('❌ Failed Tests:');
    testResults
      .filter((r) => !r.success)
      .forEach((r) => {
        console.log(`   - ${r.endpoint}: ${r.error}`);
      });
    console.log('');
  }

  // Detailed Results
  console.log('📋 Detailed Results:');
  testResults.forEach((result) => {
    const icon = result.success ? '✅' : '❌';
    console.log(
      `   ${icon} ${result.endpoint} (${result.duration}ms)`
    );
    if (!result.success) {
      console.log(`      Error: ${result.error}`);
    }
  });

  // Exit with appropriate code
  process.exit(failed > 0 ? 1 : 0);
}

// Run tests
runTests().catch((error) => {
  console.error('💥 Fatal error running tests:', error);
  process.exit(1);
});
