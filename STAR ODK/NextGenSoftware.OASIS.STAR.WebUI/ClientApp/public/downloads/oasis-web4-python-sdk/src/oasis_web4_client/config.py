"""Configuration for OASIS Web4 client"""

from dataclasses import dataclass


@dataclass
class Config:
    """
    Configuration for OASIS Web4 client.
    
    Attributes:
        api_url: Base URL for the OASIS Web4 API
        timeout: Request timeout in seconds
        debug: Enable debug logging
        auto_retry: Enable automatic retries for failed requests
        max_retries: Maximum number of retries
    """
    api_url: str = "http://localhost:5000/api"
    timeout: int = 30
    debug: bool = False
    auto_retry: bool = True
    max_retries: int = 3
