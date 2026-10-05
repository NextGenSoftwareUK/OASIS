"""
Configuration for MNEE Hackathon Submission
Autonomous AI Agent Payment Network
"""

import os
from dotenv import load_dotenv

load_dotenv()

# ============================================================================
# OASIS API Configuration
# ============================================================================

# OASIS API URL - adjust based on your environment
# For local development (default):
OASIS_API_URL = os.getenv("OASIS_API_URL", "http://localhost:5004")
# For production:
# OASIS_API_URL = "https://api.oasisweb4.com"
# Alternative local ports:
# OASIS_API_URL = "https://localhost:5002"
# OASIS_API_URL = "http://localhost:5000"

# SSL Verification - set to False for self-signed certificates (development only)
# For localhost HTTP, SSL verification is not needed
OASIS_VERIFY_SSL = os.getenv("OASIS_VERIFY_SSL", "False").lower() == "true"

# ============================================================================
# Payment Configuration (Solana Devnet for Testing)
# ============================================================================

# Payment Token Configuration
# Using SOL on Solana devnet for testing (instead of MNEE)
PAYMENT_PROVIDER_TYPE = os.getenv("PAYMENT_PROVIDER_TYPE", "SolanaOASIS")  # SolanaOASIS or EthereumOASIS
PAYMENT_TOKEN_SYMBOL = os.getenv("PAYMENT_TOKEN_SYMBOL", "SOL")  # SOL for testing, MNEE for production

# Solana Network Configuration (for testing)
SOLANA_NETWORK = os.getenv("SOLANA_NETWORK", "devnet")  # devnet, testnet, or mainnet
SOLANA_RPC_URL = os.getenv(
    "SOLANA_RPC_URL",
    "https://api.devnet.solana.com"  # Devnet RPC endpoint
)

# Ethereum/MNEE Configuration (for production)
MNEE_CONTRACT_ADDRESS = os.getenv(
    "MNEE_CONTRACT_ADDRESS",
    "0x0000000000000000000000000000000000000000"  # PLACEHOLDER - UPDATE THIS
)
ETHEREUM_CHAIN_ID = int(os.getenv("ETHEREUM_CHAIN_ID", "1"))  # 1 = Mainnet, 5 = Goerli
ETHEREUM_RPC_URL = os.getenv(
    "ETHEREUM_RPC_URL",
    "https://eth.llamarpc.com"  # Public RPC endpoint
)

# ============================================================================
# Agent Configuration
# ============================================================================

# Agent Discovery Configuration
AGENT_DISCOVERY_PORT = int(os.getenv("AGENT_DISCOVERY_PORT", "8080"))
AGENT_DISCOVERY_HOST = os.getenv("AGENT_DISCOVERY_HOST", "0.0.0.0")

# Trust & Reputation
MIN_KARMA_THRESHOLD = int(os.getenv("MIN_KARMA_THRESHOLD", "50"))
REQUIRED_APPROVALS = int(os.getenv("REQUIRED_APPROVALS", "1"))

# Payment Configuration
DEFAULT_PAYMENT_TOKEN = "MNEE"
DEFAULT_PAYMENT_CHAIN = "Ethereum"
PAYMENT_TIMEOUT_SECONDS = int(os.getenv("PAYMENT_TIMEOUT_SECONDS", "300"))  # 5 minutes

# ============================================================================
# A2A Protocol Configuration
# ============================================================================

# A2A Protocol Settings
A2A_PROTOCOL_VERSION = "2.0"
A2A_ENDPOINT_PATH = "/a2a"

# ============================================================================
# Logging Configuration
# ============================================================================

LOG_LEVEL = os.getenv("LOG_LEVEL", "INFO")
LOG_FORMAT = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"

# ============================================================================
# Development/Testing
# ============================================================================

# Test Mode - uses mock data when True
TEST_MODE = os.getenv("TEST_MODE", "False").lower() == "true"

# Mock MNEE Contract (for testing without real contract)
MOCK_MNEE_CONTRACT = os.getenv("MOCK_MNEE_CONTRACT", "False").lower() == "true"

