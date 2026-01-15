/**
 * Test All Write/Create MCP Endpoints
 * 
 * Tests all write/create operations available in OASIS MCP.
 * These operations modify data, so use with caution.
 * 
 * Usage:
 *   npx tsx test-all-write-endpoints.ts
 */

import { handleOASISTool } from './src/tools/oasisTools.js';

interface TestResult {
  endpoint: string;
  success: boolean;
  error?: string;
  result?: any;
  duration: number;
  hasError?: boolean;
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

  try {
    const result = await handleOASISTool(name, args);
    const duration = Date.now() - startTime;

    const hasError = result?.isError === true || result?.result?.isError === true;
    const errorMessage = result?.message || result?.result?.message;

    if (hasError && errorMessage) {
      console.log(`   ⚠️  API Error (${duration}ms): ${errorMessage}`);
    } else {
      console.log(`   ✅ Success (${duration}ms)`);
    }

    const resultStr = JSON.stringify(result, null, 2);
    const preview = resultStr.length > 300 ? resultStr.substring(0, 300) + '...' : resultStr;
    console.log(`   Result: ${preview}`);

    return {
      endpoint: name,
      success: !hasError,
      result,
      duration,
      hasError,
    };
  } catch (error: any) {
    const duration = Date.now() - startTime;
    console.log(`   ❌ Failed (${duration}ms): ${error.message}`);
    return {
      endpoint: name,
      success: false,
      error: error.message,
      duration,
    };
  }
}

async function runTests() {
  console.log('🚀 Testing All Write/Create MCP Endpoints\n');
  console.log('='.repeat(60));
  console.log('⚠️  WARNING: These tests will CREATE/MODIFY data!');
  console.log('='.repeat(60));

  const testAvatarId = process.env.TEST_AVATAR_ID;
  const testUsername = process.env.OASIS_USERNAME || 'OASIS_ADMIN';
  const testPassword = process.env.OASIS_PASSWORD || process.env.TEST_PASSWORD;

  if (!testAvatarId || !testPassword) {
    console.log('\n❌ Missing required environment variables:');
    console.log('   - TEST_AVATAR_ID: Avatar ID for testing');
    console.log('   - OASIS_PASSWORD or TEST_PASSWORD: Password for authentication');
    console.log('\nExample:');
    console.log('   export TEST_AVATAR_ID="your-avatar-id"');
    console.log('   export OASIS_PASSWORD="your-password"');
    console.log('   npx tsx test-all-write-endpoints.ts');
    process.exit(1);
  }

  // Authenticate first
  console.log('\n🔐 AUTHENTICATION');
  console.log('-'.repeat(60));
  const authResult = await testEndpoint(
    'oasis_authenticate_avatar',
    { username: testUsername, password: testPassword },
    'Authenticate to get JWT token'
  );

  if (authResult.hasError || !authResult.success) {
    console.log('\n❌ Authentication failed - cannot proceed with write operations');
    process.exit(1);
  }

  console.log('\n✅ Authentication successful - proceeding with write operations\n');

  // ============================================================
  // WALLET CREATION
  // ============================================================
  console.log('\n💰 WALLET CREATION OPERATIONS');
  console.log('-'.repeat(60));

  testResults.push(
    await testEndpoint(
      'oasis_create_solana_wallet',
      { avatarId: testAvatarId, setAsDefault: false },
      'Create Solana wallet (new endpoint with correct order)'
    )
  );

  // Get wallet ID from previous result if available
  let walletId: string | undefined;
  if (testResults[testResults.length - 1].result?.walletId) {
    walletId = testResults[testResults.length - 1].result.walletId;
  }

  // ============================================================
  // HOLON OPERATIONS
  // ============================================================
  console.log('\n📦 HOLON OPERATIONS');
  console.log('-'.repeat(60));

  testResults.push(
    await testEndpoint(
      'oasis_save_holon',
      {
        holon: {
          name: `Test Holon ${Date.now()}`,
          description: 'Test holon created by MCP test script',
          holonType: 0,
        },
      },
      'Save/create a test holon'
    )
  );

  // Get holon ID if available
  let holonId: string | undefined;
  const saveHolonResult = testResults[testResults.length - 1];
  if (saveHolonResult.result?.id || saveHolonResult.result?.result?.id) {
    holonId = saveHolonResult.result?.id || saveHolonResult.result?.result?.id;
  }

  if (holonId) {
    testResults.push(
      await testEndpoint(
        'oasis_update_holon',
        {
          holonId,
          holon: {
            name: `Updated Test Holon ${Date.now()}`,
            description: 'Updated test holon',
          },
        },
        'Update the test holon'
      )
    );

    // Uncomment to test deletion:
    // testResults.push(
    //   await testEndpoint(
    //     'oasis_delete_holon',
    //     { holonId },
    //     'Delete the test holon'
    //   )
    // );
  }

  // ============================================================
  // AVATAR OPERATIONS
  // ============================================================
  console.log('\n👤 AVATAR OPERATIONS');
  console.log('-'.repeat(60));

  // Note: Registration creates a new avatar, so commented out
  // testResults.push(
  //   await testEndpoint(
  //     'oasis_register_avatar',
  //     {
  //       username: `test_user_${Date.now()}`,
  //       email: `test_${Date.now()}@example.com`,
  //       password: 'TestPassword123!',
  //       confirmPassword: 'TestPassword123!',
  //       acceptTerms: true,
  //     },
  //     'Register a new test avatar'
  //   )
  // );

  // Update avatar (safe operation)
  testResults.push(
    await testEndpoint(
      'oasis_update_avatar',
      {
        avatarId: testAvatarId,
        updates: {
          description: `Updated via MCP test at ${new Date().toISOString()}`,
        },
      },
      'Update avatar information'
    )
  );

  // ============================================================
  // KARMA OPERATIONS
  // ============================================================
  console.log('\n⭐ KARMA OPERATIONS');
  console.log('-'.repeat(60));

  // Note: These modify karma - uncomment to test
  // testResults.push(
  //   await testEndpoint(
  //     'oasis_add_karma',
  //     {
  //       avatarId: testAvatarId,
  //       KarmaType: 'HelpOtherPerson',
  //       karmaSourceType: 'App',
  //       KaramSourceTitle: 'MCP Test',
  //     },
  //     'Add positive karma to avatar'
  //   )
  // );

  // testResults.push(
  //   await testEndpoint(
  //     'oasis_vote_positive_karma_weighting',
  //     {
  //       karmaType: 'HelpOtherPerson',
  //       weighting: 10,
  //     },
  //     'Vote for positive karma weighting'
  //   )
  // );

  // ============================================================
  // WALLET MANAGEMENT
  // ============================================================
  console.log('\n💼 WALLET MANAGEMENT');
  console.log('-'.repeat(60));

  if (walletId) {
    testResults.push(
      await testEndpoint(
        'oasis_set_default_wallet',
        {
          avatarId: testAvatarId,
          walletId,
          providerType: 'SolanaOASIS',
        },
        'Set wallet as default'
      )
    );
  }

  // ============================================================
  // NFT OPERATIONS
  // ============================================================
  console.log('\n🖼️  NFT OPERATIONS');
  console.log('-'.repeat(60));

  // Note: Minting creates actual NFTs - uncomment to test
  // testResults.push(
  //   await testEndpoint(
  //     'oasis_mint_nft',
  //     {
  //       JSONMetaDataURL: 'https://example.com/metadata.json',
  //       Symbol: 'TEST',
  //       Title: 'Test NFT',
  //       Description: 'Test NFT created by MCP test script',
  //     },
  //     'Mint a test NFT'
  //   )
  // );

  // ============================================================
  // SUMMARY
  // ============================================================
  console.log('\n' + '='.repeat(60));
  console.log('📊 Test Summary\n');

  const successful = testResults.filter((r) => r.success).length;
  const withApiErrors = testResults.filter((r) => r.hasError).length;
  const failed = testResults.filter((r) => !r.success && !r.hasError).length;
  const totalDuration = testResults.reduce((sum, r) => sum + r.duration, 0);

  console.log(`Total Tests: ${testResults.length}`);
  console.log(`✅ Successful: ${successful}`);
  if (withApiErrors > 0) {
    console.log(`⚠️  API Errors: ${withApiErrors}`);
  }
  console.log(`❌ Failed: ${failed}`);
  console.log(`⏱️  Total Duration: ${totalDuration}ms\n`);

  console.log('📋 Tested Write Endpoints:');
  testResults.forEach((result) => {
    const icon = result.success ? '✅' : result.hasError ? '⚠️' : '❌';
    console.log(`   ${icon} ${result.endpoint} (${result.duration}ms)`);
  });

  process.exit(failed > 0 ? 1 : 0);
}

runTests().catch((error) => {
  console.error('💥 Fatal error:', error);
  process.exit(1);
});
