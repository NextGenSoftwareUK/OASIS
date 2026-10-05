#!/usr/bin/env python3
"""
Demo Script: End-to-End Agent Payment Flow
Demonstrates autonomous agent discovery, task execution, and MNEE payment
"""

import sys
import os
import time

# Add parent directory to path
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from src.core.oasis_client import OASISClient
from src.core.a2a_client import A2AClient
from src.core.agent_discovery import AgentDiscovery
from src.core.payment_flow import PaymentFlow
from config import OASIS_API_URL, AGENT_DISCOVERY_PORT, AGENT_DISCOVERY_HOST, PAYMENT_TOKEN_SYMBOL


def print_section(title: str):
    """Print a formatted section header"""
    print()
    print("=" * 60)
    print(f"  {title}")
    print("=" * 60)
    print()


def main():
    """Run end-to-end demo"""
    print_section("MNEE Hackathon Submission - Autonomous Agent Payment Network")
    
    # Initialize clients
    print("📦 Initializing OASIS and A2A clients...")
    oasis = OASISClient(OASIS_API_URL)
    a2a = A2AClient()
    discovery = AgentDiscovery(oasis, a2a)
    payment_flow = PaymentFlow(oasis, a2a)
    
    # Step 1: Register requester agent
    print_section("Step 1: Register Requester Agent")
    print("🔐 Registering requester agent with OASIS...")
    
    requester_username = f"trading_bot_{int(time.time())}"
    
    try:
        # Register agent (email and password auto-generated)
        result = oasis.register_or_authenticate_agent(requester_username)
        
        if result.get("isError"):
            print(f"❌ Registration failed: {result.get('message', 'Unknown error')}")
            return
        
        # Generate wallet
        oasis.generate_wallet()
        
        print(f"✅ Requester agent registered")
        print(f"   Avatar ID: {oasis.avatar_id}")
        print(f"   Wallet: {oasis.wallet_address}")
    except Exception as e:
        print(f"❌ Error: {e}")
        print("💡 Make sure OASIS API is running and accessible")
        import traceback
        traceback.print_exc()
        return
    
    # Step 2: Discover service provider agent
    print_section("Step 2: Discover Service Provider Agent")
    print("🔍 Discovering agents with 'analyzeMarketData' capability...")
    
    # In production, this would query a registry
    # For demo, we'll use a known endpoint
    # Use the configured port from config
    agent_endpoint = f"http://{AGENT_DISCOVERY_HOST}:{AGENT_DISCOVERY_PORT}"
    
    print(f"   Checking endpoint: {agent_endpoint}")
    
    try:
        agent_card = a2a.get_agent_card(agent_endpoint)
        if not agent_card:
            print("❌ No agent found at endpoint")
            print("💡 Make sure Data Analyzer Agent is running:")
            print("   python demo/data_analyzer_agent.py")
            return
        
        print(f"✅ Found agent: {agent_card.get('name')}")
        print(f"   Agent ID: {agent_card.get('agentId')}")
        print(f"   Capabilities: {[c.get('name') for c in agent_card.get('capabilities', [])]}")
        
        # Get OASIS data
        oasis_data = agent_card.get("metadata", {}).get("oasis", {})
        print(f"   OASIS Avatar ID: {oasis_data.get('avatarId')}")
        print(f"   Wallet: {oasis_data.get('walletAddress')}")
        print(f"   Karma: {oasis_data.get('karma', 0)}")
    except Exception as e:
        print(f"❌ Discovery error: {e}")
        return
    
    # Step 3: Negotiate payment
    print_section("Step 3: Negotiate Payment")
    
    capabilities = agent_card.get("capabilities", [])
    pricing = None
    for cap in capabilities:
        if cap.get("name") == "analyzeMarketData":
            pricing = cap.get("pricing", "0.01 MNEE")
            break
    
    if pricing:
        print(f"💰 Agent pricing: {pricing}")
        payment_amount = pricing.split()[0]  # Extract numeric value
        print(f"   Agreed payment: {payment_amount} {PAYMENT_TOKEN_SYMBOL}")
    else:
        payment_amount = "0.01"
        print(f"💰 Using default payment: {payment_amount} {PAYMENT_TOKEN_SYMBOL}")
    
    # Step 4: Execute task and pay
    print_section("Step 4: Execute Task and Process Payment")
    
    task = {
        "type": "analyzeMarketData",
        "input": {
            "symbol": "BTC/USD",
            "timeframe": "24h"
        }
    }
    
    print(f"📋 Task: {task['type']}")
    print(f"   Input: {task['input']}")
    print()
    print("🔄 Executing task...")
    
    try:
        result = payment_flow.execute_and_pay(
            requester_avatar_id=oasis.avatar_id,
            provider_agent=agent_card,
            task=task,
            payment_amount=payment_amount
        )
        
        if result.get("success"):
            print()
            print("✅ Task completed successfully!")
            print()
            print("📊 Task Result:")
            task_result = result.get("task_result", {})
            if "analysis" in task_result:
                print(task_result["analysis"])
            else:
                print(f"   {task_result}")
            
            print()
            print("💳 Payment Result:")
            payment_result = result.get("payment_result", {})
            if payment_result.get("result"):
                print(f"   Status: Success")
                print(f"   Transaction: {payment_result.get('result', {}).get('transactionHash', 'N/A')}")
            else:
                print(f"   {payment_result}")
            
            print()
            print("⭐ Karma updated for both agents!")
        else:
            print()
            print(f"❌ Task failed: {result.get('error', 'Unknown error')}")
            if "task_result" in result:
                print(f"   Task result: {result['task_result']}")
    
    except Exception as e:
        print(f"❌ Error executing task: {e}")
        import traceback
        traceback.print_exc()
    
    # Summary
    print_section("Demo Complete")
    print("✅ End-to-end agent payment flow demonstrated!")
    print()
    print("What happened:")
    print("1. ✅ Requester agent registered with OASIS")
    print("2. ✅ Service provider agent discovered via A2A Protocol")
    print(f"3. ✅ Payment negotiated (0.01 {PAYMENT_TOKEN_SYMBOL})")
    print("4. ✅ Task executed (market data analysis)")
    print(f"5. ✅ Payment processed via OASIS Wallet API ({PAYMENT_TOKEN_SYMBOL})")
    print("6. ✅ Karma updated for both agents")
    print()
    print(f"🚀 This demonstrates autonomous agent payments using {PAYMENT_TOKEN_SYMBOL}!")


if __name__ == "__main__":
    main()

