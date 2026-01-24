# OASIS IDE: How to Actually Use It

**Date:** January 2026  
**Status:** ✅ Working Implementation Guide

---

## 🎯 Quick Answer

The IDE is now **fully functional**! The chat assistant can:
- ✅ Execute OASIS MCP tools
- ✅ Answer questions about the codebase
- ✅ Help with OASIS operations

---

## 🚀 Getting Started

### 1. Start the IDE

```bash
cd /Users/maxgershfield/OASIS_CLEAN/OASIS-IDE
npm install
npm run dev
```

### 2. Open the Chat

The chat panel is at the bottom of the IDE. You'll see:
```
Hello! I'm your OASIS IDE assistant...
```

### 3. Try These Commands

#### **Check OASIS Health**
```
You: "Check OASIS health"
IDE: ✅ OASIS API is healthy and running!
```

#### **Create a Wallet**
```
You: "Create a Solana wallet"
IDE: ✅ Solana wallet created!
     Address: 7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU
```

#### **Mint an NFT**
```
You: "Mint NFT with title My Art"
IDE: ✅ NFT minted!
     Mint Address: abc123...
     Title: My Art
```

#### **Explore Codebase**
```
You: "Show me the OASIS codebase structure"
IDE: 📚 OASIS Codebase Structure:
     - Core: /OASIS Architecture/
     - MCP: /MCP/
     - Agents: /A2A/
     ...
```

#### **Ask About MCP**
```
You: "How does MCP integration work?"
IDE: 📁 MCP Integration:
     - Server: /MCP/src/index.ts
     - Tools: /MCP/src/tools/oasisTools.ts
     ...
```

---

## 💬 What You Can Say

### **OASIS Operations**

✅ **Health & Status**
- "Check OASIS health"
- "Check OASIS status"

✅ **Wallets**
- "Create a Solana wallet"
- "Create an Ethereum wallet"
- "Create Solana wallet for avatar abc123"

✅ **NFTs**
- "Mint NFT with title My Art"
- "Mint NFT with title My Art and image https://example.com/image.png"
- "Create NFT called My Collection"

✅ **Avatars**
- "Create avatar with username testuser and email test@example.com"

✅ **Karma**
- "Get karma for avatar abc123"
- "Check karma for avatar abc123"

✅ **Holons**
- "Save holon with name UserProfile"
- "Create holon called MyData"

### **Codebase Queries**

✅ **Structure**
- "Show me the OASIS codebase structure"
- "Where is the MCP code?"
- "How does MCP integration work?"
- "Where are the agents?"

✅ **APIs**
- "Show me the OASIS API structure"
- "Where are the API endpoints?"

---

## 🔧 How It Works

### **1. Natural Language Processing**

When you type a message, the AI Assistant:
1. **Parses your message** - Understands what you want
2. **Maps to MCP tool** - Finds the right tool
3. **Extracts parameters** - Gets values from your message
4. **Executes tool** - Calls the MCP tool
5. **Formats response** - Shows you the result

### **2. Example Flow**

```
You: "Create a Solana wallet"
     ↓
AI Assistant: Maps to "oasis_create_solana_wallet"
     ↓
MCP Tool: Executes via OASIS API
     ↓
Response: "✅ Solana wallet created! Address: ..."
```

---

## 🎯 Real Examples

### Example 1: Check System Status

```
You: "Check OASIS health"

IDE Process:
1. Maps to: oasis_health_check
2. Executes: Calls OASIS API /api/health
3. Returns: ✅ OASIS API is healthy and running!
```

### Example 2: Create Wallet

```
You: "Create a Solana wallet"

IDE Process:
1. Maps to: oasis_create_solana_wallet
2. Parameters: { setAsDefault: true }
3. Executes: Creates wallet via OASIS API
4. Returns: ✅ Solana wallet created! Address: ...
```

### Example 3: Explore Codebase

```
You: "Show me the OASIS codebase structure"

IDE Process:
1. Recognizes as codebase query
2. Returns: Codebase structure with paths
3. Shows: Where to find MCP, Agents, APIs, etc.
```

---

## 🐛 Troubleshooting

### **"Loading tools..." Forever**

**Problem:** MCP server not starting

**Solution:**
1. Check MCP server is built: `cd ../MCP && npm run build`
2. Verify path in `MCPServerManager.ts`
3. Check console for errors

### **"No tools available"**

**Problem:** MCP server not connected

**Solution:**
1. Check OASIS API is running: `curl http://127.0.0.1:5003/api/health`
2. Restart IDE
3. Check Electron console for errors

### **Tool Execution Fails**

**Problem:** Tool executes but returns error

**Solution:**
1. Check OASIS API is running
2. Verify authentication (some tools need auth)
3. Check tool parameters are correct

---

## 📋 Supported Commands

### **Currently Working**

✅ Health checks  
✅ Wallet creation (Solana, Ethereum)  
✅ NFT minting (basic)  
✅ Avatar creation  
✅ Karma queries  
✅ Holon operations  
✅ Codebase exploration  

### **Coming Soon**

🚧 Full NFT minting (with images)  
🚧 Agent invocation  
🚧 Code generation  
🚧 File operations  
🚧 OAPP creation  

---

## 💡 Tips

### **1. Be Specific**

❌ "Create wallet"  
✅ "Create Solana wallet"

### **2. Include Details**

❌ "Mint NFT"  
✅ "Mint NFT with title My Art"

### **3. Ask for Help**

If unsure, just ask:
- "What can you do?"
- "Show me available tools"
- "Help me create a wallet"

---

## 🎉 What Makes This Special

**Unlike Cursor:**
- ❌ Cursor: You configure MCP manually
- ✅ OASIS IDE: MCP tools work automatically

**Unlike Other IDEs:**
- ❌ Other IDEs: Generic code editing
- ✅ OASIS IDE: Understands OASIS ecosystem

**The Result:**
- You can build interoperable apps
- Without learning APIs
- Without configuration
- Just by talking to the IDE

---

## 🚀 Next Steps

1. **Try the commands above** - See what works
2. **Explore the codebase** - Ask about structure
3. **Create wallets** - Test blockchain operations
4. **Mint NFTs** - Try NFT creation
5. **Build something** - Use it for real projects

---

*The IDE is ready to use! Just start chatting and it will help you build! 🎉*
