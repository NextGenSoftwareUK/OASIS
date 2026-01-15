#!/usr/bin/env node

/**
 * Script to authenticate with OASIS avatar using MCP client
 * Usage: npx tsx authenticate-avatar.ts
 */

import { OASISClient } from './src/clients/oasisClient.js';

async function authenticate() {
  const client = new OASISClient();
  
  const username = 'OASIS_ADMIN';
  const password = 'Uppermall1!';
  
  try {
    console.log(`🔐 Authenticating as ${username}...`);
    
    // Authenticate
    const authResponse = await client.authenticateAvatar(username, password);
    
    // Check for error
    if (authResponse.isError) {
      console.error('❌ Authentication failed:', authResponse.message);
      process.exit(1);
    }
    
    // Extract token and avatar info - OASIS API returns nested structure
    const result = authResponse.result?.result || authResponse.result;
    const token = result?.jwtToken || result?.token || result?.Token || authResponse.result?.jwtToken || authResponse.result?.token;
    const avatar = result || authResponse.result?.avatar || authResponse.result?.Avatar || authResponse.result;
    
    if (token) {
      client.setAuthToken(token);
      console.log('✅ Authentication successful!');
      console.log(`   Token: ${token.substring(0, 50)}...`);
    } else {
      console.warn('⚠️  No token found in response');
    }
    
    // Get avatar ID - check nested structure
    let avatarId: string | undefined;
    
    if (avatar) {
      avatarId = avatar.avatarId || avatar.AvatarId || avatar.id || avatar.Id;
    }
    
    // If we don't have it from auth response, try getting by username
    if (!avatarId) {
      console.log('📋 Getting avatar by username...');
      const avatarResponse = await client.getAvatarByUsername(username);
      
      if (avatarResponse.isError) {
        console.error('❌ Failed to get avatar:', avatarResponse.message);
      } else {
        const avatarData = avatarResponse.result || avatarResponse;
        avatarId = avatarData.id || avatarData.Id || avatarData.avatarId || avatarData.AvatarId;
      }
    }
    
    if (avatarId) {
      console.log(`   Avatar ID: ${avatarId}`);
    }
    
    // Display full response for debugging
    console.log('\n📄 Full authentication response:');
    console.log(JSON.stringify(authResponse, null, 2));
    
    console.log('\n✅ Authentication complete! Token has been set for subsequent requests.');
    
  } catch (error: any) {
    console.error('❌ Error during authentication:', error.message);
    if (error.response) {
      console.error('   Response status:', error.response.status);
      console.error('   Response data:', JSON.stringify(error.response.data, null, 2));
    }
    process.exit(1);
  }
}

authenticate();
