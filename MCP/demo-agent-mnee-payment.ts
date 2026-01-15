#!/usr/bin/env node

/**
 * Demo: Agent-to-Agent MNEE Payment
 * 
 * Demonstrates autonomous agent-to-agent payments using MNEE stablecoin.
 * 
 * Usage:
 *   npx tsx demo-agent-mnee-payment.ts <agentA_username> <agentA_password> <agentB_username> <agentB_password>
 * 
 * Or set environment variables:
 *   AGENT_A_USERNAME=agent_a AGENT_A_PASSWORD=pass_a AGENT_B_USERNAME=agent_b AGENT_B_PASSWORD=pass_b npx tsx demo-agent-mnee-payment.ts
 */

import { OASISClient } from './src/clients/oasisClient.js';
import axios from 'axios';
import https from 'https';

const API_BASE_URL = process.env.API_BASE_URL || 'http://localhost:5003';
const PAYMENT_AMOUNT = parseFloat(process.env.PAYMENT_AMOUNT || '10.5');

// Configure axios for self-signed certificates
const httpsAgent = new https.Agent({
  rejectUnauthorized: false
});

interface PaymentResult {
  messageId?: string;
  transactionHash?: string;
  payload?: any;
}

async function authenticateAgent(client: OASISClient, username: string, password: string): Promise<{ token: string; avatarId: string } | null> {
  console.log(`🔐 Authenticating ${username}...`);
  
  const authResponse = await client.authenticateAvatar(username, password);
  
  if (authResponse.isError) {
    console.error(`❌ Authentication failed for ${username}:`, authResponse.message);
    return null;
  }
  
  const result = authResponse.result?.result || authResponse.result;
  const token = result?.jwtToken || result?.token || result?.Token;
  const avatar = result || authResponse.result?.avatar || authResponse.result?.Avatar;
  const avatarId = avatar?.avatarId || avatar?.AvatarId || avatar?.id || avatar?.Id;
  
  if (!token || !avatarId) {
    console.error(`❌ Could not extract token or avatar ID for ${username}`);
    return null;
  }
  
  console.log(`✅ ${username} authenticated (ID: ${avatarId.substring(0, 8)}...)`);
  return { token, avatarId };
}

async function getMNEEBalance(token: string): Promise<number | null> {
  try {
    const response = await axios.get(
      `${API_BASE_URL}/api/a2a/mnee/balance`,
      { 
        headers: { 'Authorization': `Bearer ${token}` },
        httpsAgent,
        validateStatus: () => true
      }
    );
    
    if (response.data?.isError === false) {
      return parseFloat(response.data.result || 0);
    }
    return null;
  } catch (error: any) {
    console.error(`❌ Error getting balance:`, error.message);
    return null;
  }
}

async function sendMNEEPayment(fromToken: string, toAgentId: string, amount: number, description?: string): Promise<PaymentResult | null> {
  try {
    const response = await axios.post(
      `${API_BASE_URL}/api/a2a/mnee/payment`,
      {
        toAgentId,
        amount,
        description: description || `Payment for agent service`,
        autoExecute: true
      },
      {
        headers: {
          'Authorization': `Bearer ${fromToken}`,
          'Content-Type': 'application/json'
        },
        httpsAgent,
        validateStatus: () => true,
        timeout: 60000
      }
    );
    
    if (response.data?.isError === false) {
      return response.data.result;
    } else {
      console.error(`❌ Payment failed:`, response.data?.message || 'Unknown error');
      console.error(`   Response:`, JSON.stringify(response.data, null, 2).substring(0, 300));
      return null;
    }
  } catch (error: any) {
    console.error(`❌ Error sending payment:`, error.message);
    if (error.response) {
      console.error(`   Response:`, JSON.stringify(error.response.data, null, 2).substring(0, 300));
    }
    return null;
  }
}

async function main() {
  console.log('='.repeat(60));
  console.log('A2A Protocol MNEE Payment Demo');
  console.log('='.repeat(60));
  console.log();
  
  // Get agent credentials
  const agentAUsername = process.env.AGENT_A_USERNAME || process.argv[2] || 'agent_a';
  const agentAPassword = process.env.AGENT_A_PASSWORD || process.argv[3] || 'password_a';
  const agentBUsername = process.env.AGENT_B_USERNAME || process.argv[4] || 'agent_b';
  const agentBPassword = process.env.AGENT_B_PASSWORD || process.argv[5] || 'password_b';
  
  if (!process.env.AGENT_A_USERNAME && process.argv.length < 6) {
    console.log('Usage:');
    console.log('  npx tsx demo-agent-mnee-payment.ts <agentA_user> <agentA_pass> <agentB_user> <agentB_pass>');
    console.log();
    console.log('Or set environment variables:');
    console.log('  AGENT_A_USERNAME=... AGENT_A_PASSWORD=... AGENT_B_USERNAME=... AGENT_B_PASSWORD=... npx tsx demo-agent-mnee-payment.ts');
    console.log();
    console.log('Using default credentials (will likely fail - please provide real agent credentials)');
    console.log();
  }
  
  const client = new OASISClient();
  
  // Step 1: Authenticate Agent A
  console.log('Step 1: Authenticating Agent A...');
  const agentA = await authenticateAgent(client, agentAUsername, agentAPassword);
  if (!agentA) {
    process.exit(1);
  }
  console.log();
  
  // Step 2: Authenticate Agent B
  console.log('Step 2: Authenticating Agent B...');
  const agentB = await authenticateAgent(client, agentBUsername, agentBPassword);
  if (!agentB) {
    process.exit(1);
  }
  console.log();
  
  // Step 3: Check initial balances
  console.log('Step 3: Checking initial MNEE balances...');
  const balanceA = await getMNEEBalance(agentA.token);
  const balanceB = await getMNEEBalance(agentB.token);
  
  if (balanceA !== null) {
    console.log(`   Agent A balance: ${balanceA} MNEE`);
  } else {
    console.log('   ⚠️  Could not get Agent A balance');
  }
  
  if (balanceB !== null) {
    console.log(`   Agent B balance: ${balanceB} MNEE`);
  } else {
    console.log('   ⚠️  Could not get Agent B balance');
  }
  console.log();
  
  // Step 4: Send payment
  console.log(`Step 4: Sending ${PAYMENT_AMOUNT} MNEE from Agent A to Agent B...`);
  const paymentResult = await sendMNEEPayment(
    agentA.token,
    agentB.avatarId,
    PAYMENT_AMOUNT,
    'Payment for data analysis service'
  );
  
  if (paymentResult) {
    console.log('✅ Payment sent successfully!');
    if (paymentResult.messageId) {
      console.log(`   Message ID: ${paymentResult.messageId}`);
    }
    const txHash = paymentResult.transactionHash || paymentResult.payload?.transactionHash;
    if (txHash) {
      console.log(`   Transaction Hash: ${txHash}`);
    }
    const status = paymentResult.payload?.paymentStatus || 'unknown';
    console.log(`   Payment Status: ${status}`);
  } else {
    console.log('❌ Payment failed');
    process.exit(1);
  }
  console.log();
  
  // Step 5: Wait for transaction
  console.log('Step 5: Waiting for transaction confirmation...');
  await new Promise(resolve => setTimeout(resolve, 3000));
  console.log();
  
  // Step 6: Check updated balances
  console.log('Step 6: Checking updated MNEE balances...');
  const newBalanceA = await getMNEEBalance(agentA.token);
  const newBalanceB = await getMNEEBalance(agentB.token);
  
  if (newBalanceA !== null) {
    const diff = balanceA !== null ? newBalanceA - balanceA : 0;
    console.log(`   Agent A balance: ${newBalanceA} MNEE${diff !== 0 ? ` (change: ${diff > 0 ? '+' : ''}${diff.toFixed(2)} MNEE)` : ''}`);
  } else {
    console.log('   ⚠️  Could not get Agent A balance');
  }
  
  if (newBalanceB !== null) {
    const diff = balanceB !== null ? newBalanceB - balanceB : 0;
    console.log(`   Agent B balance: ${newBalanceB} MNEE${diff !== 0 ? ` (change: ${diff > 0 ? '+' : ''}${diff.toFixed(2)} MNEE)` : ''}`);
  } else {
    console.log('   ⚠️  Could not get Agent B balance');
  }
  console.log();
  
  // Summary
  console.log('='.repeat(60));
  console.log('Demo Summary');
  console.log('='.repeat(60));
  if (paymentResult) {
    console.log('✅ MNEE payment completed successfully!');
    console.log(`   Amount: ${PAYMENT_AMOUNT} MNEE`);
    console.log(`   From: Agent A (${agentAUsername})`);
    console.log(`   To: Agent B (${agentBUsername})`);
    const txHash = paymentResult.transactionHash || paymentResult.payload?.transactionHash;
    if (txHash) {
      console.log(`   Transaction: ${txHash}`);
    }
  } else {
    console.log('❌ Payment demo failed');
    process.exit(1);
  }
  console.log();
}

main().catch(error => {
  console.error('❌ Unexpected error:', error);
  process.exit(1);
});
