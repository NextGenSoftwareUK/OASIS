# A2A Protocol Demo Proposal

## Executive Summary

This document outlines a comprehensive demo showcasing the **Agent2Agent (A2A) Protocol** integrated with **OASIS infrastructure**. The demo will demonstrate real-world agent collaboration, identity management, trust systems, and payment flows.

---

## Demo Overview

### Core Concept: "Agentic Marketplace"

A multi-agent system where specialized agents collaborate to solve complex tasks, showcasing:
- **Agent Discovery** via A2A Agent Cards
- **Cross-Agent Communication** using A2A protocol
- **Identity & Trust** via OASIS Avatar & Karma systems
- **Payments** via OASIS multi-chain wallets
- **Task Orchestration** across multiple agents

---

## Demo Scenario: "Travel Planning Agent Network"

### Scenario Description

A user wants to plan a vacation. Instead of one monolithic agent, multiple specialized agents collaborate:

1. **Travel Coordinator Agent** (Orchestrator)
   - Receives user request
   - Discovers and coordinates other agents
   - Manages the overall workflow

2. **Flight Booking Agent**
   - Specialized in finding and booking flights
   - Integrates with flight APIs
   - Returns flight options and prices

3. **Hotel Booking Agent**
   - Specialized in hotel searches
   - Integrates with hotel APIs
   - Returns hotel recommendations

4. **Activity Planning Agent**
   - Finds local activities and attractions
   - Provides recommendations based on preferences
   - Returns activity suggestions

5. **Budget Advisor Agent**
   - Analyzes total costs
   - Provides budget recommendations
   - Calculates payments between agents

### Flow Diagram

```
User Request
    ↓
Travel Coordinator Agent (OASIS Avatar)
    ↓
├──→ Discovers Flight Agent (via A2A Agent Card)
│    ├── Executes flight search task (A2A invokeTask)
│    ├── Receives flight options
│    └── Payment processed (OASIS Wallet)
│
├──→ Discovers Hotel Agent (via A2A Agent Card)
│    ├── Executes hotel search task (A2A invokeTask)
│    ├── Receives hotel options
│    └── Payment processed (OASIS Wallet)
│
├──→ Discovers Activity Agent (via A2A Agent Card)
│    ├── Executes activity search task (A2A invokeTask)
│    ├── Receives activity recommendations
│    └── Payment processed (OASIS Wallet)
│
└──→ Sends data to Budget Agent
     ├── Analyzes total costs
     ├── Generates budget report
     └── Karma updated (OASIS Karma API)
         ↓
    Final Travel Plan (Artifact)
```

---

## Technical Architecture

### Component Breakdown

#### 1. A2A Protocol Layer
- **JSON-RPC 2.0** endpoints for each agent
- **Agent Cards** describing capabilities
- **Task management** with lifecycle states
- **Message passing** between agents
- **Artifact generation** for deliverables

#### 2. OASIS Integration Layer
- **Avatar Registration** for each agent
- **Multi-chain Wallets** (Ethereum, Solana, Arbitrum)
- **Karma System** for trust tracking
- **Payment Processing** via Wallet API

#### 3. Agent Implementation
- **Python-based** agents using `a2a-sdk`
- **Flask/FastAPI** servers for HTTP endpoints
- **LLM Integration** (optional) for intelligent responses
- **External API Integration** for real data

---

## Demo Features to Showcase

### 1. Agent Discovery
**What:** Travel Coordinator discovers specialized agents
**How:** 
- Agents publish Agent Cards with capabilities
- Coordinator queries discovery service
- Filters by skills, karma scores, availability

**Code Example:**
```python
# Discover agents with "flight_booking" skill
discovered_agents = discover_agents(
    capability="flight_booking",
    min_karma=50  # Trust threshold
)

# Get Agent Card
agent_card = get_agent_card(discovered_agents[0]['endpoint'])
```

### 2. Task Delegation
**What:** Coordinator delegates tasks to specialized agents
**How:**
- Coordinator sends A2A `invokeTask` request
- Specialized agent processes task
- Returns task with artifacts

**A2A Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "message.send",
  "params": {
    "message": {
      "messageId": "msg-001",
      "role": "user",
      "parts": [{
        "text": "Find flights from NYC to Paris for dates 2025-07-15 to 2025-07-20"
      }]
    }
  },
  "id": 1
}
```

### 3. Payment Flow
**What:** Agents receive payment for completed tasks
**How:**
- Task completion triggers payment
- OASIS Wallet API processes transaction
- Payment recorded on blockchain

**Code:**
```python
if task.status == "completed":
    # Pay agent for service
    payment = oasis_client.send_payment(
        to_avatar_id=agent_avatar_id,
        amount=0.001,  # ETH
        description=f"Payment for {task.taskType}"
    )
```

### 4. Trust & Karma
**What:** Agents build reputation through successful tasks
**How:**
- Successful task completion adds karma
- Karma score visible in Agent Card
- Coordinator filters agents by karma threshold

**Code:**
```python
# Update karma after successful task
oasis_client.add_karma(
    karma_type="Helpful",
    source_title="Completed flight booking task",
    source_desc="Successfully found and booked flights"
)
```

### 5. Multi-Turn Conversation
**What:** Agents ask for clarification when needed
**How:**
- Agent responds with `input-required` task state
- User provides additional information
- Task continues with new context

**Task State:**
```json
{
  "id": "task-123",
  "status": {
    "state": "input-required",
    "message": {
      "role": "agent",
      "parts": [{
        "text": "What is your preferred departure time?"
      }]
    }
  }
}
```

### 6. Streaming Responses
**What:** Real-time task progress updates
**How:**
- Agent streams progress via Server-Sent Events (SSE)
- Client receives incremental updates
- Final artifact delivered when complete

### 7. Artifact Generation
**What:** Structured outputs from agents
**How:**
- Agents generate artifacts (documents, data, files)
- Artifacts linked to tasks
- Can be referenced in follow-up tasks

**Artifact Example:**
```json
{
  "artifactId": "artifact-flight-options-001",
  "name": "Flight Options",
  "description": "Available flights for NYC to Paris",
  "parts": [{
    "data": {
      "flights": [
        {
          "airline": "Air France",
          "price": "$850",
          "departure": "2025-07-15 10:00",
          "arrival": "2025-07-15 22:30"
        }
      ]
    }
  }]
}
```

---

## Implementation Plan

### Phase 1: Foundation (Week 1)
- [ ] Set up development environment
- [ ] Register OASIS avatars for all agents
- [ ] Generate wallets for each agent
- [ ] Create basic A2A server skeleton
- [ ] Implement Agent Card generation

### Phase 2: Core Agents (Week 2)
- [ ] Implement Travel Coordinator Agent
- [ ] Implement Flight Booking Agent
- [ ] Implement Hotel Booking Agent
- [ ] Implement Activity Planning Agent
- [ ] Implement Budget Advisor Agent

### Phase 3: Integration (Week 3)
- [ ] Integrate OASIS Wallet API
- [ ] Integrate OASIS Karma API
- [ ] Implement agent discovery
- [ ] Implement payment flows
- [ ] Implement trust filtering

### Phase 4: Advanced Features (Week 4)
- [ ] Add streaming support (SSE)
- [ ] Implement multi-turn conversations
- [ ] Add artifact generation
- [ ] Implement error handling
- [ ] Add logging and monitoring

### Phase 5: Demo Preparation (Week 5)
- [ ] Create demo script
- [ ] Record demo video
- [ ] Create presentation slides
- [ ] Write documentation
- [ ] Test end-to-end flow

---

## Code Structure

```
a2a-oasis-demo/
├── agents/
│   ├── coordinator/
│   │   ├── agent.py              # Main agent logic
│   │   ├── a2a_handler.py        # A2A protocol handler
│   │   └── oasis_client.py       # OASIS API client
│   ├── flight/
│   │   ├── agent.py
│   │   ├── a2a_handler.py
│   │   ├── flight_api.py         # External flight API integration
│   │   └── oasis_client.py
│   ├── hotel/
│   ├── activity/
│   └── budget/
├── shared/
│   ├── agent_card.py             # Agent Card utilities
│   ├── discovery.py              # Agent discovery service
│   └── utils.py                  # Common utilities
├── demo/
│   ├── demo_script.py            # Demo orchestration
│   ├── scenarios/                # Pre-defined scenarios
│   └── presentation/             # Demo materials
├── tests/
│   ├── test_agents.py
│   ├── test_integration.py
│   └── test_oasis.py
├── docs/
│   ├── README.md
│   ├── ARCHITECTURE.md
│   └── API.md
├── requirements.txt
└── docker-compose.yml            # Run all agents locally
```

---

## Demo Scenarios

### Scenario 1: Simple Task (5 minutes)
**Goal:** Show basic agent communication
1. User requests: "Find flights from NYC to Paris"
2. Coordinator discovers Flight Agent
3. Flight Agent processes request
4. Returns flight options
5. Payment processed
6. Karma updated

### Scenario 2: Complex Multi-Agent Task (10 minutes)
**Goal:** Show agent orchestration
1. User requests: "Plan a 5-day vacation to Paris"
2. Coordinator discovers all agents
3. Parallel task execution:
   - Flight Agent finds flights
   - Hotel Agent finds hotels
   - Activity Agent finds activities
4. Budget Agent analyzes costs
5. Coordinator assembles final plan
6. All payments processed
7. Karma updated for all agents

### Scenario 3: Trust-Based Filtering (5 minutes)
**Goal:** Show karma/trust system
1. User requests: "Find a luxury hotel"
2. Coordinator filters agents by karma
3. Only agents with karma > 75 are considered
4. High-karma agent selected
5. Task completed
6. Karma increased

### Scenario 4: Multi-Turn Conversation (5 minutes)
**Goal:** Show interactive task flow
1. User requests: "Book a flight"
2. Flight Agent asks: "What are your preferred dates?"
3. User responds: "July 15-20"
4. Flight Agent asks: "What's your budget?"
5. User responds: "$1000"
6. Flight Agent finds options within budget
7. Task completed

---

## Technology Stack

### Core Technologies
- **Python 3.10+** - Primary language
- **a2a-sdk** - A2A protocol SDK
- **Flask/FastAPI** - HTTP server framework
- **requests** - HTTP client for OASIS API

### OASIS Integration
- **OASIS REST API** - Avatar, Wallet, Karma APIs
- **Multi-chain support** - Ethereum, Solana, Arbitrum

### Optional Enhancements
- **LangChain/LangGraph** - LLM agent framework
- **OpenAI/Anthropic API** - LLM integration
- **Redis** - Agent discovery cache
- **PostgreSQL** - Task history storage

---

## Success Metrics

### Functional Metrics
- ✅ All agents register with OASIS
- ✅ Agent Cards are A2A-compliant
- ✅ Agents can discover each other
- ✅ Tasks are successfully delegated
- ✅ Payments process correctly
- ✅ Karma updates after tasks
- ✅ Multi-turn conversations work
- ✅ Artifacts are generated correctly

### Demo Quality Metrics
- ✅ Demo runs smoothly without errors
- ✅ Clear visual feedback on progress
- ✅ Easy to understand for non-technical audience
- ✅ Showcases key value propositions
- ✅ Demonstrates real-world use case

---

## Next Steps

1. **Review & Approve** this proposal
2. **Set up repository** for demo code
3. **Create project board** with tasks
4. **Assign developers** to agents
5. **Begin Phase 1** implementation

---

## Resources

- **A2A Protocol Docs:** `/Volumes/Storage/OASIS_CLEAN/A2A/docs/`
- **A2A Specification:** `/Volumes/Storage/OASIS_CLEAN/A2A/specification/`
- **OASIS Integration Guide:** `/Volumes/Storage/OASIS_CLEAN/A2A/docs/oasis-integration/`
- **OASIS Use Cases:** `/Volumes/Storage/OASIS_CLEAN/Docs/OASIS_JAM_CONCRETE_USE_CASES.md`
- **A2A Python SDK:** https://github.com/a2aproject/a2a-python
- **A2A Protocol Site:** https://a2a-protocol.org/

---

**Last Updated:** January 2026  
**Status:** Proposal - Ready for Review













