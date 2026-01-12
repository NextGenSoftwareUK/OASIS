#!/usr/bin/env python3
"""
Test script for Generic Token Operations (ERC-20 compatible)
Tests the new generic token endpoints in WalletController
"""

import os
import sys
import json
import requests
from typing import Optional, Dict, Any

# Configuration
API_BASE_URL = os.getenv("API_BASE_URL", "http://localhost:5003")
JWT_TOKEN = os.getenv("JWT_TOKEN", "")
AVATAR_ID = os.getenv("AVATAR_ID", "")
MNEE_CONTRACT = "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF"
USDC_CONTRACT = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48"  # Example: USDC on Ethereum
SPENDER_ADDRESS = os.getenv("SPENDER_ADDRESS", "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb")

# Test counters
passed = 0
failed = 0


def print_header(text: str):
    """Print a formatted header"""
    print(f"\n{'=' * 50}")
    print(f"{text}")
    print(f"{'=' * 50}\n")


def test_endpoint(
    name: str,
    method: str,
    url: str,
    data: Optional[Dict[str, Any]] = None,
    params: Optional[Dict[str, Any]] = None
) -> bool:
    """
    Test an API endpoint
    
    Args:
        name: Test name
        method: HTTP method (GET, POST, etc.)
        url: Endpoint URL
        data: Request body data (for POST/PUT)
        params: Query parameters (for GET)
    
    Returns:
        True if test passed, False otherwise
    """
    global passed, failed
    
    print(f"🧪 Testing: {name}")
    print(f"   {method} {url}")
    if params:
        print(f"   Params: {params}")
    if data:
        print(f"   Body: {json.dumps(data, indent=2)}")
    
    headers = {
        "Authorization": f"Bearer {JWT_TOKEN}",
        "Content-Type": "application/json"
    }
    
    try:
        if method.upper() == "GET":
            response = requests.get(
                f"{API_BASE_URL}{url}",
                headers=headers,
                params=params,
                verify=False,
                timeout=30
            )
        elif method.upper() == "POST":
            response = requests.post(
                f"{API_BASE_URL}{url}",
                headers=headers,
                json=data,
                params=params,
                verify=False,
                timeout=30
            )
        else:
            print(f"   ❌ Unsupported method: {method}")
            failed += 1
            return False
        
        response.raise_for_status()
        result = response.json()
        
        # Check if response indicates success
        if isinstance(result, dict):
            is_error = result.get("isError", False)
            if is_error:
                print(f"   ❌ API Error: {result.get('message', 'Unknown error')}")
                failed += 1
                return False
            else:
                print(f"   ✅ Success")
                # Pretty print result (truncate if too long)
                result_str = json.dumps(result, indent=2)
                if len(result_str) > 500:
                    result_str = result_str[:500] + "..."
                print(f"   Response: {result_str}")
                passed += 1
                return True
        else:
            print(f"   ✅ Success (non-JSON response)")
            print(f"   Response: {str(result)[:200]}")
            passed += 1
            return True
            
    except requests.exceptions.RequestException as e:
        print(f"   ❌ Request failed: {str(e)}")
        failed += 1
        return False
    except json.JSONDecodeError as e:
        print(f"   ⚠️  Response is not valid JSON: {str(e)}")
        print(f"   Raw response: {response.text[:200]}")
        failed += 1
        return False
    except Exception as e:
        print(f"   ❌ Unexpected error: {str(e)}")
        failed += 1
        return False


def main():
    """Run all tests"""
    global passed, failed
    
    print_header("Generic Token Operations Test Suite")
    
    # Check if JWT token is set
    if not JWT_TOKEN:
        print("❌ Error: JWT_TOKEN environment variable is not set")
        print("Please set it with: export JWT_TOKEN='your_token_here'")
        sys.exit(1)
    
    # Check API availability
    print("Checking API availability...")
    try:
        requests.get(f"{API_BASE_URL}/api/health", verify=False, timeout=5)
        print("✅ API is reachable\n")
    except:
        print("⚠️  Warning: API health check failed. Continuing anyway...\n")
    
    # Test 1: Get MNEE Token Balance
    print_header("Test 1: Get MNEE Token Balance")
    params = {
        "tokenContractAddress": MNEE_CONTRACT,
        "providerType": "EthereumOASIS"
    }
    if AVATAR_ID:
        params["avatarId"] = AVATAR_ID
    test_endpoint("Get MNEE Balance", "GET", "/api/wallet/token/balance", params=params)
    
    # Test 2: Get USDC Token Balance (example of different token)
    print_header("Test 2: Get USDC Token Balance (Generic)")
    params = {
        "tokenContractAddress": USDC_CONTRACT,
        "providerType": "EthereumOASIS"
    }
    if AVATAR_ID:
        params["avatarId"] = AVATAR_ID
    test_endpoint("Get USDC Balance", "GET", "/api/wallet/token/balance", params=params)
    
    # Test 3: Get Token Info (MNEE)
    print_header("Test 3: Get MNEE Token Info")
    params = {
        "tokenContractAddress": MNEE_CONTRACT,
        "providerType": "EthereumOASIS"
    }
    test_endpoint("Get MNEE Token Info", "GET", "/api/wallet/token/info", params=params)
    
    # Test 4: Get Token Info (USDC)
    print_header("Test 4: Get USDC Token Info (Generic)")
    params = {
        "tokenContractAddress": USDC_CONTRACT,
        "providerType": "EthereumOASIS"
    }
    test_endpoint("Get USDC Token Info", "GET", "/api/wallet/token/info", params=params)
    
    # Test 5: Get Token Allowance
    print_header("Test 5: Get Token Allowance")
    params = {
        "tokenContractAddress": MNEE_CONTRACT,
        "spenderAddress": SPENDER_ADDRESS,
        "providerType": "EthereumOASIS"
    }
    if AVATAR_ID:
        params["avatarId"] = AVATAR_ID
    test_endpoint("Get MNEE Allowance", "GET", "/api/wallet/token/allowance", params=params)
    
    # Test 6: Approve Token
    print_header("Test 6: Approve Token")
    approval_data = {
        "avatarId": AVATAR_ID if AVATAR_ID else "00000000-0000-0000-0000-000000000000",
        "tokenContractAddress": MNEE_CONTRACT,
        "spenderAddress": SPENDER_ADDRESS,
        "amount": 100.0,
        "providerType": "EthereumOASIS"
    }
    test_endpoint("Approve MNEE", "POST", "/api/wallet/token/approve", data=approval_data)
    
    # Summary
    print_header("Test Summary")
    print(f"✅ Passed: {passed}")
    print(f"❌ Failed: {failed}")
    print()
    
    if failed == 0:
        print("🎉 All tests passed!")
        sys.exit(0)
    else:
        print("⚠️  Some tests failed. Check the output above.")
        sys.exit(1)


if __name__ == "__main__":
    main()
