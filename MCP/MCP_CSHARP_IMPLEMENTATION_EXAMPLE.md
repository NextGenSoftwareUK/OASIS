# MCP Controllers in C# - Implementation Example

This document shows how to implement MCP protocol support directly in the OASIS C# API.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    Cursor IDE                            │
└──────────────────────┬────────────────────────────────────┘
                       │ HTTP/SSE (MCP Protocol)
                       ↓
┌─────────────────────────────────────────────────────────┐
│              OASIS API (C#)                              │
│  ┌──────────────────────────────────────────────────┐  │
│  │  MCP Protocol Handler                             │  │
│  │  - tools/list                                     │  │
│  │  - tools/call                                     │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  MCP Tool Controllers                            │  │
│  │  - OASISMCPController                            │  │
│  │  - SmartContractMCPController                    │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  OASIS Managers (Direct Access)                  │  │
│  │  - AvatarManager                                 │  │
│  │  - NFTManager                                    │  │
│  │  - WalletManager                                 │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

## Step 1: MCP Protocol Models

```csharp
// Models/MCP/MCPRequest.cs
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.MCP
{
    public class MCPRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("method")]
        public string Method { get; set; }
        
        [JsonPropertyName("params")]
        public object? Params { get; set; }
    }

    public class MCPToolCallParams
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("arguments")]
        public Dictionary<string, object>? Arguments { get; set; }
    }
}

// Models/MCP/MCPResponse.cs
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.MCP
{
    public class MCPResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("result")]
        public object? Result { get; set; }
        
        [JsonPropertyName("error")]
        public MCPError? Error { get; set; }
    }

    public class MCPError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    public class MCPContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";
        
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class MCPTool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonPropertyName("inputSchema")]
        public MCPToolInputSchema InputSchema { get; set; } = new();
    }

    public class MCPToolInputSchema
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";
        
        [JsonPropertyName("properties")]
        public Dictionary<string, MCPToolProperty> Properties { get; set; } = new();
        
        [JsonPropertyName("required")]
        public List<string> Required { get; set; } = new();
    }

    public class MCPToolProperty
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "string";
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}
```

## Step 2: MCP Protocol Handler

```csharp
// Services/MCP/MCPProtocolHandler.cs
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.MCP
{
    public interface IMCPProtocolHandler
    {
        Task<MCPResponse> HandleRequestAsync(MCPRequest request);
        Task<List<MCPTool>> ListToolsAsync();
    }

    public class MCPProtocolHandler : IMCPProtocolHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, IMCPToolHandler> _toolHandlers;

        public MCPProtocolHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _toolHandlers = new Dictionary<string, IMCPToolHandler>
            {
                // Register tool handlers
                { "oasis_", _serviceProvider.GetRequiredService<OASISMCPToolHandler>() },
                { "solana_", _serviceProvider.GetRequiredService<SolanaMCPToolHandler>() },
                { "scgen_", _serviceProvider.GetRequiredService<SmartContractMCPToolHandler>() }
            };
        }

        public async Task<MCPResponse> HandleRequestAsync(MCPRequest request)
        {
            try
            {
                return request.Method switch
                {
                    "tools/list" => new MCPResponse
                    {
                        Id = request.Id,
                        Result = new { tools = await ListToolsAsync() }
                    },
                    "tools/call" => await HandleToolCallAsync(request),
                    _ => new MCPResponse
                    {
                        Id = request.Id,
                        Error = new MCPError
                        {
                            Code = -32601,
                            Message = $"Method not found: {request.Method}"
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return new MCPResponse
                {
                    Id = request.Id,
                    Error = new MCPError
                    {
                        Code = -32603,
                        Message = "Internal error",
                        Data = ex.Message
                    }
                };
            }
        }

        private async Task<MCPResponse> HandleToolCallAsync(MCPRequest request)
        {
            var toolCallParams = JsonSerializer.Deserialize<MCPToolCallParams>(
                JsonSerializer.Serialize(request.Params),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (toolCallParams == null || string.IsNullOrEmpty(toolCallParams.Name))
            {
                return new MCPResponse
                {
                    Id = request.Id,
                    Error = new MCPError
                    {
                        Code = -32602,
                        Message = "Invalid params: tool name required"
                    }
                };
            }

            // Find appropriate handler
            var handler = _toolHandlers.FirstOrDefault(h => 
                toolCallParams.Name.StartsWith(h.Key)
            ).Value;

            if (handler == null)
            {
                return new MCPResponse
                {
                    Id = request.Id,
                    Error = new MCPError
                    {
                        Code = -32601,
                        Message = $"Unknown tool: {toolCallParams.Name}"
                    }
                };
            }

            // Execute tool
            var result = await handler.HandleToolAsync(
                toolCallParams.Name,
                toolCallParams.Arguments ?? new Dictionary<string, object>()
            );

            return new MCPResponse
            {
                Id = request.Id,
                Result = new
                {
                    content = new[]
                    {
                        new MCPContent
                        {
                            Type = "text",
                            Text = JsonSerializer.Serialize(result, new JsonSerializerOptions
                            {
                                WriteIndented = true
                            })
                        }
                    }
                }
            };
        }

        public async Task<List<MCPTool>> ListToolsAsync()
        {
            var tools = new List<MCPTool>();

            // Get tools from all handlers
            foreach (var handler in _toolHandlers.Values)
            {
                var handlerTools = await handler.GetToolsAsync();
                tools.AddRange(handlerTools);
            }

            return tools;
        }
    }
}
```

## Step 3: MCP Tool Handler Interface

```csharp
// Services/MCP/IMCPToolHandler.cs
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.MCP
{
    public interface IMCPToolHandler
    {
        Task<object> HandleToolAsync(string toolName, Dictionary<string, object> arguments);
        Task<List<MCPTool>> GetToolsAsync();
    }
}
```

## Step 4: OASIS MCP Tool Handler

```csharp
// Services/MCP/OASISMCPToolHandler.cs
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.MCP
{
    public class OASISMCPToolHandler : IMCPToolHandler
    {
        private readonly IAvatarManager _avatarManager;
        private readonly INFTManager _nftManager;
        private readonly IWalletManager _walletManager;
        private readonly IHolonManager _holonManager;

        public OASISMCPToolHandler(
            IAvatarManager avatarManager,
            INFTManager nftManager,
            IWalletManager walletManager,
            IHolonManager holonManager)
        {
            _avatarManager = avatarManager;
            _nftManager = nftManager;
            _walletManager = walletManager;
            _holonManager = holonManager;
        }

        public async Task<object> HandleToolAsync(string toolName, Dictionary<string, object> arguments)
        {
            return toolName switch
            {
                "oasis_get_avatar" => await HandleGetAvatarAsync(arguments),
                "oasis_get_karma" => await HandleGetKarmaAsync(arguments),
                "oasis_get_nfts" => await HandleGetNFTsAsync(arguments),
                "oasis_mint_nft" => await HandleMintNFTAsync(arguments),
                "oasis_create_wallet" => await HandleCreateWalletAsync(arguments),
                "oasis_save_holon" => await HandleSaveHolonAsync(arguments),
                // ... more tools
                _ => throw new ArgumentException($"Unknown tool: {toolName}")
            };
        }

        private async Task<object> HandleGetAvatarAsync(Dictionary<string, object> arguments)
        {
            // Direct access to manager - no HTTP overhead!
            var avatarId = arguments.GetValueOrDefault("avatarId")?.ToString();
            var username = arguments.GetValueOrDefault("username")?.ToString();
            var email = arguments.GetValueOrDefault("email")?.ToString();

            IAvatar? avatar = null;

            if (!string.IsNullOrEmpty(avatarId))
            {
                var result = await _avatarManager.LoadAvatarAsync(Guid.Parse(avatarId));
                avatar = result.Result;
            }
            else if (!string.IsNullOrEmpty(username))
            {
                var result = await _avatarManager.LoadAvatarAsync(username);
                avatar = result.Result;
            }
            else if (!string.IsNullOrEmpty(email))
            {
                var result = await _avatarManager.LoadAvatarByEmailAsync(email);
                avatar = result.Result;
            }

            return avatar ?? new { error = "Avatar not found" };
        }

        private async Task<object> HandleGetKarmaAsync(Dictionary<string, object> arguments)
        {
            var avatarId = Guid.Parse(arguments["avatarId"].ToString()!);
            var result = await _avatarManager.GetKarmaAsync(avatarId);
            return new { karma = result.Result };
        }

        private async Task<object> HandleGetNFTsAsync(Dictionary<string, object> arguments)
        {
            var avatarId = Guid.Parse(arguments["avatarId"].ToString()!);
            var result = await _nftManager.GetNFTsForAvatarAsync(avatarId);
            return result.Result ?? new List<object>();
        }

        private async Task<object> HandleMintNFTAsync(Dictionary<string, object> arguments)
        {
            // Direct access - no serialization overhead!
            var mintRequest = new MintNFTRequest
            {
                Symbol = arguments["Symbol"].ToString()!,
                Title = arguments.GetValueOrDefault("Title")?.ToString() ?? "Untitled NFT",
                Description = arguments.GetValueOrDefault("Description")?.ToString(),
                JSONMetaDataURL = arguments["JSONMetaDataURL"].ToString()!,
                // ... map other fields
            };

            var result = await _nftManager.MintNFTAsync(mintRequest);
            return result.Result ?? new { error = "Failed to mint NFT" };
        }

        private async Task<object> HandleCreateWalletAsync(Dictionary<string, object> arguments)
        {
            var avatarId = Guid.Parse(arguments["avatarId"].ToString()!);
            var walletType = arguments["walletType"].ToString()!;
            
            var result = await _walletManager.CreateWalletAsync(avatarId, walletType);
            return result.Result ?? new { error = "Failed to create wallet" };
        }

        private async Task<object> HandleSaveHolonAsync(Dictionary<string, object> arguments)
        {
            // Direct holon creation - no HTTP overhead!
            var holon = JsonSerializer.Deserialize<IHolon>(
                JsonSerializer.Serialize(arguments["holon"])
            );

            if (holon == null)
                return new { error = "Invalid holon data" };

            var result = await _holonManager.SaveHolonAsync(holon);
            return result.Result ?? new { error = "Failed to save holon" };
        }

        public async Task<List<MCPTool>> GetToolsAsync()
        {
            return new List<MCPTool>
            {
                new MCPTool
                {
                    Name = "oasis_get_avatar",
                    Description = "Get avatar by ID, username, or email",
                    InputSchema = new MCPToolInputSchema
                    {
                        Properties = new Dictionary<string, MCPToolProperty>
                        {
                            { "avatarId", new MCPToolProperty { Type = "string", Description = "Avatar ID (UUID)" } },
                            { "username", new MCPToolProperty { Type = "string", Description = "Avatar username" } },
                            { "email", new MCPToolProperty { Type = "string", Description = "Avatar email" } }
                        }
                    }
                },
                new MCPTool
                {
                    Name = "oasis_get_karma",
                    Description = "Get karma score for an avatar",
                    InputSchema = new MCPToolInputSchema
                    {
                        Properties = new Dictionary<string, MCPToolProperty>
                        {
                            { "avatarId", new MCPToolProperty { Type = "string", Description = "Avatar ID (UUID)", Required = true } }
                        },
                        Required = new List<string> { "avatarId" }
                    }
                },
                // ... more tools
            };
        }
    }
}
```

## Step 5: MCP Controller

```csharp
// Controllers/MCPController.cs
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MCPController : ControllerBase
    {
        private readonly IMCPProtocolHandler _mcpHandler;
        private readonly ILogger<MCPController> _logger;

        public MCPController(
            IMCPProtocolHandler mcpHandler,
            ILogger<MCPController> logger)
        {
            _mcpHandler = mcpHandler;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> HandleMCPRequest([FromBody] MCPRequest request)
        {
            _logger.LogInformation("MCP Request: {Method} (ID: {Id})", request.Method, request.Id);

            var response = await _mcpHandler.HandleRequestAsync(request);

            return Ok(response);
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok", protocol = "MCP", version = "1.0" });
        }
    }
}
```

## Step 6: Dependency Injection Setup

```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // ... existing services

    // Register MCP services
    services.AddScoped<IMCPProtocolHandler, MCPProtocolHandler>();
    services.AddScoped<OASISMCPToolHandler>();
    services.AddScoped<SolanaMCPToolHandler>();
    services.AddScoped<SmartContractMCPToolHandler>();

    // ... rest of configuration
}
```

## Step 7: Cursor Configuration

```json
{
  "mcpServers": {
    "oasis-unified": {
      "url": "http://localhost:5000/api/mcp",
      "transport": "http",
      "headers": {
        "Authorization": "Bearer ${OASIS_API_KEY}"
      }
    }
  }
}
```

## Performance Benefits

### Before (Node.js MCP Server):
```csharp
// In Node.js MCP Server
const response = await axios.post('http://localhost:5000/api/avatar/load', {
    avatarId: '...'
});
// HTTP serialization + network + HTTP deserialization
```

### After (C# Controllers):
```csharp
// Direct method call - no HTTP overhead!
var result = await _avatarManager.LoadAvatarAsync(avatarId);
// Direct object access - no serialization!
```

**Performance Improvement:**
- Eliminates HTTP request overhead (~10-50ms saved)
- Eliminates JSON serialization/deserialization
- Direct memory access
- Better error handling (same exception model)

## Testing

```csharp
[Fact]
public async Task TestMCPGetAvatar()
{
    // Arrange
    var request = new MCPRequest
    {
        Id = "test-1",
        Method = "tools/call",
        Params = new MCPToolCallParams
        {
            Name = "oasis_get_avatar",
            Arguments = new Dictionary<string, object>
            {
                { "avatarId", "test-avatar-id" }
            }
        }
    };

    // Act
    var response = await _mcpHandler.HandleRequestAsync(request);

    // Assert
    Assert.NotNull(response.Result);
    // Verify avatar data
}
```

## Migration Strategy

1. **Phase 1:** Implement MCP controllers alongside existing API
2. **Phase 2:** Test with Cursor using HTTP transport
3. **Phase 3:** Benchmark performance vs Node.js MCP server
4. **Phase 4:** Keep both options, let users choose
5. **Phase 5:** (Optional) Deprecate Node.js server if C# is clearly better

## Conclusion

This implementation:
- ✅ Eliminates HTTP overhead between MCP and API
- ✅ Provides direct access to OASIS managers
- ✅ Faster execution (C# performance)
- ✅ Single codebase (easier maintenance)
- ✅ Works with Cursor via HTTP/SSE transport

**Next Step:** Implement a proof-of-concept with 2-3 tools to validate the approach.
