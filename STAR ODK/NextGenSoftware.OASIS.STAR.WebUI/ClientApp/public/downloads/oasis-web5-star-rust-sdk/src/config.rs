//! Configuration for OASIS Web5 STAR client

use std::time::Duration;

/// Configuration for the OASIS Web5 STAR client
#[derive(Clone, Debug)]
pub struct Config {
    /// API base URL
    pub api_url: String,
    /// Request timeout
    pub timeout: Duration,
    /// Enable debug logging
    pub debug: bool,
}

impl Config {
    /// Create a new configuration with the given API URL
    pub fn new(api_url: impl Into<String>) -> Self {
        Self {
            api_url: api_url.into(),
            timeout: Duration::from_secs(30),
            debug: false,
        }
    }

    /// Set the request timeout
    pub fn with_timeout(mut self, timeout: Duration) -> Self {
        self.timeout = timeout;
        self
    }

    /// Enable debug logging
    pub fn with_debug(mut self, debug: bool) -> Self {
        self.debug = debug;
        self
    }
}

impl Default for Config {
    fn default() -> Self {
        Self::new("http://localhost:50564/api")
    }
}
