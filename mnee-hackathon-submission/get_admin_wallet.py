#!/usr/bin/env python3
"""
Get OASIS_ADMIN avatar's Solana wallet address
"""
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'src'))

from core.oasis_client import OASISClient
from config import OASIS_API_URL

def get_admin_wallet():
    print("=" * 60)
    print("  Get OASIS_ADMIN Solana Wallet Address")
    print("=" * 60)
    print()
    
    # Create OASIS client
    oasis = OASISClient(OASIS_API_URL)
    
    # Authenticate as OASIS_ADMIN
    print("🔐 Authenticating as OASIS_ADMIN...")
    result = oasis.authenticate("OASIS_ADMIN", "Uppermall1!")
    
    if result.get("isError") or not oasis.avatar_id or not oasis.token:
        print("❌ Authentication failed")
        print(f"   Error: {result.get('message', 'Unknown error')}")
        return None
    
    print(f"✅ Authenticated as OASIS_ADMIN")
    print(f"   Avatar ID: {oasis.avatar_id}")
    print()
    
    # Get wallets
    print("🔍 Fetching wallets...")
    wallets_result = oasis.get_wallets()
    
    if wallets_result.get("isError"):
        print("❌ Failed to get wallets")
        print(f"   Error: {wallets_result.get('message', 'Unknown error')}")
        return None
    
    # Extract wallets from response
    wallets_data = wallets_result.get("result", {})
    
    # Handle different response structures
    wallets = []
    if isinstance(wallets_data, dict):
        # Dictionary of provider types -> wallets
        for provider, wallet_list in wallets_data.items():
            if isinstance(wallet_list, list):
                wallets.extend(wallet_list)
            elif wallet_list:
                wallets.append(wallet_list)
    elif isinstance(wallets_data, list):
        wallets = wallets_data
    
    # Find Solana wallet
    solana_wallet = None
    for wallet in wallets:
        provider_type = wallet.get("providerType") or wallet.get("provider_type")
        if provider_type in ["SolanaOASIS", "Solana"]:
            solana_wallet = wallet
            break
    
    if not solana_wallet:
        print("❌ No Solana wallet found")
        print(f"   Found {len(wallets)} wallet(s) but none are Solana")
        if wallets:
            print(f"   Provider types: {[w.get('providerType', 'unknown') for w in wallets]}")
        return None
    
    # Extract wallet address
    wallet_address = (
        solana_wallet.get("address") or
        solana_wallet.get("walletAddress") or
        solana_wallet.get("publicKey") or
        solana_wallet.get("PublicKey")
    )
    
    if not wallet_address:
        print("❌ Solana wallet found but no address")
        print(f"   Wallet data: {solana_wallet}")
        return None
    
    print("✅ Found Solana wallet!")
    print()
    print("=" * 60)
    print("  SOLANA WALLET ADDRESS")
    print("=" * 60)
    print(f"  {wallet_address}")
    print("=" * 60)
    print()
    print("💡 You can now:")
    print(f"   1. Fund this wallet: https://faucet.solana.com/?address={wallet_address}")
    print(f"   2. Transfer SOL from this wallet to agent wallets")
    print(f"   3. Check balance: solana balance {wallet_address} --url devnet")
    print()
    
    return wallet_address

if __name__ == "__main__":
    wallet_address = get_admin_wallet()
    sys.exit(0 if wallet_address else 1)

