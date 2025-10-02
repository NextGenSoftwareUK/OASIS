# OASIS Web4 Rust SDK

Official Rust client for the OASIS Web4 API - Decentralized avatar management, karma system, NFTs, and cross-provider data storage.

## 🦀 Features

- **Type-safe API**: Full Rust type safety with `serde` serialization
- **Async/Await**: Built on `tokio` and `reqwest` for async operations
- **Comprehensive**: All Web4 API endpoints supported
- **Well-documented**: Complete API documentation and examples
- **Error handling**: Robust error types with `thiserror`

## 🚀 Installation

Add to your `Cargo.toml`:

```toml
[dependencies]
oasis-web4-client = "1.0"
tokio = { version = "1", features = ["full"] }
```

## 📖 Quick Start

### Basic Usage

```rust
use oasis_web4_client::{OASISWeb4Client, Config};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    // Create client
    let config = Config::new("http://localhost:5000/api");
    let mut client = OASISWeb4Client::new(config);

    // Authenticate
    let auth_result = client.authenticate("holochain", None).await?;
    println!("Logged in as: {}", auth_result.result.unwrap().avatar.username);

    Ok(())
}
```

### Authentication

```rust
use std::collections::HashMap;

// Simple authentication
let result = client.authenticate("ethereum", None).await?;

// With credentials
let mut credentials = HashMap::new();
credentials.insert("username".to_string(), "user@example.com".to_string());
credentials.insert("password".to_string(), "password123".to_string());

let result = client.authenticate("holochain", Some(credentials)).await?;

// Access token is automatically set
if let Some(auth) = result.result {
    println!("Token: {}", auth.token);
}
```

### Avatar Management

```rust
use oasis_web4_client::{CreateAvatarRequest, UpdateAvatarRequest};

// Get avatar
let avatar = client.get_avatar("avatar-id").await?;

// Create avatar
let create_req = CreateAvatarRequest {
    username: "john_doe".to_string(),
    email: "john@example.com".to_string(),
    password: "secure-password".to_string(),
    first_name: Some("John".to_string()),
    last_name: Some("Doe".to_string()),
    accept_terms: true,
};

let new_avatar = client.create_avatar(&create_req).await?;

// Update avatar
let update_req = UpdateAvatarRequest {
    bio: Some("Rust developer".to_string()),
    image: Some("https://example.com/avatar.jpg".to_string()),
    ..Default::default()
};

let updated = client.update_avatar("avatar-id", &update_req).await?;

// Search avatars
let results = client.search_avatars("john").await?;
```

### Karma System

```rust
use oasis_web4_client::AddKarmaRequest;

// Get karma
let karma = client.get_karma("avatar-id").await?;
if let Some(k) = karma.result {
    println!("Total karma: {}", k.total);
    println!("Rank: {:?}", k.rank);
}

// Add karma
let add_karma = AddKarmaRequest {
    amount: 100,
    reason: "Completed quest".to_string(),
    karma_type: Some("QuestCompleted".to_string()),
    karma_source_type: None,
};

client.add_karma("avatar-id", &add_karma).await?;

// Get karma history
let history = client.get_karma_history("avatar-id", 50).await?;

// Get leaderboard
let leaderboard = client.get_karma_leaderboard("month", 100).await?;
```

### NFT Operations

```rust
use oasis_web4_client::MintNFTRequest;
use std::collections::HashMap;

// Get NFTs
let nfts = client.get_nfts("avatar-id").await?;

// Mint NFT
let mut metadata = HashMap::new();
metadata.insert(
    "attributes".to_string(),
    serde_json::json!([{"trait_type": "Rarity", "value": "Legendary"}])
);

let mint_req = MintNFTRequest {
    name: "My Awesome NFT".to_string(),
    description: "A unique digital asset".to_string(),
    image_url: "https://example.com/nft.png".to_string(),
    collection: Some("OASIS Collection".to_string()),
    price: Some(1000.0),
    metadata: Some(metadata),
    blockchain: Some("ethereum".to_string()),
};

let nft = client.mint_nft("avatar-id", &mint_req).await?;

// Transfer NFT
client.transfer_nft("nft-id", "recipient-avatar-id").await?;

// Burn NFT
client.burn_nft("nft-id").await?;
```

### Provider Management

```rust
// Get available providers
let providers = client.get_available_providers().await?;

// Get current provider
let current = client.get_current_provider().await?;

// Switch provider
client.switch_provider("ipfs").await?;
```

### Messaging

```rust
// Get messages
let messages = client.get_chat_messages("chat-id", 100).await?;

// Send message
let message = client.send_message(
    "chat-id",
    "avatar-id",
    "Hello, world!"
).await?;
```

### Social Features

```rust
// Get friends
let friends = client.get_friends("avatar-id").await?;

// Add friend
client.add_friend("avatar-id", "friend-avatar-id").await?;

// Remove friend
client.remove_friend("avatar-id", "friend-avatar-id").await?;
```

## 🔧 Configuration

### Advanced Configuration

```rust
use std::time::Duration;

let config = Config::new("https://api.oasis.network")
    .with_timeout(Duration::from_secs(60))
    .with_debug(true);

let client = OASISWeb4Client::new(config);
```

### Environment Variables

```bash
export OASIS_WEB4_API_URL=http://localhost:5000/api
```

## 🔒 Error Handling

All methods return `Result<OASISResult<T>, Error>`:

```rust
use oasis_web4_client::Error;

match client.get_avatar("avatar-id").await {
    Ok(result) => {
        if result.is_error {
            eprintln!("API Error: {}", result.message);
        } else if let Some(avatar) = result.result {
            println!("Avatar: {}", avatar.username);
        }
    }
    Err(Error::RequestFailed(e)) => {
        eprintln!("Request failed: {}", e);
    }
    Err(Error::ApiError(msg)) => {
        eprintln!("API error: {}", msg);
    }
    Err(e) => {
        eprintln!("Error: {}", e);
    }
}
```

## 📚 Examples

See the `examples/` directory for complete examples:

- `basic_auth.rs` - Authentication and avatar management
- `karma_system.rs` - Working with karma
- `nft_operations.rs` - Minting and transferring NFTs
- `social_features.rs` - Friends and messaging

Run an example:

```bash
cargo run --example basic_auth
```

## 🧪 Testing

```bash
cargo test
```

## 📄 License

MIT

## 🔗 Links

- [Documentation](https://docs.rs/oasis-web4-client)
- [API Reference](https://docs.oasis.network/web4-api)
- [GitHub](https://github.com/NextGenSoftwareUK/OASIS-API)
- [Crates.io](https://crates.io/crates/oasis-web4-client)

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md).
