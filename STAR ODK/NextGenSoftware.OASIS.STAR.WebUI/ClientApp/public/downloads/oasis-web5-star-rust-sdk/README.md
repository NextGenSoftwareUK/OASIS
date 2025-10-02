# OASIS Web5 STAR Rust SDK

Official Rust client for the OASIS Web5 STAR API - Gamification, OAPPs, missions, quests, STARNET, and metaverse functionality.

## 🌟 Features

- **Type-safe API**: Full Rust type safety with `serde` serialization
- **Async/Await**: Built on `tokio` and `reqwest`
- **STAR Operations**: Ignite, light, evolve STAR
- **OAPPs**: Create and manage decentralized applications
- **Gamification**: Missions, quests, chapters
- **Holons & Zomes**: Plug-and-play components
- **STARNET**: Decentralized network operations

## 🚀 Installation

```toml
[dependencies]
oasis-web5-star-client = "1.0"
tokio = { version = "1", features = ["full"] }
```

## 📖 Quick Start

```rust
use oasis_web5_star_client::{OASISWeb5STARClient, Config};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let config = Config::new("http://localhost:50564/api");
    let client = OASISWeb5STARClient::new(config);

    // Ignite STAR
    let status = client.ignite_star().await?;
    println!("STAR ignited: {}", status.result.unwrap().is_ignited);

    Ok(())
}
```

## 🌐 STAR Operations

```rust
// Ignite STAR
let status = client.ignite_star().await?;

// Get STAR status
let status = client.get_star_status().await?;

// Light STAR
let status = client.light_star().await?;

// Extinguish STAR
client.extinguish_star().await?;
```

## 📱 OAPP Management

```rust
use oasis_web5_star_client::CreateOAPPRequest;

// Get all OAPPs
let oapps = client.get_all_oapps().await?;

// Create OAPP
let request = CreateOAPPRequest {
    name: "My Awesome OAPP".to_string(),
    description: "A decentralized app".to_string(),
    category: "Social".to_string(),
    version: Some("1.0.0".to_string()),
    icon: None,
    template_id: None,
    config: None,
};

let oapp = client.create_oapp(&request).await?;

// Publish OAPP
client.publish_oapp(&oapp.result.unwrap().id).await?;

// Install OAPP
client.install_oapp("oapp-id", "avatar-id").await?;
```

## 🎮 Gamification

### Missions

```rust
// Get all missions
let missions = client.get_all_missions().await?;

// Get specific mission
let mission = client.get_mission("mission-id").await?;

// Start mission
client.start_mission("mission-id", "avatar-id").await?;

// Complete mission
client.complete_mission("mission-id", "avatar-id").await?;
```

### Quests

```rust
// Get all quests
let quests = client.get_all_quests().await?;

// Start quest
client.start_quest("quest-id", "avatar-id").await?;

// Complete quest
client.complete_quest("quest-id", "avatar-id").await?;
```

### Chapters

```rust
let chapters = client.get_all_chapters().await?;
let chapter = client.get_chapter("chapter-id").await?;
```

## 🧩 Holons & Zomes

```rust
// Get all holons
let holons = client.get_all_holons().await?;

// Get all zomes
let zomes = client.get_all_zomes().await?;

// Install zome in OAPP
client.install_zome("zome-id", "oapp-id").await?;
```

## 🔌 STAR Plugins

```rust
// Get all plugins
let plugins = client.get_all_star_plugins().await?;

// Install plugin
client.install_star_plugin("plugin-id").await?;
```

## 🌍 STARNET

```rust
// Join STARNET
let config = serde_json::json!({
    "nodeType": "full",
    "bandwidth": "high"
});

client.join_starnet(Some(config)).await?;

// Get STARNET status
let status = client.get_starnet_status().await?;

// Get nodes
let nodes = client.get_starnet_nodes().await?;
```

## 🔗 Integration with Web4

```rust
use oasis_web4_client::OASISWeb4Client;
use oasis_web5_star_client::OASISWeb5STARClient;

// Create both clients
let web4_client = OASISWeb4Client::new(web4_config);
let mut star_client = OASISWeb5STARClient::new(star_config);

// Authenticate via Web4
let auth = web4_client.authenticate("holochain", None).await?;
if let Some(auth_response) = auth.result {
    star_client.set_auth_token(auth_response.token);
}

// Use STAR features
star_client.ignite_star().await?;
```

## 📚 Examples

See `examples/` for complete examples:

```bash
cargo run --example ignite_star
cargo run --example create_oapp
cargo run --example missions
```

## 🧪 Testing

```bash
cargo test
```

## 📄 License

MIT

## 🔗 Links

- [Documentation](https://docs.rs/oasis-web5-star-client)
- [Web4 Rust SDK](https://crates.io/crates/oasis-web4-client)
- [API Reference](https://docs.oasis.network/web5-star-api)
- [GitHub](https://github.com/NextGenSoftwareUK/OASIS-STAR)
