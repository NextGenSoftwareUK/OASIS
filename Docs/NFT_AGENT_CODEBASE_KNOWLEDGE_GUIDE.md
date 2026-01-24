# Adding an AI Agent to NFT - OASIS Codebase Knowledge

## Overview

This guide shows how to create an AI agent that knows about the OASIS codebase and link it to your NFT, so NFT holders can chat with it about OASIS.

---

## Step 1: Create the Agent Avatar

First, register an agent avatar in OASIS:

```javascript
// Via MCP tool
await oasis_register_avatar({
  username: "oasis_codebase_agent",
  email: "agent@oasis.local",
  password: "secure_password_here",
  avatarType: "Agent",
  firstName: "OASIS",
  lastName: "Codebase Assistant",
  title: "AI Codebase Expert"
});
```

**Response:**
```json
{
  "avatarId": "abc123-def456-...",
  "username": "oasis_codebase_agent",
  "avatarType": "Agent"
}
```

---

## Step 2: Authenticate the Agent

```javascript
await oasis_authenticate_avatar({
  username: "oasis_codebase_agent",
  password: "secure_password_here"
});
```

This sets the JWT token for subsequent requests.

---

## Step 3: Register Agent Capabilities

Register what the agent can do:

```javascript
await oasis_register_agent_capabilities({
  services: [
    "codebase-qa",
    "oasis-documentation",
    "api-reference",
    "architecture-consulting"
  ],
  skills: [
    "OASIS API",
    "STAR API",
    "C# Development",
    "TypeScript",
    "Blockchain Integration",
    "NFT Systems",
    "Agent Development"
  ],
  description: "AI assistant specialized in OASIS codebase, APIs, and architecture. Can answer questions about OASIS implementation, endpoints, data structures, and best practices.",
  status: "Available",
  max_concurrent_tasks: 10
});
```

---

## Step 4: Configure Agent with Codebase Knowledge

The agent needs access to OASIS codebase knowledge. There are several approaches:

### Option A: RAG (Retrieval Augmented Generation) - Recommended

**How it works:**
1. Index the OASIS codebase (code files, docs, API specs)
2. When user asks a question, search the index
3. Use relevant context to generate answer

**Implementation:**
```python
# agent_service.py (runs on your server)
from langchain.vectorstores import Chroma
from langchain.embeddings import OpenAIEmbeddings
from langchain.text_splitter import RecursiveCharacterTextSplitter
from langchain.document_loaders import DirectoryLoader

class OASISCodebaseAgent:
    def __init__(self):
        # Load and index OASIS codebase
        loader = DirectoryLoader('/path/to/OASIS_CLEAN', 
                                 glob='**/*.{cs,ts,md,json}')
        documents = loader.load()
        
        # Split into chunks
        text_splitter = RecursiveCharacterTextSplitter(
            chunk_size=1000, 
            chunk_overlap=200
        )
        chunks = text_splitter.split_documents(documents)
        
        # Create vector store
        self.vectorstore = Chroma.from_documents(
            chunks, 
            OpenAIEmbeddings()
        )
    
    async def answer_question(self, question: str):
        # Search codebase for relevant context
        docs = self.vectorstore.similarity_search(question, k=5)
        
        # Build context from relevant docs
        context = "\n".join([doc.page_content for doc in docs])
        
        # Generate answer using LLM with context
        prompt = f"""
        You are an expert on the OASIS codebase. Answer the question using 
        the provided context from the codebase.
        
        Context:
        {context}
        
        Question: {question}
        
        Answer:
        """
        
        response = await llm.generate(prompt)
        return response
```

### Option B: Pre-trained Knowledge Base

**How it works:**
1. Generate comprehensive documentation from codebase
2. Store in agent's metadata or external knowledge base
3. Agent references this when answering

**Implementation:**
```javascript
// Store codebase summary in agent metadata
const codebaseKnowledge = {
  "apis": {
    "oasis": "500+ endpoints for avatars, NFTs, wallets, karma...",
    "star": "500+ endpoints for missions, quests, celestial bodies..."
  },
  "architecture": {
    "holons": "Universal data containers with parent-child relationships...",
    "providers": "Multi-database support with auto-failover..."
  },
  // ... more knowledge
};

await oasis_update_avatar({
  avatarId: agentAvatarId,
  updates: {
    metaData: {
      codebaseKnowledge: codebaseKnowledge,
      knowledgeBaseUrl: "https://docs.oasisplatform.world"
    }
  }
});
```

### Option C: Live Codebase Access

**How it works:**
1. Agent has direct access to codebase (via file system or API)
2. Agent can read files on-demand when answering questions

**Implementation:**
```python
class OASISCodebaseAgent:
    def __init__(self, codebase_path):
        self.codebase_path = codebase_path
    
    async def answer_question(self, question: str):
        # Use codebase_search or file reading
        relevant_files = await self.find_relevant_files(question)
        
        # Read and analyze files
        context = await self.read_files(relevant_files)
        
        # Generate answer
        return await self.generate_answer(question, context)
```

---

## Step 5: Create Agent Endpoint

The agent needs an HTTP endpoint that can receive questions:

```python
# agent_endpoint.py (Flask/FastAPI)
from flask import Flask, request, jsonify

app = Flask(__name__)
agent = OASISCodebaseAgent()

@app.route('/api/agent/chat', methods=['POST'])
async def chat():
    data = request.json
    question = data.get('question')
    avatar_id = data.get('avatar_id')  # NFT holder's avatar ID
    
    # Verify NFT ownership (optional - for access control)
    # has_nft = await verify_nft_ownership(avatar_id, "FOREIGN_RELATIONS")
    
    # Get answer
    answer = await agent.answer_question(question)
    
    return jsonify({
        "answer": answer,
        "sources": agent.get_sources()  # Which files/docs were used
    })
```

**Deploy this endpoint** (Docker, cloud, etc.)

---

## Step 6: Register Agent as SERV Service

Make the agent discoverable:

```javascript
await oasis_register_agent_as_serv_service();
```

This registers the agent in the ONET Unified Architecture service registry.

---

## Step 7: Link Agent to NFT

Now link the agent to your "Foreign Relations" NFT:

```javascript
// Update NFT metadata with agent info
await oasis_mint_nft({
  // ... existing NFT details ...
  MetaData: {
    // ... existing metadata ...
    AgentId: agentAvatarId,  // Link to agent
    AgentType: "Agent",
    AgentCard: {
      agentId: agentAvatarId,
      name: "OASIS Codebase Assistant",
      capabilities: {
        services: ["codebase-qa", "oasis-documentation"],
        skills: ["OASIS API", "C#", "TypeScript"]
      },
      connection: {
        endpoint: "https://your-agent-server.com/api/agent/chat",
        protocol: "jsonrpc2.0"
      }
    },
    AgentEndpoint: "https://your-agent-server.com/api/agent/chat",
    AgentDescription: "AI assistant that knows everything about the OASIS codebase"
  }
});
```

**Or update existing NFT:**

```javascript
// Get NFT ID first
const nfts = await oasis_get_nfts({ avatarId: yourAvatarId });
const foreignRelationsNFT = nfts.result.find(nft => nft.symbol === "FOREIGN_RELATIONS");

// Update NFT metadata
await oasis_update_holon({
  holonId: foreignRelationsNFT.id,
  holon: {
    metaData: {
      ...foreignRelationsNFT.metaData,
      AgentId: agentAvatarId,
      AgentEndpoint: "https://your-agent-server.com/api/agent/chat"
    }
  }
});
```

---

## Step 8: NFT Holders Access the Agent

### Frontend Integration

```javascript
// In your portal or app
async function chatWithOASISAgent(question) {
    // 1. Get user's NFTs
    const nfts = await loadNFTsForAvatar(avatarId);
    
    // 2. Find NFT with agent
    const nftWithAgent = nfts.find(nft => 
        nft.metaData?.AgentId || 
        nft.metaData?.AgentEndpoint
    );
    
    if (!nftWithAgent) {
        return { error: "You don't own an NFT with an agent" };
    }
    
    // 3. Call agent endpoint
    const response = await fetch(nftWithAgent.metaData.AgentEndpoint, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${jwtToken}`
        },
        body: JSON.stringify({
            question: question,
            avatar_id: avatarId,
            nft_id: nftWithAgent.id
        })
    });
    
    const result = await response.json();
    return result;
}
```

### UI Example

```html
<!-- Chat interface for NFT holders -->
<div class="agent-chat">
    <h3>Chat with OASIS Codebase Assistant</h3>
    <div id="chat-messages"></div>
    <input type="text" id="question-input" placeholder="Ask about OASIS...">
    <button onclick="askAgent()">Ask</button>
</div>

<script>
async function askAgent() {
    const question = document.getElementById('question-input').value;
    const response = await chatWithOASISAgent(question);
    
    // Display answer
    const messagesDiv = document.getElementById('chat-messages');
    messagesDiv.innerHTML += `
        <div class="question">Q: ${question}</div>
        <div class="answer">A: ${response.answer}</div>
    `;
}
</script>
```

---

## Step 9: Enhanced Features (Optional)

### Access Control

Only NFT holders can use the agent:

```python
@app.route('/api/agent/chat', methods=['POST'])
async def chat():
    data = request.json
    avatar_id = data.get('avatar_id')
    nft_id = data.get('nft_id')
    
    # Verify NFT ownership via OASIS API
    nft = await oasis_api.get_nft(nft_id)
    if nft.current_owner_avatar_id != avatar_id:
        return jsonify({"error": "You don't own this NFT"}), 403
    
    # Continue with chat...
```

### Usage Tracking

Track how the agent is used:

```python
# Log usage
await log_agent_usage({
    "nft_id": nft_id,
    "avatar_id": avatar_id,
    "question": question,
    "timestamp": datetime.utcnow()
});
```

### Rate Limiting

Limit usage per NFT holder:

```python
from flask_limiter import Limiter

limiter = Limiter(
    app,
    key_func=lambda: request.json.get('avatar_id'),
    default_limits=["10 per hour"]
)
```

---

## Complete Example: Full Implementation

### 1. Agent Service (Python)

```python
# oasis_agent_service.py
import os
from flask import Flask, request, jsonify
from langchain.vectorstores import Chroma
from langchain.embeddings import OpenAIEmbeddings
from langchain.document_loaders import DirectoryLoader
import openai

app = Flask(__name__)
agent = None

def initialize_agent():
    global agent
    # Load OASIS codebase
    loader = DirectoryLoader(
        '/Users/maxgershfield/OASIS_CLEAN',
        glob='**/*.{cs,ts,md,json}',
        recursive=True
    )
    documents = loader.load()
    
    # Create vector store
    agent = {
        'vectorstore': Chroma.from_documents(
            documents,
            OpenAIEmbeddings()
        )
    }

@app.route('/api/agent/chat', methods=['POST'])
async def chat():
    data = request.json
    question = data.get('question')
    
    # Search codebase
    docs = agent['vectorstore'].similarity_search(question, k=5)
    context = "\n".join([doc.page_content for doc in docs])
    
    # Generate answer
    response = openai.ChatCompletion.create(
        model="gpt-4",
        messages=[
            {"role": "system", "content": "You are an expert on the OASIS codebase."},
            {"role": "user", "content": f"Context:\n{context}\n\nQuestion: {question}"}
        ]
    )
    
    return jsonify({
        "answer": response.choices[0].message.content,
        "sources": [doc.metadata.get('source') for doc in docs]
    })

if __name__ == '__main__':
    initialize_agent()
    app.run(host='0.0.0.0', port=8080)
```

### 2. Deploy Agent

```bash
# Dockerfile
FROM python:3.11
WORKDIR /app
COPY requirements.txt .
RUN pip install -r requirements.txt
COPY oasis_agent_service.py .
CMD ["python", "oasis_agent_service.py"]
```

```bash
# Deploy
docker build -t oasis-codebase-agent .
docker run -d -p 8080:8080 oasis-codebase-agent
```

### 3. Link to NFT

```javascript
// Update "Foreign Relations" NFT
const agentEndpoint = "https://your-server.com/api/agent/chat";

await oasis_update_holon({
  holonId: foreignRelationsNFT.id,
  holon: {
    metaData: {
      ...foreignRelationsNFT.metaData,
      AgentId: agentAvatarId,
      AgentEndpoint: agentEndpoint,
      AgentName: "OASIS Codebase Assistant",
      AgentDescription: "Ask me anything about the OASIS codebase!"
    }
  }
});
```

---

## Summary

**Complete Flow:**

1. ✅ Create agent avatar
2. ✅ Register agent capabilities
3. ✅ Deploy agent service (with codebase knowledge)
4. ✅ Register as SERV service
5. ✅ Link agent to NFT via metadata
6. ✅ NFT holders can chat with agent

**The agent becomes a utility of the NFT** - only holders can access it, and it provides value through codebase expertise!

---

## Next Steps

1. **Choose knowledge approach**: RAG (recommended), pre-trained, or live access
2. **Deploy agent service**: Docker, cloud, or your server
3. **Link to NFT**: Update "Foreign Relations" NFT metadata
4. **Build UI**: Add chat interface to portal or create standalone app
5. **Test**: Verify NFT holders can access the agent

Would you like me to help implement any specific part of this?
