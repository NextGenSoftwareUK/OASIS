"""
OASIS API Client for MNEE Hackathon Submission
Handles avatar registration, wallet generation, MNEE payments, and karma tracking
"""

import requests
import json
import time
from typing import Optional, Dict, Any, List
from config import (
    OASIS_API_URL, 
    OASIS_VERIFY_SSL, 
    PAYMENT_PROVIDER_TYPE,
    PAYMENT_TOKEN_SYMBOL,
    MNEE_CONTRACT_ADDRESS, 
    ETHEREUM_CHAIN_ID
)


class OASISClient:
    """OASIS API client for agent registration, wallet management, and MNEE payments"""
    
    def __init__(self, base_url: Optional[str] = None):
        self.base_url = base_url or OASIS_API_URL
        self.token: Optional[str] = None
        self.avatar_id: Optional[str] = None
        self.wallet_address: Optional[str] = None
        self.verify_ssl = OASIS_VERIFY_SSL
        self.token_expiry: Optional[float] = None
    
    def _make_request(
        self, 
        method: str, 
        endpoint: str, 
        data: Optional[Dict] = None,
        requires_auth: bool = True,
        headers_override: Optional[Dict] = None
    ) -> Dict[str, Any]:
        """Make HTTP request to OASIS API"""
        url = f"{self.base_url}{endpoint}"
        headers = {"Content-Type": "application/json"}
        
        if requires_auth and self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        
        try:
            if method.upper() == "GET":
                response = requests.get(url, headers=headers, verify=self.verify_ssl, timeout=30)
            elif method.upper() == "POST":
                response = requests.post(url, headers=headers, json=data, verify=self.verify_ssl, timeout=30)
            elif method.upper() == "PUT":
                response = requests.put(url, headers=headers, json=data, verify=self.verify_ssl, timeout=30)
            elif method.upper() == "DELETE":
                response = requests.delete(url, headers=headers, verify=self.verify_ssl, timeout=30)
            else:
                raise ValueError(f"Unsupported HTTP method: {method}")
            
            response.raise_for_status()
            return response.json()
        except requests.exceptions.SSLError as e:
            print(f"⚠️  SSL error: {e}")
            print("💡 Try setting OASIS_VERIFY_SSL = False in config.py")
            raise
        except requests.exceptions.ConnectionError as e:
            print(f"❌ Connection error: {e}")
            print(f"💡 Is OASIS API running at {self.base_url}?")
            raise
        except requests.exceptions.HTTPError as e:
            error_data = {}
            try:
                error_data = e.response.json()
            except:
                try:
                    error_data = {"message": e.response.text}
                except:
                    pass
            error_msg = error_data.get('message', str(e))
            print(f"❌ HTTP error {e.response.status_code}: {error_msg}")
            # Print full error details for debugging
            if error_data:
                print(f"   Full error: {error_data}")
            raise
        except Exception as e:
            print(f"❌ Request error: {e}")
            raise
    
    def register_avatar(
        self, 
        username: str, 
        email: Optional[str] = None, 
        password: str = "",
        avatar_type: str = "User",
        auto_generate_email: bool = True
    ) -> Dict[str, Any]:
        """
        Register agent as OASIS Avatar
        
        Args:
            username: Username for the avatar
            email: Email address (auto-generated if None and auto_generate_email=True)
            password: Password (auto-generated if empty)
            avatar_type: Type of avatar (default: "User")
            auto_generate_email: If True, generates fake email for agents
        """
        # Auto-generate email for agents if not provided
        if email is None and auto_generate_email:
            email = f"agent_{username}@agents.local"
        
        # Auto-generate password if not provided
        if not password:
            import secrets
            password = secrets.token_urlsafe(16)
        
        # OASIS API requires FirstName, LastName, and ConfirmPassword (from CreateRequest base class)
        # For agents, we'll use generic values
        data = {
            "username": username,
            "email": email,
            "password": password,
            "confirmPassword": password,  # Required - must match password
            "firstName": "Agent",  # Required field
            "lastName": username.title(),  # Use username as last name
            "title": "Agent",  # Optional but can help
            "avatarType": avatar_type,
            "acceptTerms": True
        }
        
        try:
            result = self._make_request("POST", "/api/avatar/register", data=data, requires_auth=False)
        except requests.exceptions.HTTPError as e:
            # If registration fails, capture the full error response
            error_data = {}
            try:
                error_data = e.response.json()
            except:
                try:
                    error_data = {"message": e.response.text, "statusCode": e.response.status_code}
                except:
                    error_data = {"message": str(e), "statusCode": e.response.status_code if hasattr(e, 'response') else None}
            
            print(f"❌ Registration HTTP error {e.response.status_code if hasattr(e, 'response') else 'unknown'}")
            print(f"   Error data: {error_data}")
            return {"isError": True, **error_data}
        
        # Check if result indicates error (multiple ways to check)
        is_error = (
            result.get("isError") or 
            result.get("result", {}).get("isError") or
            result.get("statusCode", 200) >= 400
        )
        
        if not is_error:
            # Handle OASIS API response structure: result.result.result.avatarId
            result_data = result.get("result", {})
            inner_result = result_data.get("result", {})
            
            # Extract avatar ID from nested structure
            self.avatar_id = (
                inner_result.get("avatarId") or
                inner_result.get("id") or
                result_data.get("avatarId") or
                result_data.get("id")
            )
            
            # Registration doesn't return a token - we need to authenticate after
            # But check if one exists anyway
            self.token = (
                inner_result.get("token") or
                inner_result.get("jwtToken") or
                result_data.get("token") or
                result_data.get("jwtToken")
            )
            
            if self.avatar_id:
                print(f"✅ Registered as OASIS Avatar: {self.avatar_id}")
                print(f"   Email: {email} (auto-generated for agent)")
                
                # After registration, authenticate to get token
                if not self.token:
                    print(f"🔄 Authenticating to get token...")
                    try:
                        auth_result = self.authenticate(username, password)
                        if not auth_result.get("isError") and self.token:
                            print(f"✅ Authenticated and received token")
                    except Exception as e:
                        print(f"⚠️  Authentication after registration failed: {e}")
                        print(f"   You may need to verify email first, or authentication may work without token")
            else:
                print(f"⚠️  Registration succeeded but avatar ID not found")
                print(f"   Response structure: result.result.result.avatarId")
                print(f"   Available keys: {list(inner_result.keys())[:10]}")
        else:
            error_msg = (
                result.get("message") or 
                result.get("result", {}).get("message") or
                "Unknown error"
            )
            print(f"❌ Registration failed: {error_msg}")
            # Print more details for debugging
            if result.get("result", {}).get("detailedMessage"):
                print(f"   Details: {result.get('result', {}).get('detailedMessage')}")
        
        return result
    
    def verify_email(self, token: str) -> Dict[str, Any]:
        """Verify email using verification token"""
        data = {"token": token}
        result = self._make_request("POST", "/api/avatar/verify-email", data=data, requires_auth=False)
        return result
    
    def authenticate(self, username: str, password: str) -> Dict[str, Any]:
        """
        Authenticate existing avatar
        
        Note: Authentication should work even if email is not verified.
        If authentication fails due to email verification, try registering again.
        """
        data = {"username": username, "password": password}
        
        try:
            result = self._make_request("POST", "/api/avatar/authenticate", data=data, requires_auth=False)
        except requests.exceptions.HTTPError as e:
            # Authentication failed (likely email not verified)
            error_data = {}
            try:
                error_data = e.response.json()
            except:
                error_data = {"message": str(e), "statusCode": e.response.status_code if hasattr(e, 'response') else None}
            
            # Check if it's an email verification error
            if error_data.get("statusCode") == 401:
                error_msg = error_data.get("result", {}).get("message", "") or error_data.get("message", "")
                if "verified" in error_msg.lower() or "email" in error_msg.lower():
                    return {
                        "isError": True,
                        "message": "Email not verified. Please verify email or disable verification for agents.",
                        "requiresVerification": True,
                        **error_data
                    }
            
            return {"isError": True, **error_data}
        
        # Check multiple ways for error
        is_error = (
            result.get("isError") or 
            result.get("result", {}).get("isError") or
            result.get("statusCode", 200) >= 400
        )
        
        if not is_error:
            # Handle different response structures
            result_data = result.get("result", {})
            inner_result = result_data.get("result", {})
            
            # Try to get avatar ID from different possible locations
            new_avatar_id = (
                inner_result.get("avatarId") or
                inner_result.get("id") or
                result_data.get("avatarId") or
                result_data.get("id")
            )
            
            # Only update avatar_id if we found a new one (don't clear existing)
            if new_avatar_id:
                self.avatar_id = new_avatar_id
            
            # Try to get token from different possible locations
            new_token = (
                inner_result.get("token") or
                inner_result.get("jwtToken") or
                result_data.get("token") or
                result_data.get("jwtToken")
            )
            
            if new_token:
                self.token = new_token
                self.token_expiry = time.time() + (24 * 60 * 60)
            
            if self.avatar_id and self.token:
                print(f"✅ Authenticated as OASIS Avatar: {self.avatar_id}")
            elif self.avatar_id:
                print(f"⚠️  Authentication succeeded but no token received")
                print(f"   Avatar ID: {self.avatar_id}")
                print(f"   This may indicate email verification is required")
            else:
                print(f"⚠️  Authentication succeeded but avatar ID not found in response")
        else:
            error_msg = (
                result.get("message") or 
                result.get("result", {}).get("message") or
                "Unknown error"
            )
            if "verified" in error_msg.lower() or "email" in error_msg.lower():
                print(f"❌ Authentication failed: Email verification required")
                print(f"   Message: {error_msg}")
                print(f"💡 See EMAIL_VERIFICATION_WORKAROUND.md for solutions")
                result["requiresVerification"] = True
            else:
                print(f"❌ Authentication failed: {error_msg}")
        
        return result
    
    def register_or_authenticate_agent(
        self,
        username: str,
        password: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Convenience method for agents: register if new, authenticate if exists
        
        Args:
            username: Username for the agent
            password: Password (auto-generated if None, deterministic for test agents)
        
        Returns:
            Registration or authentication result
        """
        # Use deterministic password for test agents (so we can reuse them)
        # For production agents, use random password
        if password is None:
            # Use deterministic password based on username for test agents
            # This allows us to reuse the same agents across test runs
            import hashlib
            password_hash = hashlib.sha256(username.encode()).hexdigest()[:16]
            password = f"test_{password_hash}"
        
        # Try to authenticate first (in case agent already exists)
        try:
            result = self.authenticate(username, password)
            # Check if authentication actually succeeded (has avatar_id and token)
            if not result.get("isError") and self.avatar_id and self.token:
                print(f"✅ Agent already exists, authenticated")
                return result
            else:
                print(f"⚠️  Authentication returned but missing avatar_id or token")
        except Exception as e:
            print(f"❌ Authentication failed: {e}")
        
        # If authentication fails or incomplete, try to register
        print(f"🔄 Registering new agent: {username}")
        result = self.register_avatar(
            username=username,
            email=None,  # Auto-generate
            password=password,
            avatar_type="User",
            auto_generate_email=True
        )
        
        # Store password for potential future authentication
        self._agent_password = password
        
        return result
    
    def generate_wallet(self, provider_type: Optional[str] = None) -> Dict[str, Any]:
        """
        Generate wallet for avatar using OASIS Keys API
        
        Flow:
        1. Generate keypair for provider
        2. Link private key to avatar (creates wallet)
        3. Link public key to avatar (completes wallet)
        """
        if provider_type is None:
            provider_type = PAYMENT_PROVIDER_TYPE
        
        if not self.token or not self.avatar_id:
            raise ValueError("Must register/authenticate first")
        
        try:
            # Step 1: Generate keypair
            print(f"🔑 Step 1: Generating {provider_type} keypair...")
            keypair_result = self._make_request(
                "POST",
                f"/api/keys/generate_keypair_for_provider/{provider_type}"
            )
            
            if keypair_result.get("isError") or keypair_result.get("result", {}).get("isError"):
                error_msg = keypair_result.get("result", {}).get("message") or keypair_result.get("message", "Unknown error")
                raise ValueError(f"Failed to generate keypair: {error_msg}")
            
            # Extract keypair from nested response
            keypair_data = (
                keypair_result.get("result", {}).get("result") or
                keypair_result.get("result") or
                keypair_result
            )
            
            private_key = keypair_data.get("privateKey")
            public_key = keypair_data.get("publicKey")
            wallet_address = keypair_data.get("walletAddress") or public_key
            
            if not private_key or not public_key:
                raise ValueError("Invalid keypair response: missing privateKey or publicKey")
            
            print(f"✅ Keypair generated: {wallet_address[:20]}...")
            
            # Step 2: Link private key (creates wallet)
            print(f"🔗 Step 2: Linking private key to avatar...")
            link_private_result = self._make_request(
                "POST",
                "/api/keys/link_provider_private_key_to_avatar_by_id",
                data={
                    "AvatarID": self.avatar_id,
                    "ProviderType": provider_type,
                    "ProviderKey": private_key
                }
            )
            
            if link_private_result.get("isError") or link_private_result.get("result", {}).get("isError"):
                error_msg = link_private_result.get("result", {}).get("message") or link_private_result.get("message", "Unknown error")
                raise ValueError(f"Failed to link private key: {error_msg}")
            
            wallet_id = (
                link_private_result.get("result", {}).get("result", {}).get("walletId") or
                link_private_result.get("result", {}).get("walletId") or
                link_private_result.get("walletId")
            )
            
            print(f"✅ Private key linked, wallet ID: {wallet_id}")
            
            # Step 3: Link public key (completes wallet)
            print(f"🔗 Step 3: Linking public key to avatar...")
            link_public_result = self._make_request(
                "POST",
                "/api/keys/link_provider_public_key_to_avatar_by_id",
                data={
                    "AvatarID": self.avatar_id,
                    "ProviderType": provider_type,
                    "ProviderKey": public_key,
                    "WalletId": wallet_id
                }
            )
            
            if link_public_result.get("isError") or link_public_result.get("result", {}).get("isError"):
                error_msg = link_public_result.get("result", {}).get("message") or link_public_result.get("message", "Unknown error")
                print(f"⚠️  Warning: Failed to link public key: {error_msg}")
                # Continue anyway - wallet might still work
            
            print(f"✅ Public key linked")
            
            # Store wallet address
            self.wallet_address = wallet_address
            
            # Refresh wallets to get full details
            self.get_wallets()
            
            # Auto-request devnet SOL if Solana wallet
            if provider_type == "SolanaOASIS" and wallet_address:
                print()
                print("💰 Requesting devnet SOL from faucet...")
                faucet_result = self.request_devnet_sol(wallet_address, amount_sol=2.0)
                if faucet_result.get("success"):
                    print(f"✅ Devnet SOL requested: {faucet_result.get('amount', 2.0)} SOL")
                    if faucet_result.get("signature"):
                        print(f"   Transaction: {faucet_result.get('signature')}")
                    print(f"   Method: {faucet_result.get('method', 'unknown')}")
                    print(f"   Balance will update in a few seconds...")
                else:
                    print(f"⚠️  Automated faucet unavailable")
                    print(f"   {faucet_result.get('message', 'Unknown error')}")
                    manual = faucet_result.get("manual_options", {})
                    if manual:
                        print(f"   📋 Manual options:")
                        if manual.get("web"):
                            print(f"      Web: {manual['web']}")
                        if manual.get("cli"):
                            print(f"      CLI: {manual['cli']}")
                    else:
                        print(f"   💡 Try: https://faucet.solana.com/?address={wallet_address}")
            
            return {
                "isError": False,
                "walletAddress": wallet_address,
                "walletId": wallet_id,
                "providerType": provider_type
            }
            
        except Exception as e:
            print(f"❌ Wallet generation error: {e}")
            return {
                "isError": True,
                "message": str(e)
            }
    
    def request_devnet_sol(self, wallet_address: str, amount_sol: float = 2.0) -> Dict[str, Any]:
        """
        Request devnet SOL from Solana faucet
        
        Tries multiple methods:
        1. Solana JSON-RPC requestAirdrop
        2. Alternative faucet endpoints
        
        Args:
            wallet_address: Solana wallet address
            amount_sol: Amount of SOL to request (default: 2.0)
        
        Returns:
            Dict with success status, transaction signature, and amount
        """
        # Convert SOL to lamports (1 SOL = 1e9 lamports)
        lamports = int(amount_sol * 1_000_000_000)
        
        # Method 1: Try Solana JSON-RPC requestAirdrop
        rpc_url = "https://api.devnet.solana.com"
        payload = {
            "jsonrpc": "2.0",
            "id": 1,
            "method": "requestAirdrop",
            "params": [wallet_address, lamports]
        }
        
        try:
            response = requests.post(
                rpc_url,
                json=payload,
                headers={"Content-Type": "application/json"},
                timeout=30
            )
            
            response.raise_for_status()
            data = response.json()
            
            if "error" in data:
                error_msg = data["error"].get("message", "Unknown error")
                error_code = data["error"].get("code", 0)
                
                # Rate limit or temporary errors - try alternative
                if error_code == 429 or "rate limit" in error_msg.lower() or "429" in str(error_code):
                    print(f"   ⚠️  Rate limited, trying alternative method...")
                    return self._try_alternative_faucet(wallet_address, amount_sol)
                
                # Internal errors - also try alternative
                if "internal" in error_msg.lower() or error_code == -32603:
                    print(f"   ⚠️  RPC error, trying alternative method...")
                    return self._try_alternative_faucet(wallet_address, amount_sol)
                
                return {
                    "success": False,
                    "message": error_msg,
                    "amount": amount_sol,
                    "error_code": error_code
                }
            
            signature = data.get("result")
            if signature:
                return {
                    "success": True,
                    "signature": signature,
                    "amount": amount_sol,
                    "lamports": lamports,
                    "message": f"Successfully requested {amount_sol} SOL",
                    "method": "json-rpc"
                }
            else:
                return self._try_alternative_faucet(wallet_address, amount_sol)
                
        except requests.exceptions.Timeout:
            print(f"   ⚠️  RPC timeout, trying alternative method...")
            return self._try_alternative_faucet(wallet_address, amount_sol)
        except requests.exceptions.RequestException as e:
            print(f"   ⚠️  RPC error: {str(e)}, trying alternative method...")
            return self._try_alternative_faucet(wallet_address, amount_sol)
        except Exception as e:
            return {
                "success": False,
                "message": f"Error requesting SOL: {str(e)}",
                "amount": amount_sol
            }
    
    def _try_alternative_faucet(self, wallet_address: str, amount_sol: float) -> Dict[str, Any]:
        """Try alternative faucet methods"""
        # Try QuickNode faucet (if available)
        try:
            quicknode_url = "https://faucet.quicknode.com/solana/devnet"
            response = requests.post(
                quicknode_url,
                json={"address": wallet_address},
                headers={"Content-Type": "application/json"},
                timeout=15
            )
            if response.status_code == 200:
                data = response.json()
                if data.get("success") or "signature" in str(data):
                    return {
                        "success": True,
                        "amount": amount_sol,
                        "message": f"Successfully requested {amount_sol} SOL via QuickNode",
                        "method": "quicknode"
                    }
        except:
            pass
        
        # If all automated methods fail, provide helpful message
        return {
            "success": False,
            "message": "Automated faucet unavailable. Use manual methods:",
            "amount": amount_sol,
            "manual_options": {
                "web": f"https://faucet.solana.com/?address={wallet_address}",
                "cli": f"solana airdrop {amount_sol} {wallet_address} --url devnet",
                "explorer": f"https://explorer.solana.com/address/{wallet_address}?cluster=devnet"
            }
        }
    
    def get_wallets(self) -> Dict[str, Any]:
        """Get all wallets for avatar"""
        if not self.token or not self.avatar_id:
            raise ValueError("Must register/authenticate first")
        
        # Try different endpoint formats
        endpoints = [
            f"/api/wallet/avatar/{self.avatar_id}/wallets/false/false",
            f"/api/wallet/avatar/{self.avatar_id}/wallets",
            f"/api/wallet/avatar/id/{self.avatar_id}/wallets"
        ]
        
        for endpoint in endpoints:
            try:
                result = self._make_request("GET", endpoint)
                if not result.get("isError") and result.get("result"):
                    # Extract wallet address from response
                    wallets_data = result.get("result", {})
                    
                    # Handle different response structures
                    if isinstance(wallets_data, dict):
                        # Dictionary of provider types -> wallets
                        for provider, wallets in wallets_data.items():
                            if wallets and len(wallets) > 0:
                                wallet = wallets[0] if isinstance(wallets, list) else wallets
                                self.wallet_address = (
                                    wallet.get("address") or
                                    wallet.get("walletAddress") or
                                    wallet.get("publicKey")
                                )
                                if self.wallet_address:
                                    print(f"✅ Wallet: {self.wallet_address}")
                                    return result
                    elif isinstance(wallets_data, list):
                        # List of wallets
                        if len(wallets_data) > 0:
                            wallet = wallets_data[0]
                            self.wallet_address = (
                                wallet.get("address") or
                                wallet.get("walletAddress") or
                                wallet.get("publicKey")
                            )
                            if self.wallet_address:
                                print(f"✅ Wallet: {self.wallet_address}")
                                return result
                
                # If we got here without error, return the result
                if not result.get("isError"):
                    return result
            except Exception as e:
                # Try next endpoint
                continue
        
        # If wallet_address was set during generation, use that
        if self.wallet_address:
            print(f"✅ Using generated wallet: {self.wallet_address}")
            return {"isError": False, "walletAddress": self.wallet_address}
        
        return {"isError": True, "message": "Could not retrieve wallets"}
    
    def get_wallet_balance(self, wallet_id: Optional[str] = None) -> Dict[str, Any]:
        """Get wallet balance"""
        if not self.token or not self.avatar_id:
            raise ValueError("Must register/authenticate first")
        
        if wallet_id:
            result = self._make_request("GET", f"/api/wallet/{wallet_id}/balance")
        else:
            # Get default wallet balance
            wallets = self.get_wallets()
            if wallets.get("result"):
                for provider, wallet_list in wallets["result"].items():
                    if wallet_list and len(wallet_list) > 0:
                        wallet_id = wallet_list[0].get("id")
                        if wallet_id:
                            result = self._make_request("GET", f"/api/wallet/{wallet_id}/balance")
                            return result
        
        return {"isError": True, "message": "No wallet found"}
    
    def send_payment(
        self,
        to_avatar_id: str,
        amount: str,
        description: str = "",
        provider_type: Optional[str] = None,
        to_wallet_address: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Send payment to another avatar (SOL on Solana or MNEE on Ethereum)
        
        Args:
            to_avatar_id: Avatar ID of recipient
            amount: Amount to send (as string, e.g., "0.01")
            description: Payment description
            provider_type: Provider type (defaults to PAYMENT_PROVIDER_TYPE)
        """
        if not self.token or not self.avatar_id:
            raise ValueError("Must register/authenticate first")
        
        if provider_type is None:
            provider_type = PAYMENT_PROVIDER_TYPE
        
        # For Solana, use new SendToAvatar endpoint which uses the authenticated avatar's wallet
        if provider_type == "SolanaOASIS":
            # Use SolanaController.SendToAvatar endpoint which:
            # 1. Gets the authenticated avatar's private key from their wallet
            # 2. Signs the transaction with the correct key (not temporary account)
            # 3. Sends the transaction
            
            print(f"💳 Sending {amount} {PAYMENT_TOKEN_SYMBOL} using authenticated avatar's wallet...")
            print(f"   From Avatar: {self.avatar_id}")
            print(f"   To Avatar: {to_avatar_id}")
            
            try:
                amount_decimal = float(amount)
            except ValueError:
                return {
                    "isError": True,
                    "message": f"Invalid amount: {amount}"
                }
            
            # Use new SendToAvatar endpoint: POST /api/solana/SendToAvatar/{toAvatarId}
            # Amount is sent in request body as decimal, memoText as query parameter
            memo_text = description or f"Payment from {self.avatar_id} to {to_avatar_id}"
            # Send amount as decimal number in body (ASP.NET will deserialize it)
            # Need to send as raw number, not in a dict
            url = f"{self.base_url}/api/solana/SendToAvatar/{to_avatar_id}?memoText={memo_text}"
            headers = {"Content-Type": "application/json"}
            if self.token:
                headers["Authorization"] = f"Bearer {self.token}"
            
            import json
            response = requests.post(
                url,
                headers=headers,
                data=json.dumps(amount_decimal),  # Send decimal as JSON number
                verify=self.verify_ssl,
                timeout=30
            )
            response.raise_for_status()
            result = response.json()
            
            if not result.get("isError"):
                # Extract transaction hash from response
                transaction_hash = (
                    result.get("result", {}).get("result", {}).get("transactionHash") or
                    result.get("result", {}).get("transactionHash") or
                    result.get("transactionHash")
                )
                print(f"✅ Sent {amount} {PAYMENT_TOKEN_SYMBOL} to {to_avatar_id}")
                if transaction_hash:
                    print(f"   Transaction: {transaction_hash}")
            else:
                error_msg = (
                    result.get("result", {}).get("message") or 
                    result.get("message") or 
                    "Unknown error"
                )
                print(f"❌ Payment failed: {error_msg}")
            
            return result
        
        # For Ethereum/MNEE, use send_token endpoint
        else:
            data = {
                "fromAvatarId": self.avatar_id,
                "toAvatarId": to_avatar_id,
                "amount": amount,
                "tokenAddress": MNEE_CONTRACT_ADDRESS,
                "chainId": ETHEREUM_CHAIN_ID,
                "description": description or f"{PAYMENT_TOKEN_SYMBOL} payment from {self.avatar_id} to {to_avatar_id}"
            }
            
            result = self._make_request("POST", "/api/wallet/send_token", data=data)
            
            if not result.get("isError"):
                print(f"✅ Sent {amount} {PAYMENT_TOKEN_SYMBOL} to {to_avatar_id}")
            else:
                print(f"❌ Payment failed: {result.get('message', 'Unknown error')}")
            
            return result
    
    def send_mnee_payment(
        self,
        to_avatar_id: str,
        amount: str,
        description: str = ""
    ) -> Dict[str, Any]:
        """Send MNEE payment (alias for send_payment for backward compatibility)"""
        return self.send_payment(to_avatar_id, amount, description)
    
    def get_karma_stats(self, avatar_id: Optional[str] = None) -> Dict[str, Any]:
        """Get karma statistics for avatar"""
        if not self.token:
            raise ValueError("Must register/authenticate first")
        
        target_avatar_id = avatar_id or self.avatar_id
        if not target_avatar_id:
            raise ValueError("No avatar ID available")
        
        result = self._make_request("GET", f"/api/karma/get-karma-stats/{target_avatar_id}")
        return result
    
    def add_karma(
        self,
        karma_type: str = "Helpful",
        source_title: str = "Task Completion",
        source_desc: str = "A2A task completion"
    ) -> Dict[str, Any]:
        """Add karma after successful task"""
        if not self.token or not self.avatar_id:
            raise ValueError("Must register/authenticate first")
        
        data = {
            "karmaType": karma_type,
            "karmaSourceType": "App",
            "karamSourceTitle": source_title,
            "karmaSourceDesc": source_desc
        }
        
        result = self._make_request(
            "POST",
            f"/api/karma/add-karma-to-avatar/{self.avatar_id}",
            data=data
        )
        
        if not result.get("isError"):
            print(f"✅ Added karma: {source_title}")
        
        return result
    
    def get_avatar(self, avatar_id: Optional[str] = None) -> Dict[str, Any]:
        """Get avatar details"""
        if not self.token:
            raise ValueError("Must register/authenticate first")
        
        target_avatar_id = avatar_id or self.avatar_id
        if not target_avatar_id:
            raise ValueError("No avatar ID available")
        
        result = self._make_request("GET", f"/api/avatar/{target_avatar_id}")
        return result

