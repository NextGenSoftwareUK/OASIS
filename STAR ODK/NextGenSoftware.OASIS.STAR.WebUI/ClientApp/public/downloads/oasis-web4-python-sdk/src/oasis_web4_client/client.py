"""OASIS Web4 API Client"""

from typing import Optional, Dict, List, Any
import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

from .config import Config
from .models import OASISResult, Avatar, Karma, NFT, Provider, CreateAvatarRequest, AddKarmaRequest
from .exceptions import OASISAPIError, OASISAuthError


class OASISWeb4Client:
    """
    OASIS Web4 API Client
    
    Example:
        >>> client = OASISWeb4Client(Config(api_url="http://localhost:5000/api"))
        >>> result = client.authenticate("holochain")
        >>> print(result.result.avatar.username)
    """
    
    def __init__(self, config: Optional[Config] = None):
        """
        Initialize the OASIS Web4 client.
        
        Args:
            config: Configuration object. If None, uses default configuration.
        """
        self.config = config or Config()
        self.session = requests.Session()
        self.auth_token: Optional[str] = None
        
        # Configure retry strategy
        if self.config.auto_retry:
            retry_strategy = Retry(
                total=self.config.max_retries,
                backoff_factor=1,
                status_forcelist=[408, 429, 500, 502, 503, 504],
            )
            adapter = HTTPAdapter(max_retries=retry_strategy)
            self.session.mount("http://", adapter)
            self.session.mount("https://", adapter)
        
        # Set timeout
        self.session.timeout = self.config.timeout
        
        # Set headers
        self.session.headers.update({"Content-Type": "application/json"})
    
    def _get_headers(self) -> Dict[str, str]:
        """Get headers including auth token if available."""
        headers = {}
        if self.auth_token:
            headers["Authorization"] = f"Bearer {self.auth_token}"
        return headers
    
    def _handle_response(self, response: requests.Response) -> Dict[str, Any]:
        """Handle API response and errors."""
        try:
            response.raise_for_status()
            data = response.json()
            
            if self.config.debug:
                print(f"[OASIS Web4] Response: {response.status_code} {response.url}")
            
            return data
        except requests.exceptions.HTTPError as e:
            raise OASISAPIError(f"HTTP {response.status_code}: {response.text}")
        except requests.exceptions.RequestException as e:
            raise OASISAPIError(f"Request failed: {str(e)}")
    
    def set_auth_token(self, token: str) -> None:
        """Set the authentication token."""
        self.auth_token = token
    
    def clear_auth_token(self) -> None:
        """Clear the authentication token."""
        self.auth_token = None
    
    # Authentication Methods
    def authenticate(self, provider: str, credentials: Optional[Dict[str, Any]] = None) -> OASISResult[Dict[str, Any]]:
        """
        Authenticate with a provider.
        
        Args:
            provider: Provider name (e.g., "holochain", "ethereum")
            credentials: Optional credentials dictionary
            
        Returns:
            OASISResult containing authentication response
        """
        body = {"provider": provider}
        if credentials:
            body.update(credentials)
        
        response = self.session.post(
            f"{self.config.api_url}/avatar/authenticate",
            json=body,
            headers=self._get_headers()
        )
        
        data = self._handle_response(response)
        
        if data.get("result") and data["result"].get("token"):
            self.auth_token = data["result"]["token"]
        
        return OASISResult(**data)
    
    def logout(self) -> OASISResult[bool]:
        """Logout current session."""
        response = self.session.post(
            f"{self.config.api_url}/avatar/logout",
            headers=self._get_headers()
        )
        
        data = self._handle_response(response)
        self.auth_token = None
        
        return OASISResult(**data)
    
    # Avatar Methods
    def get_avatar(self, avatar_id: str) -> OASISResult[Avatar]:
        """Get avatar by ID."""
        response = self.session.get(
            f"{self.config.api_url}/avatar/{avatar_id}",
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def get_avatar_by_username(self, username: str) -> OASISResult[Avatar]:
        """Get avatar by username."""
        response = self.session.get(
            f"{self.config.api_url}/avatar/username/{username}",
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def get_avatar_by_email(self, email: str) -> OASISResult[Avatar]:
        """Get avatar by email."""
        response = self.session.get(
            f"{self.config.api_url}/avatar/email/{email}",
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def create_avatar(self, request: CreateAvatarRequest) -> OASISResult[Avatar]:
        """Create a new avatar."""
        response = self.session.post(
            f"{self.config.api_url}/avatar",
            json=request.dict(exclude_none=True),
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def update_avatar(self, avatar_id: str, updates: Dict[str, Any]) -> OASISResult[Avatar]:
        """Update an avatar."""
        response = self.session.put(
            f"{self.config.api_url}/avatar/{avatar_id}",
            json=updates,
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def delete_avatar(self, avatar_id: str) -> OASISResult[bool]:
        """Delete an avatar."""
        response = self.session.delete(
            f"{self.config.api_url}/avatar/{avatar_id}",
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def search_avatars(self, query: str) -> OASISResult[List[Avatar]]:
        """Search for avatars."""
        response = self.session.get(
            f"{self.config.api_url}/avatar/search",
            params={"q": query},
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    # Karma Methods
    def get_karma(self, avatar_id: str) -> OASISResult[Karma]:
        """Get karma for an avatar."""
        response = self.session.get(
            f"{self.config.api_url}/avatar/{avatar_id}/karma",
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def add_karma(self, avatar_id: str, request: AddKarmaRequest) -> OASISResult[Karma]:
        """Add karma to an avatar."""
        response = self.session.post(
            f"{self.config.api_url}/avatar/{avatar_id}/karma",
            json=request.dict(exclude_none=True),
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def get_karma_history(self, avatar_id: str, limit: int = 50) -> OASISResult[List[Dict[str, Any]]]:
        """Get karma history for an avatar."""
        response = self.session.get(
            f"{self.config.api_url}/avatar/{avatar_id}/karma/history",
            params={"limit": limit},
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def get_karma_leaderboard(self, time_range: str = "all", limit: int = 100) -> OASISResult[List[Avatar]]:
        """Get karma leaderboard."""
        response = self.session.get(
            f"{self.config.api_url}/karma/leaderboard",
            params={"range": time_range, "limit": limit},
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    # NFT Methods
    def get_nfts(self, avatar_id: str) -> OASISResult[List[NFT]]:
        """Get NFTs for an avatar."""
        response = self.session.get(
            f"{self.config.api_url}/nft",
            params={"avatarId": avatar_id},
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def get_nft(self, nft_id: str) -> OASISResult[NFT]:
        """Get a specific NFT."""
        response = self.session.get(
            f"{self.config.api_url}/nft/{nft_id}",
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def mint_nft(self, avatar_id: str, nft_data: Dict[str, Any]) -> OASISResult[NFT]:
        """Mint a new NFT."""
        body = {"avatarId": avatar_id, **nft_data}
        response = self.session.post(
            f"{self.config.api_url}/nft/mint",
            json=body,
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def transfer_nft(self, nft_id: str, to_avatar_id: str) -> OASISResult[NFT]:
        """Transfer an NFT to another avatar."""
        response = self.session.post(
            f"{self.config.api_url}/nft/{nft_id}/transfer",
            json={"toAvatarId": to_avatar_id},
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def burn_nft(self, nft_id: str) -> OASISResult[bool]:
        """Burn an NFT."""
        response = self.session.delete(
            f"{self.config.api_url}/nft/{nft_id}",
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    # Provider Management
    def get_available_providers(self) -> OASISResult[List[Provider]]:
        """Get available providers."""
        response = self.session.get(
            f"{self.config.api_url}/providers",
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def get_current_provider(self) -> OASISResult[Provider]:
        """Get current provider."""
        response = self.session.get(
            f"{self.config.api_url}/providers/current",
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def switch_provider(self, provider_name: str) -> OASISResult[Provider]:
        """Switch to a different provider."""
        response = self.session.post(
            f"{self.config.api_url}/providers/switch",
            json={"provider": provider_name},
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    # Messaging
    def get_chat_messages(self, chat_id: str, limit: int = 100) -> OASISResult[List[Dict[str, Any]]]:
        """Get chat messages."""
        response = self.session.get(
            f"{self.config.api_url}/chat/{chat_id}/messages",
            params={"limit": limit},
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def send_message(self, chat_id: str, avatar_id: str, content: str) -> OASISResult[Dict[str, Any]]:
        """Send a chat message."""
        response = self.session.post(
            f"{self.config.api_url}/chat/messages",
            json={"chatId": chat_id, "avatarId": avatar_id, "content": content},
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    # Social Features
    def get_friends(self, avatar_id: str) -> OASISResult[List[Avatar]]:
        """Get friends for an avatar."""
        response = self.session.get(
            f"{self.config.api_url}/avatar/{avatar_id}/friends",
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def add_friend(self, avatar_id: str, friend_id: str) -> OASISResult[bool]:
        """Add a friend."""
        response = self.session.post(
            f"{self.config.api_url}/avatar/{avatar_id}/friends",
            json={"friendId": friend_id},
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
    
    def remove_friend(self, avatar_id: str, friend_id: str) -> OASISResult[bool]:
        """Remove a friend."""
        response = self.session.delete(
            f"{self.config.api_url}/avatar/{avatar_id}/friends/{friend_id}",
            headers=self._get_headers()
        )
        return OASISResult(**self._handle_response(response))
