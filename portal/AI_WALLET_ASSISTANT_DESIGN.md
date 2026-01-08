# AI Wallet Assistant Design

## Overview
Following the pattern established by the AI NFT Assistant, the AI Wallet Assistant will provide natural language interaction for wallet management operations in the OASIS Portal.

## Key Features

### 1. Natural Language Wallet Operations
Users can describe wallet actions in plain English:
- **Wallet Creation**: "Create an Ethereum wallet named 'My DeFi Wallet'"
- **Token Transfers**: "Send 0.5 ETH to 0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb"
- **Balance Queries**: "Show me my Solana wallet balance"
- **Portfolio Analysis**: "What's my total portfolio value across all chains?"
- **Transaction History**: "Show my last 5 transactions"
- **Wallet Information**: "What wallets do I have on Ethereum?"

### 2. Smart Wallet Recommendations
- "Which wallet should I use for DeFi transactions?"
- "What's the best wallet for storing NFTs?"
- "Recommend a wallet for high-frequency trading"

### 3. Security & Best Practices
- "How do I secure my wallet?"
- "What's the difference between hot and cold wallets?"
- "How do I backup my wallet?"

### 4. Multi-Chain Operations
- "Create wallets for Ethereum, Solana, and Polygon"
- "Show me balances across all my wallets"
- "Transfer 100 USDC from Ethereum to Polygon"

### 5. Portfolio Insights
- "What's my portfolio breakdown by chain?"
- "Which token has the highest value in my portfolio?"
- "Show me my top 5 holdings"

## UI Design

### Chat Interface (Similar to NFT Assistant)
```
┌─────────────────────────────────────────┐
│  AI Wallet Assistant                    │
│  Ask me anything about your wallets     │
├─────────────────────────────────────────┤
│                                         │
│  [Welcome Message]                      │
│  [User Messages]                        │
│  [AI Responses]                         │
│  [Action Previews]                      │
│                                         │
├─────────────────────────────────────────┤
│  [Example Prompts]                      │
│  [Input Field]                          │
│  [Send Button]                          │
└─────────────────────────────────────────┘
```

### Mode Toggle
- **Form Mode**: Traditional form-based wallet operations
- **AI Mode**: Natural language chat interface

### Example Prompts
- "Create Wallet" - Shows wallet creation example
- "Send Token" - Shows token transfer example
- "Check Balance" - Shows balance query example
- "Portfolio" - Shows portfolio analysis example

## Supported Intents

### 1. Create Wallet
**Intent**: `create_wallet`
**Parameters**:
- `walletName` (string): Name for the wallet
- `providerType` (string): Blockchain provider (Ethereum, Solana, etc.)
- `description` (string, optional): Wallet description
- `isDefault` (boolean, optional): Set as default wallet

**Examples**:
- "Create an Ethereum wallet called 'Main Wallet'"
- "Make a Solana wallet named 'NFT Storage' and set it as default"

### 2. Send Token
**Intent**: `send_token`
**Parameters**:
- `amount` (decimal): Amount to send
- `tokenSymbol` (string): Token symbol (ETH, SOL, USDC, etc.)
- `toAddress` (string): Recipient address
- `fromWallet` (string, optional): Source wallet address or name
- `memo` (string, optional): Transaction memo

**Examples**:
- "Send 0.5 ETH to 0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb"
- "Transfer 100 USDC from my main wallet to 0x123..."

### 3. Get Balance
**Intent**: `get_balance`
**Parameters**:
- `walletAddress` (string, optional): Specific wallet address
- `providerType` (string, optional): Filter by blockchain
- `tokenSymbol` (string, optional): Specific token balance

**Examples**:
- "Show me my Ethereum wallet balance"
- "What's my SOL balance?"
- "Check balance for wallet 0x123..."

### 4. List Wallets
**Intent**: `list_wallets`
**Parameters**:
- `providerType` (string, optional): Filter by blockchain
- `includeBalances` (boolean, optional): Include balance information

**Examples**:
- "Show all my wallets"
- "List my Solana wallets"
- "What wallets do I have?"

### 5. Get Portfolio Value
**Intent**: `get_portfolio_value`
**Parameters**:
- `breakdown` (boolean, optional): Show breakdown by chain/token
- `currency` (string, optional): Display currency (USD, EUR, etc.)

**Examples**:
- "What's my total portfolio value?"
- "Show my portfolio breakdown"
- "How much is my portfolio worth in USD?"

### 6. Get Transaction History
**Intent**: `get_transactions`
**Parameters**:
- `walletAddress` (string, optional): Filter by wallet
- `limit` (number, optional): Number of transactions
- `providerType` (string, optional): Filter by blockchain

**Examples**:
- "Show my last 10 transactions"
- "What transactions did I make on Ethereum?"
- "Show transaction history for wallet 0x123..."

### 7. Import Wallet
**Intent**: `import_wallet`
**Parameters**:
- `keyType` (string): "private" or "public"
- `key` (string): Private or public key
- `providerType` (string): Blockchain provider
- `walletName` (string, optional): Name for imported wallet

**Examples**:
- "Import wallet from private key 0xabc... for Ethereum"
- "Import a Solana wallet using this public key"

### 8. Set Default Wallet
**Intent**: `set_default_wallet`
**Parameters**:
- `walletAddress` (string): Wallet address or name
- `providerType` (string): Blockchain provider

**Examples**:
- "Set my main Ethereum wallet as default"
- "Make wallet 0x123... the default for Solana"

### 9. Get Wallet Analytics
**Intent**: `get_wallet_analytics`
**Parameters**:
- `walletAddress` (string): Wallet address
- `providerType` (string): Blockchain provider

**Examples**:
- "Show analytics for my Ethereum wallet"
- "What's the transaction volume for wallet 0x123...?"

### 10. Get Supported Chains
**Intent**: `get_supported_chains`
**Parameters**: None

**Examples**:
- "What blockchains are supported?"
- "Show me all supported chains"

## Implementation Structure

### File: `ai-wallet-assistant.js`

```javascript
// AI Wallet Assistant State
const aiWalletAssistantState = {
    isActive: false,
    userInput: '',
    parsedData: null,
    isLoading: false,
    error: null,
    apiKey: null,
    wallets: [],
    portfolio: null
};

// Main Functions:
- initAIWalletAssistant()
- renderAIWalletAssistant()
- processAIRequest()
- executeAIIntent()
- callOpenAIAPI()
- buildAIContext()
```

### Integration Points

1. **Wallet Module Integration**
   - Uses `walletState` from `wallet.js`
   - Shares authentication and API base URL
   - Can trigger wallet operations

2. **Backend API Endpoint**
   - `/api/ai/parse-wallet-intent` (similar to NFT assistant)
   - Accepts natural language input
   - Returns structured intent and parameters

3. **UI Integration**
   - Add mode toggle in wallet tab (Form Mode / AI Mode)
   - Render AI assistant when AI mode is active
   - Show action previews before execution

## Example User Flows

### Flow 1: Creating a Wallet
```
User: "Create an Ethereum wallet named 'My DeFi Wallet'"
AI: [Parsing...]
AI: "I'll create an Ethereum wallet called 'My DeFi Wallet'. Here's what I'll do:"
     - Wallet Name: My DeFi Wallet
     - Provider: Ethereum
     - Generate Key Pair: Yes
     [Create Wallet Button]
User: [Clicks Create Wallet]
AI: "✅ Wallet created successfully! Address: 0x..."
```

### Flow 2: Sending Tokens
```
User: "Send 0.5 ETH to 0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb"
AI: [Parsing...]
AI: "I'll send 0.5 ETH to 0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb. Here's the transaction:"
     - Amount: 0.5 ETH
     - To: 0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb
     - From: [Your Main Wallet]
     [Send Token Button]
User: [Clicks Send Token]
AI: "✅ Transaction sent! Hash: 0x..."
```

### Flow 3: Portfolio Query
```
User: "What's my total portfolio value?"
AI: [Fetching data...]
AI: "Your total portfolio value is $15,420.50 USD"
     Breakdown:
     - Ethereum: $8,500.25 (3 wallets)
     - Bitcoin: $3,200.15 (1 wallet)
     - Solana: $2,100.10 (2 wallets)
     - Polygon: $1,620.00 (1 wallet)
```

## Security Considerations

1. **Private Key Handling**
   - Never display full private keys in chat
   - Mask sensitive information in responses
   - Use secure backend for key operations

2. **Transaction Confirmation**
   - Always show transaction preview before execution
   - Require explicit confirmation for sends
   - Display transaction hash after completion

3. **API Key Security**
   - Store OpenAI API key on backend
   - Never expose in frontend code
   - Use secure endpoint for AI parsing

## Future Enhancements

1. **Voice Input**: Allow voice commands for wallet operations
2. **Smart Suggestions**: Proactive wallet management suggestions
3. **Transaction Templates**: Save common transaction patterns
4. **Multi-Wallet Operations**: Batch operations across wallets
5. **Price Alerts**: Set up alerts via natural language
6. **DeFi Integration**: Query DeFi positions and yields
7. **Gas Optimization**: AI suggestions for optimal gas prices
8. **Security Scanning**: AI-powered wallet security analysis

## Technical Requirements

1. **Backend Endpoint**: `/api/ai/parse-wallet-intent`
   - Accepts: `{ userInput: string, context: object }`
   - Returns: `{ intent: string, parameters: object, isValid: boolean }`

2. **OpenAI Integration**: Similar to NFT assistant
   - Use GPT-4 or similar for intent parsing
   - Structured output for wallet operations
   - Context-aware responses

3. **Error Handling**: 
   - Graceful fallback for parsing errors
   - User-friendly error messages
   - Retry mechanisms

4. **State Management**:
   - Maintain conversation history
   - Remember user preferences
   - Cache wallet data

## UI Components

1. **Chat Container**: Scrollable message area
2. **Input Field**: Multi-line text input
3. **Example Prompts**: Quick action buttons
4. **Action Preview**: Structured preview of parsed actions
5. **Confirmation Dialogs**: For sensitive operations
6. **Loading States**: Spinner during processing
7. **Error Messages**: Clear error display
8. **Success Notifications**: Transaction confirmations

## Integration with Existing Wallet Module

The AI assistant will:
- Use `walletState` from `wallet.js` for wallet data
- Call existing wallet functions (createWallet, sendToken, etc.)
- Share authentication state
- Update wallet list after operations
- Refresh portfolio data

## Example Implementation Snippet

```javascript
// In wallet.js, add AI mode toggle
function renderWalletActions() {
    return `
        <div class="portal-section">
            <div style="display: flex; gap: 1rem; align-items: center;">
                <button onclick="showCreateWalletModal()">Create Wallet</button>
                <button onclick="showImportWalletModal()">Import Wallet</button>
                <button onclick="refreshWallets()">Refresh</button>
                <!-- Add AI Mode Toggle -->
                <div style="margin-left: auto;">
                    <button onclick="toggleWalletAIMode()">AI Assistant</button>
                </div>
            </div>
        </div>
    `;
}

// Add AI assistant rendering
function renderWalletDashboard() {
    if (walletState.mode === 'ai') {
        return renderAIWalletAssistant();
    }
    // ... existing dashboard code
}
```































