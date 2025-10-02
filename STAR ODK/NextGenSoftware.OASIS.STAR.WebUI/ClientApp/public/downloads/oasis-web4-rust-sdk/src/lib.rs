//! # OASIS Web4 Rust SDK
//!
//! Official Rust client for the OASIS Web4 API - Decentralized avatar management,
//! karma system, NFTs, and cross-provider data storage.
//!
//! ## Quick Start
//!
//! ```rust
//! use oasis_web4_client::{OASISWeb4Client, Config};
//!
//! #[tokio::main]
//! async fn main() -> Result<(), Box<dyn std::error::Error>> {
//!     let config = Config::new("http://localhost:5000/api");
//!     let client = OASISWeb4Client::new(config);
//!
//!     // Authenticate
//!     let auth_result = client.authenticate("holochain", None).await?;
//!     println!("Logged in as: {}", auth_result.avatar.username);
//!
//!     // Get karma
//!     let karma = client.get_karma(&auth_result.avatar.id).await?;
//!     println!("Karma: {}", karma.total);
//!
//!     Ok(())
//! }
//! ```

pub mod client;
pub mod types;
pub mod error;
pub mod config;

pub use client::OASISWeb4Client;
pub use config::Config;
pub use types::*;
pub use error::{Error, Result};
