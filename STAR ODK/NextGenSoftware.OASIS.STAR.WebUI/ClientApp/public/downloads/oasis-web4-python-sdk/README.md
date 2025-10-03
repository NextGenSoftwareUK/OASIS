# OASIS Web4 Python SDK

Official Python client for the OASIS Web4 API - Decentralized avatar management, karma system, NFTs, and cross-provider data storage.

## 🐍 Features

- **Type-safe**: Full type hints with Pydantic models
- **Pythonic API**: Clean, intuitive Python interface
- **Async ready**: Support for both sync and async operations
- **Comprehensive**: All Web4 API endpoints supported
- **Well-tested**: Extensive test coverage
- **Auto-retry**: Automatic retry logic for failed requests

## 🚀 Installation

```bash
pip install oasis-web4-client
```

## 📖 Quick Start

### Basic Usage

```python
from oasis_web4_client import OASISWeb4Client, Config

# Initialize client
config = Config(api_url="http://localhost:5000/api", debug=True)
client = OASISWeb4Client(config)

# Authenticate
result = client.authenticate("holochain")
print(f"Logged in as: {result.result['avatar']['username']}")

# Get karma
karma = client.get_karma("avatar-id")
print(f"Total karma: {karma.result.total}")
```

### With Context Manager

```python
from oasis_web4_client import OASISWeb4Client

with OASISWeb4Client() as client:
    result = client.authenticate("ethereum")
    # Client automatically logs out when context exits
```

## 🔑 Authentication

```python
# Simple authentication
result = client.authenticate("holochain")

# With credentials
result = client.authenticate("ethereum", {
    "username": "user@example.com",
    "password": "password123"
})

# Set token manually
client.set_auth_token("your-jwt-token")

# Logout
client.logout()
```

## 👤 Avatar Management

```python
from oasis_web4_client.models import CreateAvatarRequest

# Get avatar
avatar = client.get_avatar("avatar-id")

# Get by username
avatar = client.get_avatar_by_username("john_doe")

# Get by email
avatar = client.get_avatar_by_email("john@example.com")

# Create avatar
request = CreateAvatarRequest(
    username="john_doe",
    email="john@example.com",
    password="secure-password",
    first_name="John",
    last_name="Doe",
    accept_terms=True
)
new_avatar = client.create_avatar(request)

# Update avatar
updated = client.update_avatar("avatar-id", {
    "bio": "Python developer",
    "image": "https://example.com/avatar.jpg"
})

# Search avatars
results = client.search_avatars("john")
```

## 🏆 Karma System

```python
from oasis_web4_client.models import AddKarmaRequest

# Get karma
karma = client.get_karma("avatar-id")
print(f"Total: {karma.result.total}")
print(f"Rank: {karma.result.rank}")

# Add karma
request = AddKarmaRequest(
    amount=100,
    reason="Completed quest",
    karma_type="QuestCompleted"
)
client.add_karma("avatar-id", request)

# Get karma history
history = client.get_karma_history("avatar-id", limit=50)

# Get leaderboard
leaderboard = client.get_karma_leaderboard("month", limit=100)
```

## 🎨 NFT Operations

```python
# Get NFTs
nfts = client.get_nfts("avatar-id")

# Mint NFT
nft = client.mint_nft("avatar-id", {
    "name": "My Awesome NFT",
    "description": "A unique digital asset",
    "image_url": "https://example.com/nft.png",
    "collection": "OASIS Collection",
    "price": 1000,
    "metadata": {
        "attributes": [
            {"trait_type": "Rarity", "value": "Legendary"}
        ]
    }
})

# Transfer NFT
client.transfer_nft("nft-id", "recipient-avatar-id")

# Burn NFT
client.burn_nft("nft-id")
```

## 🔌 Provider Management

```python
# Get available providers
providers = client.get_available_providers()

# Get current provider
current = client.get_current_provider()

# Switch provider
client.switch_provider("ipfs")
```

## 💬 Messaging

```python
# Get messages
messages = client.get_chat_messages("chat-id", limit=100)

# Send message
client.send_message("chat-id", "avatar-id", "Hello, world!")
```

## 🌐 Social Features

```python
# Get friends
friends = client.get_friends("avatar-id")

# Add friend
client.add_friend("avatar-id", "friend-avatar-id")

# Remove friend
client.remove_friend("avatar-id", "friend-avatar-id")
```

## ⚙️ Configuration

```python
from oasis_web4_client import Config

config = Config(
    api_url="https://api.oasis.network",
    timeout=60,
    debug=True,
    auto_retry=True,
    max_retries=5
)

client = OASISWeb4Client(config)
```

### Environment Variables

```bash
export OASIS_WEB4_API_URL=http://localhost:5000/api
```

## 🔒 Error Handling

```python
from oasis_web4_client.exceptions import OASISAPIError, OASISAuthError

try:
    result = client.get_avatar("avatar-id")
    if result.is_error:
        print(f"API Error: {result.message}")
    else:
        print(f"Avatar: {result.result.username}")
except OASISAPIError as e:
    print(f"Request failed: {e}")
except OASISAuthError as e:
    print(f"Authentication failed: {e}")
```

## 🧪 Testing

```bash
# Install dev dependencies
pip install -e ".[dev]"

# Run tests
pytest

# With coverage
pytest --cov=oasis_web4_client
```

## 📚 Type Hints

Full type hint support:

```python
from oasis_web4_client import OASISWeb4Client
from oasis_web4_client.models import Avatar, Karma, OASISResult

client: OASISWeb4Client = OASISWeb4Client()
result: OASISResult[Avatar] = client.get_avatar("id")
avatar: Avatar = result.result
```

## 📄 License

MIT

## 🔗 Links

- [PyPI](https://pypi.org/project/oasis-web4-client/)
- [Documentation](https://docs.oasis.network/web4-api)
- [GitHub](https://github.com/NextGenSoftwareUK/OASIS-API)
- [Issue Tracker](https://github.com/NextGenSoftwareUK/OASIS-API/issues)

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md).
