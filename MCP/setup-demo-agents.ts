#!/usr/bin/env node

/**
 * Setup Demo Agents with Ethereum Wallets
 * 
 * Creates 2 agent avatars and links Ethereum wallets for both.
 * Perfect for testing agent-to-agent MNEE payments.
 * 
 * Usage:
 *   npx tsx setup-demo-agents.ts
 * 
 * Or with custom credentials:
 *   AGENT_A_USERNAME=agent_a AGENT_A_PASSWORD=pass_a \
 *   AGENT_B_USERNAME=agent_b AGENT_B_PASSWORD=pass_b \
 *   npx tsx setup-demo-agents.ts
 */

import { OASISClient } from './src/clients/oasisClient.js';
import axios from 'axios';
import https from 'https';

const API_BASE_URL = process.env.API_BASE_URL || 'http://localhost:5003';

// Configure axios for self-signed certificates
const httpsAgent = new https.Agent({
  rejectUnauthorized: false
});

interface AgentSetup {
  username: string;
  password: string;
  email: string;
  avatarId?: string;
  token?: string;
  walletAddress?: string;
  walletId?: string;
}

async function authenticateAdmin(client: OASISClient): Promise<string | null> {
  console.log('🔐 Authenticating as OASIS_ADMIN...');
  
  const adminUsername = process.env.OASIS_ADMIN_USERNAME || 'OASIS_ADMIN';
  const adminPassword = process.env.OASIS_ADMIN_PASSWORD || 'Uppermall1!';
  
  const authResponse = await client.authenticateAvatar(adminUsername, adminPassword);
  
  if (authResponse.isError) {
    console.error('❌ Admin authentication failed:', authResponse.message);
    return null;
  }
  
  const result = authResponse.result?.result || authResponse.result;
  const token = result?.jwtToken || result?.token || result?.Token;
  
  if (!token) {
    console.error('❌ Could not extract admin token');
    return null;
  }
  
  console.log('✅ Admin authenticated');
  return token;
}

async function registerAgent(
  token: string,
  agent: AgentSetup
): Promise<{ avatarId: string } | null> {
  console.log(`📝 Registering agent: ${agent.username}...`);
  
  try {
    const response = await axios.post(
      `${API_BASE_URL}/api/avatar/register`,
      {
        username: agent.username,
        email: agent.email,
        password: agent.password,
        confirmPassword: agent.password,
        firstName: agent.username.split('_')[0] || 'Agent',
        lastName: agent.username.split('_')[1] || 'User',
        avatarType: 'Agent',
        acceptTerms: true
      },
      {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        httpsAgent,
        validateStatus: () => true,
        timeout: 30000
      }
    );
    
    if (response.data?.isError === false || response.status === 200) {
      const result = response.data?.result?.result || response.data?.result || response.data;
      const avatarId = result?.id || result?.Id || result?.avatarId || result?.AvatarId;
      
      if (avatarId) {
        console.log(`✅ Agent ${agent.username} registered (ID: ${avatarId.substring(0, 8)}...)`);
        return { avatarId };
      }
    }
    
    // Check if user already exists
    if (response.data?.message?.toLowerCase().includes('already exists') ||
        response.data?.message?.toLowerCase().includes('already in use')) {
      console.log(`⚠️  Agent ${agent.username} already exists, attempting to authenticate...`);
      
      // Try to authenticate to get avatar ID
      const client = new OASISClient();
      const authResponse = await client.authenticateAvatar(agent.username, agent.password);
      
      if (!authResponse.isError) {
        const authResult = authResponse.result?.result || authResponse.result;
        const authAvatar = authResult || authResponse.result?.avatar || authResponse.result?.Avatar;
        const avatarId = authAvatar?.avatarId || authAvatar?.AvatarId || authAvatar?.id || authAvatar?.Id;
        
        if (avatarId) {
          console.log(`✅ Found existing agent ${agent.username} (ID: ${avatarId.substring(0, 8)}...)`);
          return { avatarId };
        }
      }
    }
    
    console.error(`❌ Failed to register agent ${agent.username}:`, response.data?.message || 'Unknown error');
    console.error(`   Response:`, JSON.stringify(response.data, null, 2).substring(0, 300));
    return null;
  } catch (error: any) {
    console.error(`❌ Error registering agent ${agent.username}:`, error.message);
    return null;
  }
}

async function generateEthereumKeypair(token: string): Promise<{ privateKey: string; address: string }> {
  try {
    const response = await axios.post(
      `${API_BASE_URL}/api/keys/generate_keypair_with_wallet_address_for_provider/EthereumOASIS`,
      {},
      {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        httpsAgent,
        validateStatus: () => true,
        timeout: 30000
      }
    );
    
    if (response.data?.isError === false || response.status === 200) {
      const result = response.data?.result?.result || response.data?.result || response.data;
      const privateKey = result?.privateKey;
      const address = result?.walletAddressLegacy || result?.publicKey || result?.walletAddress;
      
      if (privateKey && address) {
        return { privateKey, address };
      }
    }
    
    throw new Error(response.data?.message || 'Failed to generate keypair');
  } catch (error: any) {
    throw new Error(`Error generating keypair: ${error.message}`);
  }
}

async function linkPublicKey(
  token: string,
  avatarId: string,
  address: string
): Promise<{ walletId: string } | null> {
  console.log(`   Linking public key (address: ${address.substring(0, 10)}...)...`);
  
  try {
    const response = await axios.post(
      `${API_BASE_URL}/api/keys/link_provider_public_key_to_avatar_by_id`,
      {
        AvatarID: avatarId,
        ProviderType: 'EthereumOASIS',
        ProviderKey: address,
        WalletAddress: address
      },
      {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        httpsAgent,
        validateStatus: () => true,
        timeout: 30000
      }
    );
    
    if (response.data?.isError === false || response.status === 200) {
      const result = response.data?.result || response.data;
      const walletId = result?.walletId || result?.id || result?.wallet?.id || result?.wallet?.walletId;
      
      if (walletId) {
        console.log(`   ✅ Public key linked (Wallet ID: ${walletId.substring(0, 8)}...)`);
        return { walletId };
      }
    }
    
    // Check if wallet already exists
    if (response.data?.message?.toLowerCase().includes('already') ||
        response.data?.message?.toLowerCase().includes('exists')) {
      console.log(`   ⚠️  Wallet already exists for this address`);
      // Try to extract wallet ID from error message or response
      const result = response.data?.result || response.data;
      const walletId = result?.walletId || result?.id;
      if (walletId) {
        return { walletId };
      }
    }
    
    console.error(`   ❌ Failed to link public key:`, response.data?.message || 'Unknown error');
    return null;
  } catch (error: any) {
    console.error(`   ❌ Error linking public key:`, error.message);
    return null;
  }
}

async function linkPrivateKey(
  token: string,
  walletId: string,
  avatarId: string,
  privateKey: string
): Promise<boolean> {
  console.log(`   Linking private key...`);
  
  try {
    const response = await axios.post(
      `${API_BASE_URL}/api/keys/link_provider_private_key_to_avatar_by_id`,
      {
        WalletId: walletId,
        AvatarID: avatarId,
        ProviderType: 'EthereumOASIS',
        ProviderKey: privateKey
      },
      {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        httpsAgent,
        validateStatus: () => true,
        timeout: 30000
      }
    );
    
    if (response.data?.isError === false || response.status === 200) {
      console.log(`   ✅ Private key linked`);
      return true;
    }
    
    // Check if private key already linked
    if (response.data?.message?.toLowerCase().includes('already') ||
        response.data?.message?.toLowerCase().includes('exists')) {
      console.log(`   ⚠️  Private key already linked`);
      return true;
    }
    
    console.error(`   ❌ Failed to link private key:`, response.data?.message || 'Unknown error');
    return false;
  } catch (error: any) {
    console.error(`   ❌ Error linking private key:`, error.message);
    return false;
  }
}

async function createEthereumWallet(
  token: string,
  avatarId: string
): Promise<{ walletAddress: string; walletId: string } | null> {
  console.log(`   Creating Ethereum wallet...`);
  
  // Generate keypair using API
  const { privateKey, address } = await generateEthereumKeypair(token);
  console.log(`   ✅ Keypair generated (address: ${address.substring(0, 10)}...)`);
  
  // Link public key FIRST
  const publicKeyResult = await linkPublicKey(token, avatarId, address);
  if (!publicKeyResult) {
    return null;
  }
  
  // Link private key SECOND
  const privateKeyLinked = await linkPrivateKey(token, publicKeyResult.walletId, avatarId, privateKey);
  if (!privateKeyLinked) {
    return null;
  }
  
  return {
    walletAddress: address,
    walletId: publicKeyResult.walletId
  };
}

async function setupAgent(
  adminToken: string,
  agent: AgentSetup
): Promise<AgentSetup | null> {
  console.log(`\n${'='.repeat(60)}`);
  console.log(`Setting up: ${agent.username}`);
  console.log('='.repeat(60));
  
  // Step 1: Register agent
  const registerResult = await registerAgent(adminToken, agent);
  if (!registerResult) {
    return null;
  }
  
  agent.avatarId = registerResult.avatarId;
  
  // Step 2: Authenticate agent to get token
  console.log(`🔐 Authenticating ${agent.username}...`);
  const client = new OASISClient();
  const authResponse = await client.authenticateAvatar(agent.username, agent.password);
  
  if (authResponse.isError) {
    console.error(`❌ Authentication failed for ${agent.username}`);
    return null;
  }
  
  const authResult = authResponse.result?.result || authResponse.result;
  agent.token = authResult?.jwtToken || authResult?.token || authResult?.Token;
  
  if (!agent.token) {
    console.error(`❌ Could not get token for ${agent.username}`);
    return null;
  }
  
  console.log(`✅ ${agent.username} authenticated`);
  
  // Step 3: Create Ethereum wallet (use agent's token, not admin token)
  console.log(`💰 Creating Ethereum wallet for ${agent.username}...`);
  const walletResult = await createEthereumWallet(agent.token!, agent.avatarId);
  
  if (!walletResult) {
    console.error(`❌ Failed to create wallet for ${agent.username}`);
    return null;
  }
  
  agent.walletAddress = walletResult.walletAddress;
  agent.walletId = walletResult.walletId;
  
  console.log(`✅ Ethereum wallet created: ${agent.walletAddress}`);
  
  return agent;
}

async function main() {
  console.log('='.repeat(60));
  console.log('Demo Agent Setup');
  console.log('Creating 2 agent avatars with Ethereum wallets');
  console.log('='.repeat(60));
  console.log();
  
  // Initialize client
  const client = new OASISClient();
  
  // Authenticate as admin
  const adminToken = await authenticateAdmin(client);
  if (!adminToken) {
    console.error('❌ Cannot proceed without admin authentication');
    process.exit(1);
  }
  console.log();
  
  // Define agents
  const agents: AgentSetup[] = [
    {
      username: process.env.AGENT_A_USERNAME || 'demo_agent_a',
      password: process.env.AGENT_A_PASSWORD || 'DemoAgentA123!',
      email: process.env.AGENT_A_EMAIL || 'demo_agent_a@oasis.demo'
    },
    {
      username: process.env.AGENT_B_USERNAME || 'demo_agent_b',
      password: process.env.AGENT_B_PASSWORD || 'DemoAgentB123!',
      email: process.env.AGENT_B_EMAIL || 'demo_agent_b@oasis.demo'
    }
  ];
  
  // Setup each agent
  const setupResults: AgentSetup[] = [];
  
  for (const agent of agents) {
    const result = await setupAgent(adminToken, agent);
    if (result) {
      setupResults.push(result);
    }
  }
  
  // Summary
  console.log('\n' + '='.repeat(60));
  console.log('Setup Summary');
  console.log('='.repeat(60));
  
  if (setupResults.length === 2) {
    console.log('✅ Both agents created successfully!');
    console.log();
    
    for (const agent of setupResults) {
      console.log(`Agent: ${agent.username}`);
      console.log(`  Avatar ID: ${agent.avatarId}`);
      console.log(`  Wallet Address: ${agent.walletAddress}`);
      console.log(`  Email: ${agent.email}`);
      console.log();
    }
    
    console.log('='.repeat(60));
    console.log('Next Steps:');
    console.log('='.repeat(60));
    console.log('1. Fund Agent A with MNEE tokens for the demo');
    console.log('2. Run the payment demo:');
    console.log(`   cd MCP`);
    console.log(`   AGENT_A_USERNAME="${setupResults[0].username}" \\`);
    console.log(`   AGENT_A_PASSWORD="${setupResults[0].password}" \\`);
    console.log(`   AGENT_B_USERNAME="${setupResults[1].username}" \\`);
    console.log(`   AGENT_B_PASSWORD="${setupResults[1].password}" \\`);
    console.log(`   npx tsx demo-agent-mnee-payment.ts`);
    console.log();
  } else {
    console.log(`⚠️  Only ${setupResults.length} of 2 agents were set up successfully`);
    process.exit(1);
  }
}

main().catch(error => {
  console.error('❌ Unexpected error:', error);
  process.exit(1);
});
