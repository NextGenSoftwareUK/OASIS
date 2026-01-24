# OASIS IDE: MCP Integration Brief

**For:** MCP Integration Development Team  
**Status:** 📋 Implementation Brief  
**Reference:** Master Brief (`OASIS_IDE_MASTER_BRIEF.md`)

---

## 🎯 Objective

Build **native MCP (Model Context Protocol) integration** that makes OASIS tools available directly in the IDE without any configuration. This is a key differentiator from Cursor.

---

## 📦 Components to Build

### 1. MCP Server Manager

**Requirements:**
- [ ] Automatic startup of OASIS MCP server
- [ ] MCP server lifecycle management (start/stop/restart)
- [ ] Multiple MCP server support
- [ ] Server health monitoring
- [ ] Error recovery and retry logic
- [ ] Server configuration management

**Implementation:**
```typescript
import { spawn } from 'child_process';
import { MCPServer } from '@modelcontextprotocol/sdk/server/index.js';

class MCPServerManager {
  private servers: Map<string, MCPServerProcess> = new Map();
  
  async startOASISMCP() {
    const server = spawn('node', [
      path.join(__dirname, '../MCP/dist/index.js')
    ], {
      stdio: ['pipe', 'pipe', 'pipe']
    });
    
    const mcpServer = new MCPServer({
      name: 'oasis-unified',
      version: '1.0.0'
    });
    
    // Connect via stdio
    const transport = new StdioServerTransport(server);
    await mcpServer.connect(transport);
    
    this.servers.set('oasis-unified', {
      process: server,
      server: mcpServer,
      status: 'running'
    });
    
    return mcpServer;
  }
  
  async listTools(serverName: string) {
    const server = this.servers.get(serverName);
    if (!server) throw new Error('Server not found');
    
    return await server.server.listTools();
  }
}
```

### 2. Tool Registry

**Requirements:**
- [ ] Discover all available tools from MCP servers
- [ ] Cache tool definitions
- [ ] Tool metadata storage (name, description, parameters)
- [ ] Tool categorization (OASIS, STAR, Smart Contracts, etc.)
- [ ] Tool search functionality
- [ ] Tool documentation viewer

**Implementation:**
```typescript
interface ToolDefinition {
  name: string;
  description: string;
  inputSchema: JSONSchema;
  category: string;
  server: string;
}

class ToolRegistry {
  private tools: Map<string, ToolDefinition> = new Map();
  
  async discoverTools() {
    const oasisServer = await this.mcpManager.startOASISMCP();
    const tools = await oasisServer.listTools();
    
    for (const tool of tools.tools) {
      this.tools.set(tool.name, {
        name: tool.name,
        description: tool.description || '',
        inputSchema: tool.inputSchema,
        category: this.categorizeTool(tool.name),
        server: 'oasis-unified'
      });
    }
  }
  
  categorizeTool(name: string): string {
    if (name.startsWith('oasis_')) return 'OASIS';
    if (name.startsWith('star_')) return 'STAR';
    if (name.startsWith('scgen_')) return 'Smart Contracts';
    return 'Other';
  }
  
  searchTools(query: string): ToolDefinition[] {
    const lowerQuery = query.toLowerCase();
    return Array.from(this.tools.values()).filter(tool =>
      tool.name.toLowerCase().includes(lowerQuery) ||
      tool.description.toLowerCase().includes(lowerQuery)
    );
  }
}
```

### 3. Tool Execution Engine

**Requirements:**
- [ ] Execute MCP tools with parameters
- [ ] Progress tracking for long-running operations
- [ ] Response handling and formatting
- [ ] Error handling and user-friendly messages
- [ ] Retry logic for failed operations
- [ ] Timeout management
- [ ] Result caching

**Implementation:**
```typescript
class ToolExecutionEngine {
  async executeTool(
    toolName: string,
    args: any,
    options?: { timeout?: number; retries?: number }
  ): Promise<ToolResult> {
    const tool = this.registry.getTool(toolName);
    if (!tool) throw new Error(`Tool not found: ${toolName}`);
    
    const server = this.mcpManager.getServer(tool.server);
    
    try {
      const result = await Promise.race([
        server.callTool(toolName, args),
        this.createTimeout(options?.timeout || 30000)
      ]);
      
      return {
        success: true,
        data: result,
        tool: toolName
      };
    } catch (error) {
      if (options?.retries && options.retries > 0) {
        return this.executeTool(toolName, args, {
          ...options,
          retries: options.retries - 1
        });
      }
      
      return {
        success: false,
        error: this.formatError(error),
        tool: toolName
      };
    }
  }
  
  private formatError(error: any): string {
    if (error.message) return error.message;
    if (error.response?.data?.message) return error.response.data.message;
    return 'Unknown error occurred';
  }
}
```

### 4. Natural Language to Tool Mapping

**Requirements:**
- [ ] Parse user intent from natural language
- [ ] Map intent to appropriate MCP tool
- [ ] Extract parameters from natural language
- [ ] Handle ambiguous requests
- [ ] Suggest tools when intent is unclear
- [ ] Learn from user corrections

**Implementation:**
```typescript
class NLToToolMapper {
  private llmClient: LLMClient;
  
  async mapToTool(userMessage: string): Promise<ToolMapping> {
    // Use LLM to understand intent
    const intent = await this.llmClient.parseIntent(userMessage);
    
    // Find matching tools
    const candidates = this.registry.searchTools(intent.action);
    
    if (candidates.length === 0) {
      return {
        success: false,
        message: 'No matching tool found. Did you mean...',
        suggestions: this.getSuggestions(intent)
      };
    }
    
    // Select best match
    const tool = this.selectBestMatch(intent, candidates);
    
    // Extract parameters
    const params = await this.extractParameters(userMessage, tool);
    
    return {
      success: true,
      tool: tool.name,
      parameters: params,
      confidence: this.calculateConfidence(intent, tool)
    };
  }
  
  private async extractParameters(
    message: string,
    tool: ToolDefinition
  ): Promise<any> {
    // Use LLM to extract structured parameters
    const prompt = `Extract parameters for tool ${tool.name} from: "${message}"
    
    Tool schema: ${JSON.stringify(tool.inputSchema)}
    
    Return JSON with extracted parameters.`;
    
    const response = await this.llmClient.complete(prompt);
    return JSON.parse(response);
  }
}
```

### 5. Tool UI Components

**Requirements:**
- [ ] Tool palette/sidebar
- [ ] Tool search interface
- [ ] Tool parameter form (auto-generated from schema)
- [ ] Tool execution progress indicator
- [ ] Tool result viewer
- [ ] Tool history/log
- [ ] Favorite tools

**Implementation:**
```typescript
// React component
const ToolPalette: React.FC = () => {
  const { tools, executeTool } = useToolRegistry();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedTool, setSelectedTool] = useState<ToolDefinition | null>(null);
  
  const filteredTools = tools.filter(tool =>
    tool.name.includes(searchQuery) ||
    tool.description.includes(searchQuery)
  );
  
  return (
    <div className="tool-palette">
      <input
        placeholder="Search tools..."
        value={searchQuery}
        onChange={e => setSearchQuery(e.target.value)}
      />
      <div className="tool-list">
        {filteredTools.map(tool => (
          <ToolItem
            key={tool.name}
            tool={tool}
            onClick={() => setSelectedTool(tool)}
          />
        ))}
      </div>
      {selectedTool && (
        <ToolExecutor
          tool={selectedTool}
          onExecute={params => executeTool(selectedTool.name, params)}
        />
      )}
    </div>
  );
};
```

### 6. Pre-configured OASIS Tools

**Requirements:**
- [ ] Auto-start OASIS MCP server on IDE launch
- [ ] Pre-register 100+ OASIS tools
- [ ] No user configuration needed
- [ ] Tools available immediately
- [ ] Tool documentation built-in

**Tools to Pre-configure:**
- Avatar operations (register, authenticate, get, update)
- Wallet operations (create, get, send transactions)
- NFT operations (mint, get, send, place GeoNFT)
- Holon operations (save, get, search, update)
- Karma operations (get, add, remove, stats)
- Agent operations (register, discover, communicate)
- Smart contract operations (generate, compile, deploy)

---

## 🔧 Technical Requirements

### Dependencies

```json
{
  "dependencies": {
    "@modelcontextprotocol/sdk": "^0.5.0",
    "openai": "^4.0.0",
    "zod": "^3.22.0"
  }
}
```

### File Structure

```
src/
├── mcp/
│   ├── MCPServerManager.ts
│   ├── ToolRegistry.ts
│   ├── ToolExecutionEngine.ts
│   ├── NLToToolMapper.ts
│   └── types.ts
├── components/
│   ├── ToolPalette.tsx
│   ├── ToolExecutor.tsx
│   ├── ToolResultViewer.tsx
│   └── ToolHistory.tsx
└── services/
    └── MCPService.ts
```

---

## 🔗 Integration Points

### With AI Assistant
- AI Assistant uses MCP tools automatically
- User says "create wallet" → AI uses `oasis_create_solana_wallet`
- No need to manually call tools

### With Chat Interface
- Chat can execute tools directly
- Tool results shown in chat
- Tool suggestions in chat

### With Code Editor
- Inline tool suggestions while coding
- Code completion suggests MCP tool calls
- Tool documentation in hover

---

## ✅ Acceptance Criteria

- [ ] OASIS MCP server starts automatically on IDE launch
- [ ] All 100+ OASIS tools are discoverable
- [ ] Tools can be executed from chat
- [ ] Natural language maps to tools correctly
- [ ] Tool execution shows progress
- [ ] Tool results are displayed clearly
- [ ] Errors are handled gracefully
- [ ] No configuration needed by user

---

## 📚 Resources

- **MCP Specification:** https://modelcontextprotocol.io
- **OASIS MCP Server:** `/MCP/src/index.ts`
- **OASIS Tools:** `/MCP/src/tools/oasisTools.ts`
- **Cursor MCP Integration:** https://github.com/getcursor/cursor (reference)

---

## 🎯 Success Metrics

- MCP server starts in < 2 seconds
- Tool discovery completes in < 1 second
- Tool execution completes in < 5 seconds (for most tools)
- Natural language mapping accuracy > 90%
- Zero configuration required

---

*This brief covers MCP integration. The MCP server itself is already built at `/MCP/` - this team integrates it into the IDE.*
