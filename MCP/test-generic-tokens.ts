#!/usr/bin/env node

/**
 * Script to authenticate OASIS_ADMIN and test generic token endpoints
 * Usage: npx tsx test-generic-tokens.ts
 */

import { OASISClient } from './src/clients/oasisClient.js';
import axios from 'axios';
import https from 'https';

const API_BASE_URL = process.env.API_BASE_URL || 'http://localhost:5003';
const MNEE_CONTRACT = '0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF';
const USDC_CONTRACT = '0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48';
const SPENDER_ADDRESS = '0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb';

// Configure axios for self-signed certificates
const httpsAgent = new https.Agent({
  rejectUnauthorized: false
});

async function authenticateAndTest() {
  const client = new OASISClient();
  
  try {
    console.log('🔐 Authenticating as OASIS_ADMIN...');
    
    // Authenticate
    const authResponse = await client.authenticateAvatar('OASIS_ADMIN', 'Uppermall1!');
    
    // Check for error
    if (authResponse.isError) {
      console.error('❌ Authentication failed:', authResponse.message);
      process.exit(1);
    }
    
    // Extract token - OASIS API returns nested structure
    const result = authResponse.result?.result || authResponse.result;
    const token = result?.jwtToken || authResponse.result?.token || authResponse.result?.Token;
    
    if (!token) {
      console.error('❌ No token received in response');
      console.log('Response:', JSON.stringify(authResponse, null, 2));
      process.exit(1);
    }
    
    console.log('✅ Authentication successful');
    console.log(`   Token: ${token.substring(0, 30)}...`);
    
    // Get avatar ID
    let avatarId: string | undefined;
    const avatar = result || authResponse.result?.avatar || authResponse.result?.Avatar;
    if (avatar) {
      avatarId = avatar.avatarId || avatar.AvatarId || avatar.id || avatar.Id;
    }
    
    if (!avatarId) {
      console.log('📋 Getting avatar by username...');
      const avatarResponse = await client.getAvatarByUsername('OASIS_ADMIN');
      if (!avatarResponse.isError) {
        const avatarData = avatarResponse.result || avatarResponse;
        avatarId = avatarData.id || avatarData.Id || avatarData.avatarId || avatarData.AvatarId;
      }
    }
    
    if (avatarId) {
      console.log(`   Avatar ID: ${avatarId}`);
    }
    
    console.log('\n🧪 Testing Generic Token Endpoints...\n');
    
    const headers = {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    };
    
    let passed = 0;
    let failed = 0;
    
    // Test 1: Get MNEE Token Balance
    console.log('Test 1: Get MNEE Token Balance');
    try {
      const params = new URLSearchParams({
        tokenContractAddress: MNEE_CONTRACT,
        providerType: 'EthereumOASIS'
      });
      if (avatarId) params.append('avatarId', avatarId);
      
      const response = await axios.get(
        `${API_BASE_URL}/api/wallet/token/balance?${params.toString()}`,
        { headers, httpsAgent, validateStatus: () => true }
      );
      
      if (response.data?.isError === false || response.data?.result !== undefined) {
        console.log('   ✅ Success');
        console.log(`   Balance: ${response.data.result || 'N/A'}`);
        passed++;
      } else {
        console.log('   ❌ Failed:', response.data?.message || response.statusText);
        console.log('   Response:', JSON.stringify(response.data, null, 2).substring(0, 200));
        failed++;
      }
    } catch (error: any) {
      console.log('   ❌ Error:', error.message);
      failed++;
    }
    console.log('');
    
    // Test 2: Get USDC Token Balance
    console.log('Test 2: Get USDC Token Balance (Generic)');
    try {
      const params = new URLSearchParams({
        tokenContractAddress: USDC_CONTRACT,
        providerType: 'EthereumOASIS'
      });
      if (avatarId) params.append('avatarId', avatarId);
      
      const response = await axios.get(
        `${API_BASE_URL}/api/wallet/token/balance?${params.toString()}`,
        { headers, httpsAgent, validateStatus: () => true }
      );
      
      if (response.data?.isError === false || response.data?.result !== undefined) {
        console.log('   ✅ Success');
        console.log(`   Balance: ${response.data.result || 'N/A'}`);
        passed++;
      } else {
        console.log('   ❌ Failed:', response.data?.message || response.statusText);
        console.log('   Response:', JSON.stringify(response.data, null, 2).substring(0, 200));
        failed++;
      }
    } catch (error: any) {
      console.log('   ❌ Error:', error.message);
      failed++;
    }
    console.log('');
    
    // Test 3: Get MNEE Token Info
    console.log('Test 3: Get MNEE Token Info');
    try {
      const params = new URLSearchParams({
        tokenContractAddress: MNEE_CONTRACT,
        providerType: 'EthereumOASIS'
      });
      
      const response = await axios.get(
        `${API_BASE_URL}/api/wallet/token/info?${params.toString()}`,
        { headers, httpsAgent, validateStatus: () => true }
      );
      
      if (response.data?.isError === false || response.data?.result !== undefined) {
        console.log('   ✅ Success');
        const info = response.data.result;
        if (info) {
          console.log(`   Name: ${info.name || 'N/A'}`);
          console.log(`   Symbol: ${info.symbol || 'N/A'}`);
          console.log(`   Decimals: ${info.decimals || 'N/A'}`);
        }
        passed++;
      } else {
        console.log('   ❌ Failed:', response.data?.message || response.statusText);
        console.log('   Response:', JSON.stringify(response.data, null, 2).substring(0, 200));
        failed++;
      }
    } catch (error: any) {
      console.log('   ❌ Error:', error.message);
      failed++;
    }
    console.log('');
    
    // Test 4: Get USDC Token Info
    console.log('Test 4: Get USDC Token Info (Generic)');
    try {
      const params = new URLSearchParams({
        tokenContractAddress: USDC_CONTRACT,
        providerType: 'EthereumOASIS'
      });
      
      const response = await axios.get(
        `${API_BASE_URL}/api/wallet/token/info?${params.toString()}`,
        { headers, httpsAgent, validateStatus: () => true }
      );
      
      if (response.data?.isError === false || response.data?.result !== undefined) {
        console.log('   ✅ Success');
        const info = response.data.result;
        if (info) {
          console.log(`   Name: ${info.name || 'N/A'}`);
          console.log(`   Symbol: ${info.symbol || 'N/A'}`);
          console.log(`   Decimals: ${info.decimals || 'N/A'}`);
        }
        passed++;
      } else {
        console.log('   ❌ Failed:', response.data?.message || response.statusText);
        console.log('   Response:', JSON.stringify(response.data, null, 2).substring(0, 200));
        failed++;
      }
    } catch (error: any) {
      console.log('   ❌ Error:', error.message);
      failed++;
    }
    console.log('');
    
    // Test 5: Get Token Allowance
    console.log('Test 5: Get Token Allowance');
    try {
      const params = new URLSearchParams({
        tokenContractAddress: MNEE_CONTRACT,
        spenderAddress: SPENDER_ADDRESS,
        providerType: 'EthereumOASIS'
      });
      if (avatarId) params.append('avatarId', avatarId);
      
      const response = await axios.get(
        `${API_BASE_URL}/api/wallet/token/allowance?${params.toString()}`,
        { headers, httpsAgent, validateStatus: () => true }
      );
      
      if (response.data?.isError === false || response.data?.result !== undefined) {
        console.log('   ✅ Success');
        console.log(`   Allowance: ${response.data.result || 'N/A'}`);
        passed++;
      } else {
        console.log('   ❌ Failed:', response.data?.message || response.statusText);
        console.log('   Response:', JSON.stringify(response.data, null, 2).substring(0, 200));
        failed++;
      }
    } catch (error: any) {
      console.log('   ❌ Error:', error.message);
      failed++;
    }
    console.log('');
    
    // Summary
    console.log('========================================');
    console.log('Test Summary');
    console.log('========================================');
    console.log(`✅ Passed: ${passed}`);
    console.log(`❌ Failed: ${failed}`);
    console.log('');
    
    if (failed === 0) {
      console.log('🎉 All tests passed!');
      process.exit(0);
    } else {
      console.log('⚠️  Some tests failed. Check the output above.');
      process.exit(1);
    }
    
  } catch (error: any) {
    console.error('❌ Unexpected error:', error.message);
    console.error(error.stack);
    process.exit(1);
  }
}

authenticateAndTest();
