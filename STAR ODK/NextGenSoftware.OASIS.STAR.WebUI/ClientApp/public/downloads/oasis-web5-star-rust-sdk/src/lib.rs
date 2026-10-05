//! # OASIS Web5 STAR Rust SDK
//!
//! Official Rust client for the OASIS Web5 STAR API - Gamification, OAPPs,
//! missions, quests, STARNET, and metaverse functionality.
//!
//! ## Quick Start
//!
//! ```rust
//! use oasis_web5_star_client::{OASISWeb5STARClient, Config};
//!
//! #[tokio::main]
//! async fn main() -> Result<(), Box<dyn std::error::Error>> {
//!     let config = Config::new("http://localhost:50564/api");
//!     let client = OASISWeb5STARClient::new(config);
//!
//!     // Ignite STAR
//!     let status = client.ignite_star().await?;
//!     println!("STAR ignited: {}", status.result.unwrap().is_ignited);
//!
//!     // Get all OAPPs
//!     let oapps = client.get_all_oapps().await?;
//!     println!("Total OAPPs: {}", oapps.result.unwrap().len());
//!
//!     Ok(())
//! }
//! ```

pub mod client;
pub mod types;
pub mod error;
pub mod config;

pub use client::OASISWeb5STARClient;
pub use config::Config;
pub use types::*;
pub use error::{Error, Result};
