# OASIS IDE: Master Brief

**Date:** January 2026  
**Status:** 📋 Master Specification  
**Purpose:** Complete overview of OASIS IDE project for all teams and agents

---

## 🎯 Project Vision

Build a **full-featured IDE** that combines:
- **Cursor-like AI coding experience** (natural language to code)
- **Native OASIS MCP integration** (100+ tools built-in)
- **Built-in agent system** (A2A Protocol support)
- **OASIS/STAR development tools** (OAPP Builder, NFT Minting, etc.)
- **Interoperable app development** (OAPPs that work everywhere)

**Key Differentiator:** Unlike Cursor (generic IDE with MCP plugin), OASIS IDE has OASIS ecosystem as **first-class citizens**.

---

## 📚 Reference Materials

### Cursor IDE Codebase
- **GitHub:** https://github.com/getcursor/cursor
- **Status:** Open source, 32.1k stars
- **Use for:** Architecture patterns, UI/UX reference, MCP integration examples

### OASIS Documentation
- **Architecture:** `/Docs/OASIS_IDE_ARCHITECTURE.md`
- **MCP Integration:** `/MCP/src/`
- **A2A Protocol:** `/A2A/`
- **STAR API:** `/Docs/Devs/API Documentation/WEB5_STAR_API_Documentation.md`

---

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              OASIS IDE (Electron Application)               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  FRONTEND LAYER (React + TypeScript)                  │  │
│  │  • Monaco Editor                                      │  │
│  │  • File Explorer                                      │  │
│  │  • Terminal (xterm.js)                               │  │
│  │  • Chat Interface                                     │  │
│  │  • OASIS Tools Panels                                 │  │
│  │  • Agent Management UI                                │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  BACKEND LAYER (Node.js/Electron Main Process)        │  │
│  │  • MCP Server Manager                                  │  │
│  │  • OASIS API Client                                    │  │
│  │  • Agent Runtime                                       │  │
│  │  • File System Operations                              │  │
│  │  • Language Server Integration                         │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  INTEGRATION LAYER                                     │  │
│  │  • OASIS API (api.oasisplatform.world)                │  │
│  │  • STAR API (star-api.oasisplatform.world)            │  │
│  │  • MCP Servers (local/remote)                          │  │
│  │  • Agent Registry (SERV)                              │  │
│  │  • LLM APIs (OpenAI, Anthropic, etc.)                 │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 Core Components Overview

### 1. **Core IDE Components**
**Responsibility:** Basic IDE functionality (editor, file system, terminal)  
**Agent Brief:** `OASIS_IDE_BRIEF_CORE_COMPONENTS.md`

**Key Features:**
- Monaco Editor (VS Code's editor)
- File Explorer with workspace management
- Integrated Terminal (xterm.js)
- Git Integration
- Language Server Protocol (LSP) support
- Debugger integration
- Multi-file editing
- Search and replace

### 2. **OASIS MCP Integration**
**Responsibility:** Native MCP server management and tool execution  
**Agent Brief:** `OASIS_IDE_BRIEF_MCP_INTEGRATION.md`

**Key Features:**
- Automatic MCP server startup
- Tool discovery and registry
- Natural language → tool mapping
- Tool execution with progress tracking
- Response visualization
- Error handling and retry
- 100+ OASIS tools pre-configured

### 3. **Agent System (A2A Protocol)**
**Responsibility:** Agent discovery, communication, and runtime  
**Agent Brief:** `OASIS_IDE_BRIEF_AGENT_SYSTEM.md`

**Key Features:**
- Agent registry UI
- Agent discovery (search by capability)
- A2A Protocol client
- Agent-to-agent communication
- Agent marketplace
- Local agent execution
- Agent monitoring dashboard

### 4. **OASIS Development Tools**
**Responsibility:** Specialized OASIS development features  
**Agent Brief:** `OASIS_IDE_BRIEF_DEVELOPMENT_TOOLS.md`

**Key Features:**
- OAPP Builder (visual drag-and-drop)
- NFT Minting UI
- Wallet Manager
- Holon Explorer
- Mission/Quest Creator
- Smart Contract Deployer

### 5. **AI Assistant & Code Completion**
**Responsibility:** AI-powered coding assistance  
**Agent Brief:** `OASIS_IDE_BRIEF_AI_ASSISTANT.md`

**Key Features:**
- Chat interface (like Cursor)
- Code completion (AI-powered)
- Inline suggestions
- Natural language to code
- Context-aware help
- Refactoring suggestions
- Bug detection

### 6. **UI/UX & Frontend Architecture**
**Responsibility:** User interface and experience design  
**Agent Brief:** `OASIS_IDE_BRIEF_UI_UX.md`

**Key Features:**
- Responsive layout system
- Theme support (light/dark)
- Panel management
- Keyboard shortcuts
- Command palette
- Settings UI
- Onboarding flow

---

## 🔧 Technical Stack

### Frontend
- **Framework:** React + TypeScript
- **Editor:** Monaco Editor
- **Terminal:** xterm.js
- **UI Library:** Custom components (or Material-UI)
- **State Management:** Zustand or Redux
- **Build Tool:** Vite or Webpack

### Backend
- **Runtime:** Electron (Main Process)
- **Language:** Node.js + TypeScript
- **IPC:** Electron IPC
- **File System:** Node.js fs module
- **Process Management:** child_process

### External Services
- **OASIS API:** REST API client
- **STAR API:** REST API client
- **MCP Servers:** stdio/HTTP transport
- **LLM APIs:** OpenAI, Anthropic, etc.
- **Language Servers:** LSP protocol

---

## 📋 Implementation Phases

### Phase 1: Foundation (Months 1-2)
**Goal:** Basic IDE with OASIS MCP integration

**Deliverables:**
- Monaco editor working
- File explorer
- Basic terminal
- OASIS MCP server integration
- Chat interface
- Basic AI assistant
- MCP tool execution

**Success Criteria:**
- Can open/edit files
- Can use MCP tools via chat
- Can execute OASIS operations

### Phase 2: Agent System (Months 3-4)
**Goal:** Add A2A Protocol and agent capabilities

**Deliverables:**
- Agent registry UI
- Agent discovery
- A2A Protocol client
- Agent invocation from IDE
- Agent marketplace
- Local agent runtime

**Success Criteria:**
- Can discover agents
- Can invoke agents from chat
- Agents can use MCP tools

### Phase 3: OASIS Development Tools (Months 5-6)
**Goal:** Add specialized OASIS development features

**Deliverables:**
- OAPP Builder UI
- NFT Minting UI
- Wallet Manager
- Holon Explorer
- Mission Creator
- Smart Contract Deployer

**Success Criteria:**
- Can create OAPPs visually
- Can mint NFTs from IDE
- Can manage wallets
- Can deploy smart contracts

### Phase 4: Advanced Features (Months 7-8)
**Goal:** Polish and advanced capabilities

**Deliverables:**
- Code completion with OASIS awareness
- Inline MCP tool suggestions
- Agent collaboration features
- OAPP templates library
- Cross-chain development tools
- Performance optimizations

**Success Criteria:**
- IDE feels as polished as Cursor
- OASIS features are seamless
- Agents enhance development workflow

---

## 🎯 Key Requirements

### Functional Requirements

1. **Code Editing**
   - Syntax highlighting for all major languages
   - IntelliSense (code completion)
   - Multi-cursor editing
   - Find/replace with regex
   - Code folding
   - Bracket matching

2. **OASIS Integration**
   - Native MCP tool access (no configuration)
   - OASIS API client built-in
   - STAR API client built-in
   - Automatic authentication
   - Tool discovery and execution

3. **Agent System**
   - Agent discovery via A2A Protocol
   - Agent invocation from chat
   - Agent-to-agent communication
   - Agent marketplace
   - Local agent execution

4. **Development Tools**
   - Visual OAPP Builder
   - NFT Minting UI
   - Wallet Manager
   - Holon Explorer
   - Mission/Quest Creator

5. **AI Assistant**
   - Natural language chat
   - Code generation
   - Code explanation
   - Refactoring suggestions
   - Bug detection

### Non-Functional Requirements

1. **Performance**
   - Fast startup (< 3 seconds)
   - Responsive UI (60 FPS)
   - Efficient memory usage
   - Quick file operations

2. **Security**
   - Secure authentication
   - Encrypted data storage
   - Safe file system access
   - Secure agent communication

3. **Usability**
   - Intuitive UI
   - Keyboard shortcuts
   - Customizable layout
   - Helpful error messages

4. **Compatibility**
   - Windows, macOS, Linux
   - All major programming languages
   - Git integration
   - Extension support (future)

---

## 🔑 Key Differentiators from Cursor

1. **Native OASIS Integration**
   - MCP tools available by default
   - No configuration needed
   - OASIS API knowledge built-in

2. **Built-in Agent System**
   - A2A Protocol support
   - Agent marketplace
   - Agent collaboration

3. **OASIS Development Tools**
   - OAPP Builder
   - NFT Minting
   - Wallet Manager
   - Holon Explorer

4. **Interoperability Focus**
   - Build OAPPs that work everywhere
   - Cross-chain development
   - Universal identity

5. **Holonic Architecture Awareness**
   - Understands holons, zomes, OAPPs
   - Visualizes relationships
   - Manages dependencies

---

## 📊 Success Metrics

### User Adoption
- Number of active users
- Daily active users
- Projects created
- OAPPs deployed

### Feature Usage
- MCP tool calls per user
- Agent invocations
- OAPP creations
- NFT mints from IDE

### Developer Experience
- Time to first OAPP
- Code completion accuracy
- Agent response quality
- User satisfaction scores

---

## 🚀 MVP Scope (3 Months)

**Minimum Viable Product:**

1. Monaco editor
2. File explorer
3. Basic terminal
4. Chat interface with AI
5. OASIS MCP integration (10 core tools)
6. Basic agent discovery
7. Simple OAPP template

**Tech Stack:**
- Electron
- React
- TypeScript
- Monaco Editor
- OpenAI API
- OASIS MCP Server

**Team:**
- 2-3 full-stack developers
- 1 UI/UX designer
- 1 OASIS domain expert

---

## 📚 Agent Briefs

Each agent/team should receive a specific brief:

1. **Core Components Team:** `OASIS_IDE_BRIEF_CORE_COMPONENTS.md`
2. **MCP Integration Team:** `OASIS_IDE_BRIEF_MCP_INTEGRATION.md`
3. **Agent System Team:** `OASIS_IDE_BRIEF_AGENT_SYSTEM.md`
4. **Development Tools Team:** `OASIS_IDE_BRIEF_DEVELOPMENT_TOOLS.md`
5. **AI Assistant Team:** `OASIS_IDE_BRIEF_AI_ASSISTANT.md`
6. **UI/UX Team:** `OASIS_IDE_BRIEF_UI_UX.md`

---

## 🔗 Related Documents

- **Full Architecture:** `/Docs/OASIS_IDE_ARCHITECTURE.md`
- **MCP Documentation:** `/MCP/README.md`
- **A2A Protocol:** `/A2A/OASIS_A2A_PROTOCOL_DOCUMENTATION.md`
- **OASIS API:** `/Docs/Devs/API Documentation/WEB4_OASIS_API_Documentation.md`
- **STAR API:** `/Docs/Devs/API Documentation/WEB5_STAR_API_Documentation.md`

---

## ✅ Next Steps

1. Review this master brief
2. Assign teams to specific components
3. Provide each team with their specific brief
4. Set up development environment
5. Begin Phase 1 implementation

---

*This master brief provides the complete overview. Each team should refer to their specific brief for detailed implementation guidance.*
