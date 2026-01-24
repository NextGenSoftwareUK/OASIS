# OASIS IDE: Agent System Brief

**For:** Agent System Development Team  
**Status:** 📋 Implementation Brief  
**Reference:** Master Brief (`OASIS_IDE_MASTER_BRIEF.md`)

---

## 🎯 Objective

Build the **agent system** that enables:
- Agent discovery via A2A Protocol
- Agent invocation from IDE
- Agent-to-agent communication
- Local agent execution
- Agent marketplace

---

## 📦 Components to Build

### 1. A2A Protocol Client

**Requirements:**
- [ ] A2A Protocol implementation (JSON-RPC 2.0)
- [ ] Agent discovery via SERV
- [ ] Agent card retrieval
- [ ] Agent capability querying
- [ ] A2A message sending/receiving
- [ ] Message queuing
- [ ] Error handling

**Reference:**
- A2A Protocol: `/A2A/OASIS_A2A_PROTOCOL_DOCUMENTATION.md`
- A2A Manager: `/OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/A2AManager/`

**Implementation:**
```typescript
class A2AClient {
  private oasisApi: OASISAPIClient;
  
  async discoverAgents(serviceName?: string): Promise<Agent[]> {
    const response = await this.oasisApi.get('/api/a2a/agents/discover-serv', {
      params: serviceName ? { service: serviceName } : {}
    });
    
    return response.data.result.map((agent: any) => ({
      id: agent.avatarId,
      name: agent.name,
      services: agent.services,
      skills: agent.skills,
      status: agent.status,
      pricing: agent.pricing
    }));
  }
  
  async getAgentCard(agentId: string): Promise<AgentCard> {
    const response = await this.oasisApi.get(`/api/a2a/agent-card/${agentId}`);
    return response.data.result;
  }
  
  async sendA2AMessage(
    toAgentId: string,
    method: string,
    params: any
  ): Promise<any> {
    const message = {
      jsonrpc: '2.0',
      method: method,
      params: params,
      id: this.generateId()
    };
    
    const response = await this.oasisApi.post('/api/a2a/jsonrpc', {
      toAgentId,
      ...message
    });
    
    return response.data.result;
  }
}
```

### 2. Agent Registry UI

**Requirements:**
- [ ] Agent list view
- [ ] Agent search and filter
- [ ] Agent card display
- [ ] Agent capabilities view
- [ ] Agent status indicators
- [ ] Agent pricing display
- [ ] Favorite agents
- [ ] Recent agents

**Implementation:**
```typescript
const AgentRegistry: React.FC = () => {
  const { agents, discoverAgents, loading } = useAgentDiscovery();
  const [searchQuery, setSearchQuery] = useState('');
  const [filterService, setFilterService] = useState<string | null>(null);
  
  useEffect(() => {
    discoverAgents(filterService || undefined);
  }, [filterService]);
  
  const filteredAgents = agents.filter(agent =>
    agent.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    agent.services.some(s => s.includes(searchQuery))
  );
  
  return (
    <div className="agent-registry">
      <div className="filters">
        <input
          placeholder="Search agents..."
          value={searchQuery}
          onChange={e => setSearchQuery(e.target.value)}
        />
        <select
          value={filterService || ''}
          onChange={e => setFilterService(e.target.value || null)}
        >
          <option value="">All Services</option>
          <option value="nft-minting">NFT Minting</option>
          <option value="code-generation">Code Generation</option>
          <option value="data-analysis">Data Analysis</option>
        </select>
      </div>
      
      <div className="agent-list">
        {filteredAgents.map(agent => (
          <AgentCard
            key={agent.id}
            agent={agent}
            onSelect={() => invokeAgent(agent.id)}
          />
        ))}
      </div>
    </div>
  );
};
```

### 3. Agent Invocation System

**Requirements:**
- [ ] Invoke agent from chat
- [ ] Invoke agent from UI
- [ ] Pass context to agent
- [ ] Handle agent responses
- [ ] Display agent progress
- [ ] Handle agent errors
- [ ] Agent result visualization

**Implementation:**
```typescript
class AgentInvocationManager {
  private a2aClient: A2AClient;
  private mcpManager: MCPServerManager;
  
  async invokeAgent(
    agentId: string,
    task: string,
    context?: ProjectContext
  ): Promise<AgentResult> {
    // Get agent card
    const agentCard = await this.a2aClient.getAgentCard(agentId);
    
    // Prepare request
    const request = {
      service: this.determineService(task),
      task: task,
      context: {
        ...context,
        // Give agent access to MCP tools
        mcpTools: this.mcpManager.getAvailableTools(),
        // Project files
        files: context?.files || []
      }
    };
    
    // Send A2A request
    const response = await this.a2aClient.sendA2AMessage(
      agentId,
      'service_request',
      request
    );
    
    // Agent may use MCP tools internally
    // Monitor tool usage
    this.monitorAgentToolUsage(agentId, response);
    
    return {
      success: response.success,
      result: response.result,
      toolCalls: response.toolCalls || [],
      executionTime: response.executionTime
    };
  }
  
  private determineService(task: string): string {
    // Use LLM or pattern matching to determine service
    if (task.includes('mint nft')) return 'nft-minting';
    if (task.includes('generate code')) return 'code-generation';
    if (task.includes('analyze data')) return 'data-analysis';
    return 'general';
  }
}
```

### 4. Local Agent Runtime

**Requirements:**
- [ ] Execute agents locally in IDE
- [ ] Agent sandboxing
- [ ] Agent resource limits
- [ ] Agent logging
- [ ] Agent debugging
- [ ] Agent hot-reload

**Implementation:**
```typescript
class LocalAgentRuntime {
  private agents: Map<string, AgentProcess> = new Map();
  
  async startAgent(agentConfig: AgentConfig): Promise<string> {
    const agentId = generateId();
    
    // Create agent process
    const process = spawn('node', [
      agentConfig.entryPoint
    ], {
      cwd: agentConfig.workingDirectory,
      env: {
        ...process.env,
        OASIS_API_URL: this.oasisApiUrl,
        OASIS_JWT_TOKEN: this.authToken,
        MCP_SERVER_URL: this.mcpServerUrl
      }
    });
    
    // Monitor agent
    process.stdout.on('data', (data) => {
      this.logAgentOutput(agentId, data.toString());
    });
    
    process.stderr.on('data', (data) => {
      this.logAgentError(agentId, data.toString());
    });
    
    this.agents.set(agentId, {
      process,
      config: agentConfig,
      status: 'running'
    });
    
    return agentId;
  }
  
  async stopAgent(agentId: string) {
    const agent = this.agents.get(agentId);
    if (agent) {
      agent.process.kill();
      this.agents.delete(agentId);
    }
  }
}
```

### 5. Agent Marketplace

**Requirements:**
- [ ] Browse available agents
- [ ] Agent categories
- [ ] Agent ratings/reviews
- [ ] Agent installation
- [ ] Agent updates
- [ ] Agent sharing

**Implementation:**
```typescript
const AgentMarketplace: React.FC = () => {
  const { agents, installAgent } = useAgentMarketplace();
  
  return (
    <div className="agent-marketplace">
      <div className="categories">
        <CategoryFilter />
      </div>
      
      <div className="agent-grid">
        {agents.map(agent => (
          <AgentMarketplaceCard
            key={agent.id}
            agent={agent}
            onInstall={() => installAgent(agent.id)}
          />
        ))}
      </div>
    </div>
  );
};
```

### 6. Agent Monitoring Dashboard

**Requirements:**
- [ ] Active agents list
- [ ] Agent status monitoring
- [ ] Agent resource usage
- [ ] Agent execution history
- [ ] Agent performance metrics
- [ ] Agent error logs

---

## 🔧 Technical Requirements

### Dependencies

```json
{
  "dependencies": {
    "axios": "^1.6.0",
    "uuid": "^9.0.0"
  }
}
```

### File Structure

```
src/
├── agents/
│   ├── A2AClient.ts
│   ├── AgentRegistry.ts
│   ├── AgentInvocationManager.ts
│   ├── LocalAgentRuntime.ts
│   └── types.ts
├── components/
│   ├── AgentRegistry.tsx
│   ├── AgentCard.tsx
│   ├── AgentInvoker.tsx
│   ├── AgentMarketplace.tsx
│   └── AgentMonitor.tsx
└── services/
    └── AgentService.ts
```

---

## 🔗 Integration Points

### With MCP Integration
- Agents can use MCP tools
- Agent tool usage is logged
- Agents can discover new tools

### With Chat Interface
- Chat can invoke agents
- "Use the NFT agent to mint NFTs"
- Agent responses shown in chat

### With Code Editor
- Agents can modify code
- Agents can generate code
- Agent suggestions inline

---

## ✅ Acceptance Criteria

- [ ] Can discover agents via A2A Protocol
- [ ] Can invoke agents from chat
- [ ] Agents can use MCP tools
- [ ] Agent responses are displayed
- [ ] Local agents can be executed
- [ ] Agent marketplace works
- [ ] Agent monitoring shows status

---

## 📚 Resources

- **A2A Protocol Docs:** `/A2A/OASIS_A2A_PROTOCOL_DOCUMENTATION.md`
- **A2A Manager:** `/OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/A2AManager/`
- **Agent Templates:** `/Docs/AGENT_TEMPLATES_USING_OASIS_STAR_MCP.md`

---

## 🎯 Success Metrics

- Agent discovery completes in < 2 seconds
- Agent invocation completes in < 10 seconds
- Agent success rate > 90%
- Agent response time < 5 seconds (average)

---

*This brief covers the agent system. Integration with OASIS A2A Protocol is already built - this team builds the IDE UI and runtime.*
