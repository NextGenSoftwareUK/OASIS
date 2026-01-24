# OASIS IDE: Quick Test Guide

**For:** Testing the IDE Right Now

---

## ✅ What's Now Working

The chat assistant is **fully functional** and can:

1. ✅ **Execute OASIS MCP Tools** - Real tool execution
2. ✅ **Answer Codebase Questions** - Knows OASIS structure
3. ✅ **Natural Language Processing** - Understands your requests

---

## 🧪 Test These Commands

### **Test 1: Health Check**
```
Type: "Check OASIS health"
Expected: ✅ OASIS API is healthy and running!
```

### **Test 2: Codebase Query**
```
Type: "Show me the OASIS codebase structure"
Expected: 📚 OASIS Codebase Structure with paths
```

### **Test 3: MCP Info**
```
Type: "How does MCP integration work?"
Expected: 📁 MCP Integration details with file paths
```

### **Test 4: Wallet Creation** (if OASIS API is running)
```
Type: "Create a Solana wallet"
Expected: ✅ Solana wallet created! Address: ...
```

### **Test 5: NFT Minting** (if authenticated)
```
Type: "Mint NFT with title Test NFT"
Expected: ✅ NFT minted! Mint Address: ...
```

---

## 🔍 How to Verify It's Working

### **1. Check Tools Loaded**

Look at the chat header - you should see:
- "X tools available" (green badge)

If you see "Loading tools..." forever:
- MCP server might not be starting
- Check console for errors

### **2. Try a Simple Command**

Type: `"Check OASIS health"`

**If it works:**
- You'll see: "✅ OASIS API is healthy and running!"
- Tool execution is working!

**If it doesn't work:**
- Check OASIS API is running
- Check console for errors

### **3. Try Codebase Query**

Type: `"Show me the OASIS codebase structure"`

**If it works:**
- You'll see codebase structure
- Natural language processing is working!

---

## 🐛 Common Issues

### **Issue: "Loading tools..." Forever**

**Check:**
1. MCP server path is correct
2. MCP server is built: `cd ../MCP && npm run build`
3. Check Electron console (DevTools)

**Fix:**
- Update path in `MCPServerManager.ts` if needed
- Rebuild MCP server

### **Issue: Tools Load But Commands Don't Work**

**Check:**
1. OASIS API is running: `curl http://127.0.0.1:5003/api/health`
2. Check network tab in DevTools
3. Check console for errors

**Fix:**
- Start OASIS API if not running
- Check API URL in `OASISAPIClient.ts`

### **Issue: "I'm not sure what you mean"**

**This is normal!** The assistant will suggest alternatives.

**Try:**
- Be more specific: "Create Solana wallet" instead of "wallet"
- Use examples from the suggestions

---

## 📊 What's Working vs. What's Not

### ✅ **Working Now**
- Natural language parsing
- Tool mapping
- Tool execution (if OASIS API is running)
- Codebase queries
- Response formatting

### 🚧 **Needs OASIS API Running**
- Wallet creation
- NFT minting
- Avatar operations
- Karma operations

### 📋 **Coming Soon**
- Full LLM integration (OpenAI)
- Code generation
- Agent invocation from chat
- File system operations

---

## 🎯 Success Criteria

**You know it's working when:**

1. ✅ Chat shows "X tools available" badge
2. ✅ "Check OASIS health" returns a response
3. ✅ Codebase queries return information
4. ✅ Tool execution shows results (if API is running)

---

## 💡 Pro Tips

1. **Start Simple** - Try "Check OASIS health" first
2. **Be Specific** - "Create Solana wallet" not just "wallet"
3. **Check Console** - DevTools show what's happening
4. **Read Responses** - The assistant explains what it's doing

---

*Try it now! The chat should work! 🚀*
