#!/usr/bin/env python3
"""
Quick test script to verify agent registration works
Tests: create avatar, authenticate, create wallet
"""

import sys
import os

# Add parent directory to path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from src.core.oasis_client import OASISClient
from config import OASIS_API_URL

def main():
    """Test agent registration flow"""
    print("=" * 60)
    print("Testing Agent Registration")
    print("=" * 60)
    print()
    
    # Initialize OASIS client
    oasis = OASISClient(OASIS_API_URL)
    
    # Test username
    username = f"test_agent_{os.getpid()}"
    
    print(f"📝 Test Agent: {username}")
    print(f"🌐 OASIS API: {OASIS_API_URL}")
    print()
    
    # Step 1: Register agent (email and password auto-generated)
    print("Step 1: Register Agent")
    print("-" * 60)
    try:
        result = oasis.register_or_authenticate_agent(username)
        
        if result.get("isError"):
            print(f"❌ Registration failed: {result.get('message', 'Unknown error')}")
            print(f"   Full response: {result}")
            return
        
        print(f"✅ Agent registered successfully")
        print(f"   Avatar ID: {oasis.avatar_id}")
        print(f"   Token: {'Yes' if oasis.token else 'No'}")
        
        # Debug: Print full response structure
        if not oasis.avatar_id:
            print(f"⚠️  Debug - Full response structure:")
            import json
            print(json.dumps(result, indent=2, default=str))
        
    except Exception as e:
        print(f"❌ Error: {e}")
        import traceback
        traceback.print_exc()
        return
    
    print()
    
    # Step 2: Generate wallet
    print("Step 2: Generate Wallet")
    print("-" * 60)
    try:
        result = oasis.generate_wallet()
        
        if result.get("isError"):
            print(f"⚠️  Wallet generation warning: {result.get('message', 'Unknown error')}")
        else:
            print(f"✅ Wallet generated")
            print(f"   Wallet Address: {oasis.wallet_address}")
        
    except Exception as e:
        print(f"❌ Error: {e}")
        import traceback
        traceback.print_exc()
        return
    
    print()
    
    # Step 3: Check wallet balance (after SOL request)
    print("Step 3: Check Wallet Balance")
    print("-" * 60)
    
    # Wait a moment for SOL to arrive
    if oasis.wallet_address:
        print("⏳ Waiting 3 seconds for SOL to arrive...")
        import time
        time.sleep(3)
    
    try:
        # Try to get balance via Solana RPC
        if oasis.wallet_address:
            print(f"   Checking balance for: {oasis.wallet_address}")
            # Use Solana RPC to check balance
            import requests
            rpc_url = "https://api.devnet.solana.com"
            payload = {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "getBalance",
                "params": [oasis.wallet_address]
            }
            response = requests.post(rpc_url, json=payload, timeout=10)
            if response.status_code == 200:
                data = response.json()
                if "result" in data:
                    lamports = data["result"].get("value", 0)
                    sol_balance = lamports / 1_000_000_000
                    print(f"✅ Balance: {sol_balance:.4f} SOL ({lamports} lamports)")
                else:
                    print(f"⚠️  Could not get balance from RPC")
            else:
                print(f"⚠️  RPC request failed: {response.status_code}")
        
        # Also try OASIS API balance
        result = oasis.get_wallet_balance()
        if not result.get("isError"):
            balance = result.get("result", {}).get("data", {}).get("balance", {})
            if balance:
                print(f"   OASIS Balance: {balance.get('amount', 'N/A')} {balance.get('currency', 'N/A')}")
        
    except Exception as e:
        print(f"⚠️  Balance check error: {e}")
    
    print()
    print("=" * 60)
    if oasis.avatar_id and oasis.wallet_address:
        print("✅ Test Complete!")
        print("=" * 60)
        print()
        print("Summary:")
        print(f"  ✅ Avatar registered: {oasis.avatar_id}")
        print(f"  ✅ Wallet created: {oasis.wallet_address}")
        print(f"  ✅ Ready for agent operations!")
    elif oasis.avatar_id:
        print("⚠️  Partial Success - Email Verification Required")
        print("=" * 60)
        print()
        print("Summary:")
        print(f"  ✅ Avatar registered: {oasis.avatar_id}")
        print(f"  ⚠️  Authentication failed: Email not verified")
        print(f"  ❌ Wallet generation blocked: Requires authentication")
        print()
        print("💡 Solution:")
        print("   See EMAIL_VERIFICATION_WORKAROUND.md for options:")
        print("   1. Modify OASIS API to skip verification for @agents.local emails")
        print("   2. Manually verify avatar in database")
        print("   3. Use admin account for testing")
    else:
        print("❌ Test Failed")
        print("=" * 60)

if __name__ == "__main__":
    main()

