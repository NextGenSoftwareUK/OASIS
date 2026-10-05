"""
OASIS Web4 Python SDK

Official Python client for the OASIS Web4 API - Decentralized avatar management,
karma system, NFTs, and cross-provider data storage.
"""

__version__ = "1.0.0"

from .client import OASISWeb4Client
from .config import Config
from .models import *

__all__ = [
    "OASISWeb4Client",
    "Config",
    "OASISResult",
    "Avatar",
    "Karma",
    "NFT",
    "Provider",
]
