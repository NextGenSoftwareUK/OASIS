//! Error types for OASIS Web5 STAR client

use thiserror::Error;

pub type Result<T> = std::result::Result<T, Error>;

#[derive(Error, Debug)]
pub enum Error {
    #[error("HTTP request failed: {0}")]
    RequestFailed(#[from] reqwest::Error),

    #[error("API error: {0}")]
    ApiError(String),

    #[error("Serialization error: {0}")]
    SerializationError(#[from] serde_json::Error),

    #[error("Authentication required")]
    NotAuthenticated,

    #[error("Invalid configuration: {0}")]
    InvalidConfig(String),

    #[error("STAR not ignited")]
    STARNotIgnited,
}
