/**
 * Quick authentication test script
 * Tests all available OASIS API endpoints
 */

const endpoints = [
  'http://api.oasisweb4.com',
  'http://localhost:5003',
  'http://localhost:5004',
];

const credentials = {
  username: 'OASIS_ADMIN',
  password: 'Uppermall1!',
};

async function testAuth(endpoint) {
  console.log(`\n🔍 Testing: ${endpoint}`);
  
  const authEndpoints = [
    '/api/avatar/authenticate',
    '/api/auth/login',
  ];
  
  for (const authPath of authEndpoints) {
    try {
      const response = await fetch(`${endpoint}${authPath}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(credentials),
      });
      
      const data = await response.text();
      let jsonData;
      try {
        jsonData = JSON.parse(data);
      } catch (e) {
        console.log(`  ❌ ${authPath}: Invalid JSON response`);
        continue;
      }
      
      // Try to extract token
      const token = 
        jsonData?.result?.result?.jwtToken ||
        jsonData?.result?.jwtToken ||
        jsonData?.jwtToken ||
        jsonData?.token;
      
      const avatarId =
        jsonData?.result?.result?.avatarId ||
        jsonData?.result?.result?.id ||
        jsonData?.result?.avatarId ||
        jsonData?.avatarId ||
        jsonData?.avatar?.id;
      
      if (token) {
        console.log(`  ✅ ${authPath}: SUCCESS`);
        console.log(`     Token: ${token.substring(0, 50)}...`);
        if (avatarId) {
          console.log(`     Avatar ID: ${avatarId}`);
        }
        return { endpoint, authPath, token, avatarId, data: jsonData };
      } else {
        console.log(`  ⚠️  ${authPath}: No token found`);
        if (jsonData.message || jsonData.error) {
          console.log(`     Message: ${jsonData.message || jsonData.error}`);
        }
      }
    } catch (error) {
      console.log(`  ❌ ${authPath}: ${error.message}`);
    }
  }
  
  return null;
}

async function main() {
  console.log('🔐 Testing OASIS Authentication...\n');
  
  for (const endpoint of endpoints) {
    const result = await testAuth(endpoint);
    if (result) {
      console.log(`\n✅ Working endpoint found: ${result.endpoint}${result.authPath}`);
      console.log(`\n📋 Summary:`);
      console.log(`   Endpoint: ${result.endpoint}`);
      console.log(`   Auth Path: ${result.authPath}`);
      console.log(`   Token: ${result.token.substring(0, 50)}...`);
      console.log(`   Avatar ID: ${result.avatarId || 'Not found'}`);
      return;
    }
  }
  
  console.log('\n❌ No working authentication endpoint found');
}

main().catch(console.error);












