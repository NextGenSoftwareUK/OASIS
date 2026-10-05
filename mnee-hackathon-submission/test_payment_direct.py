#!/usr/bin/env python3
"""
Direct payment test - tests the Base58 fix without agent discovery
"""
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'src'))

from core.oasis_client import OASISClient
from config import OASIS_API_URL

def test_payment():
    print("=" * 60)
    print("  Direct Payment Test - Base58 Fix Verification")
    print("=" * 60)
    print()
    
    # Use fixed usernames so we can reuse the same agents/wallets
    # Using new usernames with deterministic passwords
    sender_username = "payment_test_sender_v2"
    receiver_username = "payment_test_receiver_v2"
    
    # Create OASIS client
    oasis = OASISClient(OASIS_API_URL)
    
    # Step 1: Register or authenticate sender agent
    print("📝 Step 1: Registering/authenticating sender agent...")
    result = oasis.register_or_authenticate_agent(sender_username)
    
    if not result or not oasis.avatar_id:
        print("❌ Failed to register/authenticate sender agent")
        return False
    
    print(f"✅ Sender ready: {oasis.avatar_id}")
    
    # Step 2: Check if sender wallet exists, generate if not
    print("\n🔑 Step 2: Checking sender wallet...")
    sender_wallets_result = oasis.get_wallets()
    wallets = []
    if sender_wallets_result and not sender_wallets_result.get('isError'):
        wallets = sender_wallets_result.get('result', {}).get('result', [])
    
    if wallets:
        sender_address = wallets[0].get('address', 'N/A')
        print(f"✅ Sender wallet exists: {sender_address}")
    else:
        print("   No wallet found, generating new wallet...")
        wallet_result = oasis.generate_wallet("SolanaOASIS")
        if not wallet_result:
            print("❌ Failed to generate sender wallet")
            return False
        
        sender_wallets_result = oasis.get_wallets()
        if not sender_wallets_result or sender_wallets_result.get('isError'):
            print("❌ Failed to get sender wallet")
            return False
        
        wallets = sender_wallets_result.get('result', {}).get('result', [])
        sender_address = wallets[0].get('address', 'N/A') if wallets else 'N/A'
        print(f"✅ Sender wallet generated: {sender_address}")
    
    sender_address = wallets[0].get('address', 'N/A') if wallets else 'N/A'
    
    # Step 3: Register or authenticate receiver agent
    print("\n📝 Step 3: Registering/authenticating receiver agent...")
    receiver_oasis = OASISClient(OASIS_API_URL)
    receiver_result = receiver_oasis.register_or_authenticate_agent(receiver_username)
    
    if not receiver_result or not receiver_oasis.avatar_id:
        print("❌ Failed to register/authenticate receiver agent")
        return False
    
    print(f"✅ Receiver ready: {receiver_oasis.avatar_id}")
    
    # Step 4: Check if receiver wallet exists, generate if not
    print("\n🔑 Step 4: Checking receiver wallet...")
    receiver_wallets_result = receiver_oasis.get_wallets()
    receiver_wallets = []
    if receiver_wallets_result and not receiver_wallets_result.get('isError'):
        receiver_wallets = receiver_wallets_result.get('result', {}).get('result', [])
    
    if receiver_wallets:
        receiver_address = receiver_wallets[0].get('address', 'N/A')
        print(f"✅ Receiver wallet exists: {receiver_address}")
    else:
        print("   No wallet found, generating new wallet...")
        receiver_wallet_result = receiver_oasis.generate_wallet("SolanaOASIS")
        if not receiver_wallet_result:
            print("❌ Failed to generate receiver wallet")
            return False
        
        receiver_wallets_result = receiver_oasis.get_wallets()
        if not receiver_wallets_result or receiver_wallets_result.get('isError'):
            print("❌ Failed to get receiver wallet")
            return False
        
        receiver_wallets = receiver_wallets_result.get('result', {}).get('result', [])
        receiver_address = receiver_wallets[0].get('address', 'N/A') if receiver_wallets else 'N/A'
        print(f"✅ Receiver wallet generated: {receiver_address}")
    
    receiver_address = receiver_wallets[0].get('address', 'N/A') if receiver_wallets else 'N/A'
    
    # Step 5: Test payment
    print("\n💳 Step 5: Testing payment (0.01 SOL)...")
    print(f"   From: {oasis.avatar_id}")
    print(f"   To: {receiver_oasis.avatar_id}")
    
    # Verify sender is authenticated
    if not oasis.token or not oasis.avatar_id:
        print("❌ Sender not authenticated - re-authenticating...")
        oasis.register_or_authenticate_agent(sender_username)
    
    print(f"   Sender token: {'Present' if oasis.token else 'Missing'}")
    print(f"   Sender avatar_id: {oasis.avatar_id}")
    
    try:
        payment_result = oasis.send_payment(
            to_avatar_id=receiver_oasis.avatar_id,
            amount="0.01",
            description="Test payment with Base58 fix"
        )
        
        if payment_result and not payment_result.get('isError', True):
            print("✅ PAYMENT SUCCESSFUL!")
            print(f"   Transaction: {payment_result.get('result', {}).get('transactionSignature', 'N/A')}")
            return True
        else:
            error_msg = payment_result.get('message', 'Unknown error') if payment_result else 'No response'
            print(f"❌ PAYMENT FAILED: {error_msg}")
            
            # Check if it's the old error
            if "expandedPrivateKey.Count" in error_msg:
                print("\n⚠️  The Base58 fix didn't work - still getting expandedPrivateKey.Count error")
            else:
                print(f"\n⚠️  Different error: {error_msg}")
            
            return False
            
    except Exception as e:
        print(f"❌ PAYMENT EXCEPTION: {str(e)}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    success = test_payment()
    sys.exit(0 if success else 1)

