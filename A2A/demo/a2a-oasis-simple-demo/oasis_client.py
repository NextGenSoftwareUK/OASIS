"""
OASIS API Client for A2A Agent Integration
Handles avatar registration, wallet generation, and karma tracking
"""

import requests
import json
import os
from config import OASIS_API_URL, OASIS_VERIFY_SSL

class OASISClient:
    """Simple OASIS API client for agent registration"""
    
    def __init__(self, base_url=None):
        self.base_url = base_url or OASIS_API_URL
        self.token = None
        self.avatar_id = None
        self.wallet_address = None
        self.verify_ssl = OASIS_VERIFY_SSL
    
    def register_avatar(self, username, email, password):
        """Register agent as OASIS Avatar"""
        try:
            response = requests.post(
                f"{self.base_url}/api/avatar/register",
                json={
                    "username": username,
                    "email": email,
                    "password": password,
                    "avatarType": "User",
                    "acceptTerms": True
                },
                verify=self.verify_ssl,
                timeout=10
            )
            data = response.json()
            if not data.get("isError"):
                self.avatar_id = data["result"]["id"]
                self.token = data["result"]["token"]
                print(f"✅ Registered as OASIS Avatar: {self.avatar_id}")
            else:
                print(f"⚠️  Registration warning: {data.get('message', 'Unknown error')}")
            return data
        except requests.exceptions.SSLError:
            print("⚠️  SSL error - try setting OASIS_VERIFY_SSL = False in config.py")
            raise
        except requests.exceptions.ConnectionError:
            print(f"❌ Connection error - is OASIS API running at {self.base_url}?")
            raise
        except Exception as e:
            print(f"❌ Registration error: {e}")
            raise
    
    def authenticate(self, username, password):
        """Authenticate existing avatar"""
        try:
            response = requests.post(
                f"{self.base_url}/api/avatar/authenticate",
                json={"username": username, "password": password},
                verify=self.verify_ssl,
                timeout=10
            )
            data = response.json()
            if not data.get("isError"):
                self.avatar_id = data["result"]["id"]
                self.token = data["result"]["token"]
                print(f"✅ Authenticated as OASIS Avatar: {self.avatar_id}")
            return data
        except requests.exceptions.SSLError:
            print("⚠️  SSL error - try setting OASIS_VERIFY_SSL = False in config.py")
            raise
        except requests.exceptions.ConnectionError:
            print(f"❌ Connection error - is OASIS API running at {self.base_url}?")
            raise
        except Exception as e:
            print(f"❌ Authentication error: {e}")
            raise
    
    def generate_wallet(self, provider_type="EthereumOASIS"):
        """Generate wallet for avatar"""
        if not self.token or not self.avatar_id:
            print("❌ Must register/authenticate first")
            return None
        
        try:
            response = requests.post(
                f"{self.base_url}/api/wallet/avatar/{self.avatar_id}/generate",
                headers={"Authorization": f"Bearer {self.token}"},
                json={"providerType": provider_type, "setAsDefault": True},
                verify=self.verify_ssl,
                timeout=10
            )
            data = response.json()
            if not data.get("isError"):
                # Get wallet address
                self.get_wallets()
            return data
        except Exception as e:
            print(f"⚠️  Wallet generation error: {e}")
            return None
    
    def get_wallets(self):
        """Get avatar wallets"""
        if not self.token or not self.avatar_id:
            return None
        
        try:
            response = requests.get(
                f"{self.base_url}/api/wallet/avatar/{self.avatar_id}/wallets",
                headers={"Authorization": f"Bearer {self.token}"},
                verify=self.verify_ssl,
                timeout=10
            )
            data = response.json()
            if not data.get("isError") and data.get("result"):
                # Extract first wallet address
                for provider, wallets in data["result"].items():
                    if wallets and len(wallets) > 0:
                        self.wallet_address = wallets[0].get("address")
                        if self.wallet_address:
                            print(f"✅ Wallet: {self.wallet_address}")
                        break
            return data
        except Exception as e:
            print(f"⚠️  Error getting wallets: {e}")
            return None
    
    def get_karma_stats(self):
        """Get karma statistics"""
        if not self.token or not self.avatar_id:
            return None
        
        try:
            response = requests.get(
                f"{self.base_url}/api/karma/get-karma-stats/{self.avatar_id}",
                headers={"Authorization": f"Bearer {self.token}"},
                verify=self.verify_ssl,
                timeout=10
            )
            return response.json()
        except Exception as e:
            print(f"⚠️  Error getting karma: {e}")
            return None
    
    def add_karma(self, karma_type="Helpful", source_title="Task Completion"):
        """Add karma after successful task"""
        if not self.token or not self.avatar_id:
            return None
        
        try:
            response = requests.post(
                f"{self.base_url}/api/karma/add-karma-to-avatar/{self.avatar_id}",
                headers={"Authorization": f"Bearer {self.token}"},
                json={
                    "karmaType": karma_type,
                    "karmaSourceType": "App",
                    "karamSourceTitle": source_title,
                    "karmaSourceDesc": "A2A task completion"
                },
                verify=self.verify_ssl,
                timeout=10
            )
            return response.json()
        except Exception as e:
            print(f"⚠️  Error adding karma: {e}")
            return None













