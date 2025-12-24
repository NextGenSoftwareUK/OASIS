# MNEE Hackathon Submission: Autonomous AI Agent Payment Network

**Track:** AI and Agent Payments  
**Hackathon:** MNEE $50,000 Hackathon - Building the Future of Programmable Money  
**Date:** December 2025

---

## 🎯 Project Overview

An autonomous payment network where AI agents discover each other via A2A Protocol, negotiate services, execute tasks, and pay each other using **MNEE stablecoin**—all without human intervention.

### Key Features

- ✅ **Agent Discovery** - Agents discover each other via A2A Protocol Agent Cards
- ✅ **Autonomous Payments** - Agents pay each other using MNEE stablecoin via OASIS Wallet API
- ✅ **Trust System** - OASIS Karma API tracks agent reputation
- ✅ **Multi-Chain Support** - Works with Ethereum, Solana, and more via OASIS
- ✅ **Testing Mode** - Uses SOL on Solana devnet for testing (switch to MNEE for production)

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│         Autonomous AI Agent Payment Network             │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Agent A (Data Analyzer)                               │
│    ├─ OASIS Avatar ID                                   │
│    ├─ OASIS Wallet (MNEE balance)                      │
│    └─ A2A Agent Card                                    │
│         │                                                │
│         │ 1. Discovers Agent B via A2A                  │
│         │ 2. Negotiates payment (0.01 MNEE)            │
│         │ 3. Sends task via A2A invokeTask              │
│         ▼                                                │
│  Agent B (Image Generator)                              │
│    ├─ OASIS Avatar ID                                   │
│    ├─ OASIS Wallet (MNEE balance)                      │
│    └─ A2A Agent Card                                    │
│         │                                                │
│         │ 4. Completes task                             │
│         │ 5. Receives payment via OASIS Wallet          │
│         │ 6. Earns karma via OASIS Karma API            │
│         ▼                                                │
│  OASIS Infrastructure                                   │
│    ├─ Avatar API (Identity)                             │
│    ├─ Wallet API (MNEE Payments)                       │
│    ├─ A2A Protocol (Communication)                     │
│    └─ Karma API (Trust)                                │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 🚀 Quick Start

### Prerequisites

1. **Python 3.8+**
2. **OASIS API** running (see setup below)
3. **Solana Devnet** (for testing with SOL) or **MNEE Contract Address** (for production)

### Installation

```bash
# Clone or navigate to project
cd /Volumes/Storage/OASIS_CLEAN/mnee-hackathon-submission

# Create virtual environment
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt
```

### Configuration

Edit `config.py`:

```python
# OASIS API Configuration
OASIS_API_URL = "https://api.oasisweb4.com"  # Or localhost for dev
OASIS_VERIFY_SSL = True

# MNEE Configuration
MNEE_CONTRACT_ADDRESS = "0x..."  # Get from hackathon organizers
ETHEREUM_CHAIN_ID = 1  # Mainnet (or 5 for Goerli testnet)

# Agent Configuration
AGENT_DISCOVERY_PORT = 8080
MIN_KARMA_THRESHOLD = 50  # Minimum karma for agent discovery
```

### Run Demo

```bash
# Terminal 1: Start Data Analyzer Agent
python demo/data_analyzer_agent.py

# Terminal 2: Start Image Generator Agent
python demo/image_generator_agent.py

# Terminal 3: Run Demo Script
python demo/run_demo.py
```

---

## 📁 Project Structure

```
mnee-hackathon-submission/
├── README.md                    # This file
├── requirements.txt             # Python dependencies
├── config.py                    # Configuration
├── src/
│   ├── core/
│   │   ├── oasis_client.py     # OASIS API client
│   │   ├── a2a_client.py       # A2A Protocol client
│   │   ├── agent_discovery.py  # Agent discovery system
│   │   └── payment_flow.py    # MNEE payment flow
│   ├── agents/
│   │   ├── base_agent.py       # Base agent class
│   │   ├── data_analyzer.py    # Data analyzer agent
│   │   └── image_generator.py  # Image generator agent
│   └── utils/
│       ├── agent_card.py       # Agent Card generator
│       └── karma_filter.py     # Karma-based filtering
├── demo/
│   ├── data_analyzer_agent.py  # Demo: Data analyzer
│   ├── image_generator_agent.py # Demo: Image generator
│   └── run_demo.py             # End-to-end demo
├── tests/
│   └── test_payment_flow.py    # Unit tests
└── docs/
    ├── API.md                  # API documentation
    └── DEMO.md                 # Demo guide
```

---

## 🔧 Core Components

### 1. OASIS Client (`src/core/oasis_client.py`)

Handles OASIS API integration:
- Avatar registration and authentication
- Wallet generation and management
- MNEE payment processing
- Karma tracking

### 2. A2A Client (`src/core/a2a_client.py`)

Handles A2A Protocol communication:
- Agent Card generation
- Agent discovery
- Task invocation
- Message handling

### 3. Agent Discovery (`src/core/agent_discovery.py`)

Discovers agents with:
- Capability matching
- Karma filtering
- Price negotiation
- Trust verification

### 4. Payment Flow (`src/core/payment_flow.py`)

Manages MNEE payments:
- Payment negotiation
- Escrow handling
- Transaction processing
- Payment verification

---

## 💡 Usage Examples

### Register an Agent

```python
from src.core.oasis_client import OASISClient
from src.core.a2a_client import A2AClient

# Initialize clients
oasis = OASISClient()
a2a = A2AClient()

# Register agent with OASIS
avatar = oasis.register_avatar(
    username="data_analyzer_001",
    email="agent@example.com",
    password="secure_key"
)

# Generate wallet for MNEE
wallet = oasis.generate_wallet(provider_type="EthereumOASIS")

# Create A2A Agent Card
agent_card = a2a.create_agent_card(
    avatar_id=avatar["id"],
    name="Data Analyzer Agent",
    capabilities=["analyzeMarketData"],
    endpoint="https://agent.example.com/a2a"
)
```

### Discover and Pay an Agent

```python
from src.core.agent_discovery import AgentDiscovery
from src.core.payment_flow import PaymentFlow

discovery = AgentDiscovery(oasis, a2a)
payment_flow = PaymentFlow(oasis)

# Discover agent with capability
agents = discovery.discover_agents(
    capability="analyzeMarketData",
    min_karma=50,
    max_price="0.01 MNEE"
)

# Select agent
agent = agents[0]

# Execute task and pay
result = payment_flow.execute_and_pay(
    requester_avatar_id=my_avatar_id,
    provider_agent=agent,
    task={"type": "analyzeMarketData", "symbol": "BTC/USD"},
    payment_amount="0.01"
)
```

---

## 🧪 Testing

```bash
# Run tests
pytest tests/

# Run with coverage
pytest --cov=src tests/
```

---

## 📊 Demo Scenarios

### Scenario 1: Market Data Analysis

1. **Trading Bot Agent** needs market analysis
2. Discovers **Data Analyzer Agent** via A2A Protocol
3. Negotiates: "Analyze BTC/USD, pay 0.01 MNEE"
4. **Data Analyzer** completes analysis, returns results
5. **Trading Bot** automatically pays **Data Analyzer** via OASIS Wallet API (MNEE)
6. Both agents earn karma for successful transaction

### Scenario 2: Image Generation

1. **Content Creator Agent** needs an image
2. Discovers **Image Generator Agent** via A2A Protocol
3. Negotiates: "Generate logo, pay 0.05 MNEE"
4. **Image Generator** creates image, returns file
5. **Content Creator** automatically pays **Image Generator** via OASIS Wallet API (MNEE)
6. Both agents earn karma

---

## 🏆 Why This Wins

- ✅ **Real-world utility:** Agents can actually pay for services today
- ✅ **Technical innovation:** Combines A2A Protocol + OASIS + MNEE
- ✅ **Scalable:** Works with any number of agents
- ✅ **Trust system:** Karma-based reputation prevents bad actors
- ✅ **Zero gas fees:** Leverages MNEE's 1Sat Ordinals

---

## 📚 Documentation

- [API Documentation](docs/API.md)
- [Demo Guide](docs/DEMO.md)
- [OASIS API Reference](../../Docs/Devs/API%20Documentation/)
- [A2A Protocol Spec](../../A2A/specification/)

---

## 🔗 Links

- **MNEE Hackathon:** https://mnee-eth.devpost.com/
- **OASIS Platform:** https://oasisplatform.world/
- **A2A Protocol:** https://a2a-protocol.org/

---

## 📝 License

MIT License - See LICENSE file for details

---

## 👥 Team

Built for the MNEE Hackathon by the OASIS team.

---

**Let's build the future of programmable money! 🚀**

