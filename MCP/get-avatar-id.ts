#!/usr/bin/env node

/**
 * Script to get avatar ID for OASIS_ADMIN
 * Usage: npx tsx get-avatar-id.ts
 */

import { OASISClient } from './src/clients/oasisClient.js';
import { config } from './src/config.js';

async function getAvatarId() {
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
    
    // Extract token and avatar info - OASIS API returns nested structure
    const result = authResponse.result?.result || authResponse.result;
    const token = result?.jwtToken || authResponse.result?.token || authResponse.result?.Token || authResponse.token;
    const avatar = result || authResponse.result?.avatar || authResponse.result?.Avatar || authResponse.result;
    
    if (token) {
      client.setAuthToken(token);
      console.log('✅ Authentication successful');
      console.log(`   Token: ${token.substring(0, 30)}...`);
    }
    
    // Get avatar ID - check nested structure
    let avatarId: string | undefined;
    
    if (avatar) {
      avatarId = avatar.avatarId || avatar.AvatarId || avatar.id || avatar.Id;
    }
    
    // If we don't have it from auth response, try getting by username
    if (!avatarId) {
      console.log('📋 Getting avatar by username...');
      const avatarResponse = await client.getAvatarByUsername('OASIS_ADMIN');
      
      if (avatarResponse.isError) {
        console.error('❌ Failed to get avatar:', avatarResponse.message);
      } else {
        const avatarData = avatarResponse.result || avatarResponse;
        avatarId = avatarData.id || avatarData.Id || avatarData.avatarId || avatarData.AvatarId;
      }
    }
    
    if (avatarId) {
      console.log('\n✅ Avatar ID found!');
      console.log(`   Avatar ID: ${avatarId}`);
      console.log(`   Username: OASIS_ADMIN`);
      console.log('\n💡 To use in tests, set:');
      console.log(`   export TEST_AVATAR_ID="${avatarId}"`);
      console.log(`   # or add to .env file:`);
      console.log(`   echo "TEST_AVATAR_ID=${avatarId}" >> .env`);
      
      return avatarId;
    } else {
      console.error('❌ Could not find avatar ID');
      console.log('Full auth response:', JSON.stringify(authResponse, null, 2));
      process.exit(1);
    }
  } catch (error: any) {
    console.error('❌ Error:', error.message);
    console.error('Stack:', error.stack);
    process.exit(1);
  }
}

getAvatarId();
