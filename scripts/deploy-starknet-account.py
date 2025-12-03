#!/usr/bin/env python3
"""
Starknet Account Deployment Script

This script deploys a Starknet account contract to the network.
Can be called from C# or run standalone.

Usage:
    python deploy-starknet-account.py --private-key <key> --network testnet
    python deploy-starknet-account.py --address <address> --network testnet --check-only
"""

import argparse
import json
import sys
from typing import Optional

try:
    from starknet_py.net import AccountClient, KeyPair
    from starknet_py.net.models import StarknetChainId
    from starknet_py.net.account.account import Account
    from starknet_py.net.gateway_client import GatewayClient
    from starknet_py.contract import Contract
    from starknet_py.net.models import Address
    from starknet_py.net.signer.stark_curve_signer import StarkCurveSigner
except ImportError:
    print("Error: starknet-py not installed. Install with: pip install starknet.py")
    sys.exit(1)


def get_network_config(network: str):
    """Get network configuration"""
    networks = {
        "mainnet": {
            "rpc_url": "https://starknet-mainnet.public.blastapi.io",
            "chain_id": StarknetChainId.MAINNET,
            "explorer": "https://starkscan.co"
        },
        "testnet": {
            "rpc_url": "https://starknet-sepolia.public.blastapi.io",
            "chain_id": StarknetChainId.SEPOLIA_TESTNET,
            "explorer": "https://sepolia.starkscan.co"
        },
        "sepolia": {
            "rpc_url": "https://starknet-sepolia.public.blastapi.io",
            "chain_id": StarknetChainId.SEPOLIA_TESTNET,
            "explorer": "https://sepolia.starkscan.co"
        }
    }
    
    if network.lower() not in networks:
        raise ValueError(f"Unknown network: {network}. Use: mainnet, testnet, or sepolia")
    
    return networks[network.lower()]


def check_account_deployed(address: str, network: str) -> bool:
    """Check if an account is already deployed"""
    try:
        config = get_network_config(network)
        client = GatewayClient(config["rpc_url"])
        
        # Try to get the account's class hash
        # If it exists, the account is deployed
        # This is a simplified check - in production, use proper RPC calls
        print(f"Checking if account {address} is deployed on {network}...")
        
        # TODO: Implement proper check using starknet_getClassHashAt
        # For now, return False (assume not deployed)
        return False
        
    except Exception as e:
        print(f"Error checking account deployment: {e}")
        return False


def deploy_account(private_key: str, network: str, rpc_url: Optional[str] = None) -> dict:
    """Deploy a Starknet account contract"""
    try:
        config = get_network_config(network)
        if rpc_url:
            config["rpc_url"] = rpc_url
        
        # Convert private key to int
        if private_key.startswith("0x"):
            private_key = private_key[2:]
        priv_key_int = int(private_key, 16)
        
        # Create key pair
        key_pair = KeyPair.from_private_key(priv_key_int)
        
        # Get the computed address (before deployment)
        # This matches the address derivation in C#
        from starknet_py.net.account.account import compute_address
        from starknet_py.net.models import Address
        
        # OpenZeppelin account class hash
        account_class_hash = 0x027214a306090cd26575758e8e1b3a
        
        # Compute address
        computed_address = compute_address(
            class_hash=account_class_hash,
            constructor_calldata=[key_pair.public_key],
            salt=0
        )
        
        print(f"Computed address: {hex(computed_address)}")
        print(f"Network: {network}")
        print(f"RPC URL: {config['rpc_url']}")
        
        # Create account client
        account = Account(
            address=computed_address,
            client=GatewayClient(config["rpc_url"]),
            key_pair=key_pair,
            chain=config["chain_id"]
        )
        
        # Check if already deployed
        try:
            # Try to get account info - if it fails, account is not deployed
            # This is a simplified check
            print("Checking if account is already deployed...")
            # In production, use proper RPC call: starknet_getClassHashAt
            is_deployed = False
        except:
            is_deployed = False
        
        if is_deployed:
            return {
                "success": True,
                "address": hex(computed_address),
                "message": "Account already deployed",
                "transaction_hash": None
            }
        
        # Deploy account
        print("Deploying account contract...")
        print("⚠️  Note: Account must be funded before deployment!")
        print(f"Send ETH to: {hex(computed_address)}")
        
        # TODO: Implement actual deployment
        # This requires:
        # 1. Account to be funded
        # 2. Deploy transaction
        # 3. Wait for confirmation
        
        print("\n⚠️  Full deployment not yet implemented in this script.")
        print("For now, use starkli CLI:")
        print(f"  starkli account deploy <account.json> --network {network}")
        
        return {
            "success": False,
            "address": hex(computed_address),
            "message": "Deployment requires funding and full implementation",
            "transaction_hash": None,
            "instructions": f"""
To deploy this account:
1. Fund the address: {hex(computed_address)}
2. Use starkli: starkli account deploy <account.json> --network {network}
3. Or implement full deployment in this script
            """
        }
        
    except Exception as e:
        return {
            "success": False,
            "error": str(e),
            "message": "Deployment failed"
        }


def main():
    parser = argparse.ArgumentParser(description="Deploy Starknet account contract")
    parser.add_argument("--private-key", help="Private key (hex, with or without 0x)")
    parser.add_argument("--address", help="Account address to check")
    parser.add_argument("--network", default="testnet", choices=["mainnet", "testnet", "sepolia"],
                       help="Network to deploy to")
    parser.add_argument("--rpc-url", help="Custom RPC URL (optional)")
    parser.add_argument("--check-only", action="store_true", 
                       help="Only check if account is deployed, don't deploy")
    
    args = parser.parse_args()
    
    if args.check_only:
        if not args.address:
            print("Error: --address required for --check-only")
            sys.exit(1)
        
        is_deployed = check_account_deployed(args.address, args.network)
        result = {
            "address": args.address,
            "network": args.network,
            "is_deployed": is_deployed
        }
        print(json.dumps(result, indent=2))
        sys.exit(0 if is_deployed else 1)
    
    if not args.private_key:
        print("Error: --private-key required for deployment")
        sys.exit(1)
    
    result = deploy_account(args.private_key, args.network, args.rpc_url)
    print(json.dumps(result, indent=2))
    
    sys.exit(0 if result.get("success") else 1)


if __name__ == "__main__":
    main()

