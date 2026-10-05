# Contract Generator UI

**Visual Interface for Multi-Chain Smart Contract Generation**

Developer-friendly UI for creating Solidity, Rust, and Scrypto smart contracts using templates.

---

## Overview

A Next.js application that provides a visual interface to the Smart Contract Generator API. Build smart contracts by:
- Filling out a visual form
- Writing JSON specifications
- Using natural language descriptions
- Selecting from templates

Supports **Ethereum (Solidity)**, **Solana (Rust/Anchor)**, and **Radix (Scrypto)**.

---

## Key Features

### 1. Multi-Chain Support
- **Ethereum:** Solidity contracts (ERC-20, ERC-721, custom)
- **Solana:** Rust/Anchor programs (SPL Token, custom)
- **Radix:** Scrypto blueprints (Components, Resources)

### 2. Generation Methods

#### Template-Based
- Pre-built templates for common patterns
- Fill-in-the-blank approach
- Fast (2-3 seconds)
- Free and reliable

#### Enhanced Generation
- Natural language descriptions
- GPT-4 generation
- Custom logic and features
- Advanced customization

#### JSON Specification
- Full control over contract structure
- Import/export specifications
- Version control friendly
- Advanced users

### 3. Code Preview
- Syntax highlighting
- Line numbers
- Copy to clipboard
- Download generated code

### 4. Compilation & Deployment
- One-click compile
- Multi-chain deployment
- Transaction tracking
- Explorer integration

---

## Quick Start

### Prerequisites
```bash
Node.js 18+
Smart Contract Generator API running (port 5000)
```

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Open browser
open http://localhost:3001
```

### Environment Variables

Create `.env.local`:
```bash
# Smart Contract Generator API
NEXT_PUBLIC_API_URL=http://localhost:5000

# Blockchain RPC Endpoints
NEXT_PUBLIC_ETHEREUM_RPC=http://127.0.0.1:8545
NEXT_PUBLIC_SOLANA_RPC=https://api.devnet.solana.com

# Optional: OpenAI for AI-enhanced generation
OPENAI_API_KEY=your_openai_key
```

---

## User Interface

### Step 1: Select Blockchain
Choose your target blockchain:
- Ethereum (EVM-compatible)
- Solana (SPL/Anchor)
- Radix (Scrypto)

### Step 2: Choose Generation Method
- **Template:** Select from pre-built templates
- **Enhanced:** Describe in natural language
- **JSON:** Provide full specification

### Step 3: Configure Contract
Fill out the form or edit JSON:
- Contract name
- Functions and parameters
- State variables
- Events
- Custom logic

### Step 4: Preview & Generate
- See live preview of generated code
- Syntax highlighting
- Line-by-line review
- Download or copy

### Step 5: Compile & Deploy
- One-click compilation
- Deploy to testnet/mainnet
- View on blockchain explorer
- Get contract address

---

## Technical Architecture

### Tech Stack
- **Framework:** Next.js 14 (App Router)
- **Language:** TypeScript
- **Styling:** Tailwind CSS
- **API Client:** Axios
- **Code Editor:** Monaco Editor
- **Blockchain:** Web3.js, Solana Web3.js

### Project Structure

```
src/
├── app/                    # Next.js pages
│   ├── page.tsx           # Landing page
│   ├── generate/          # Contract generation
│   ├── compile/           # Compilation
│   └── deploy/            # Deployment
├── components/
│   ├── contract/          # Contract builder components
│   ├── editor/            # Code editor
│   ├── blockchain/        # Blockchain selectors
│   └── ui/                # Reusable UI
├── services/
│   ├── api.ts             # API client
│   ├── compiler.ts        # Compilation service
│   └── deployer.ts        # Deployment service
└── types/                 # TypeScript types
```

---

## API Integration

### Generate Contract

```typescript
POST /api/v1/contracts/generate
Content-Type: multipart/form-data

{
  "Language": "Rust",
  "JsonFile": specification.json
}
```

### Compile Contract

```typescript
POST /api/v1/contracts/compile
Content-Type: multipart/form-data

{
  "Language": "Rust",
  "Source": contract.rs
}
```

### Deploy Contract

```typescript
POST /api/v1/contracts/deploy
Content-Type: multipart/form-data

{
  "Language": "Rust",
  "CompiledContractFile": program.so
}
```

---

## Templates Available

### Ethereum Templates
- **ERC-20 Token:** Standard fungible token
- **ERC-721 NFT:** Non-fungible token
- **ERC-1155 Multi-Token:** Semi-fungible tokens
- **Multisig Wallet:** Multi-signature wallet
- **Timelock:** Time-locked transactions
- **DAO:** Decentralized autonomous organization

### Solana Templates
- **SPL Token:** Standard token program
- **NFT Collection:** Metaplex-compatible NFTs
- **Staking Program:** Token staking with rewards
- **Escrow:** Trustless escrow service
- **DAO:** On-chain governance
- **AMM:** Automated market maker

### Radix Templates
- **Fungible Token:** Native tokens
- **NFT Collection:** Unique resources
- **DEX:** Decentralized exchange
- **Lending Pool:** DeFi lending
- **DAO:** Governance system

---

## UI Components

### Contract Builder
- Visual form for contract parameters
- Real-time validation
- Field descriptions and examples
- Smart defaults

### Code Editor
- Monaco Editor integration
- Syntax highlighting for Rust, Solidity, Scrypto
- Auto-completion
- Error highlighting
- Dark mode

### Blockchain Selector
- Visual chain selection
- Network switcher (mainnet/testnet)
- RPC endpoint configuration
- Wallet integration

---

## Testing

### Run Tests
```bash
npm test
```

### Test Generation
1. Select Solana
2. Choose "Template-Based"
3. Pick "SPL Token"
4. Fill in token details
5. Click "Generate"
6. Verify Rust code appears

### Test Compilation
1. Use generated code
2. Click "Compile"
3. Wait for compilation (30-60 sec)
4. Download .so file
5. Verify file size

### Test Deployment
1. Have test wallet ready
2. Load compiled contract
3. Click "Deploy"
4. Confirm transaction
5. Get program ID

---

## Troubleshooting

### API Connection Issues
```bash
# Check API is running
curl http://localhost:5000/health

# Restart API if needed
cd ../../smart-contract-generator/src/SmartContractGen/ScGen.API
dotnet run
```

### Generation Fails
- Verify JSON specification is valid
- Check all required fields are filled
- Try a template first
- Check API logs

### Compilation Errors
- Verify source code syntax
- Check dependencies are correct
- Try example contracts first
- Review error messages

### Deployment Fails
- Check wallet has sufficient balance
- Verify network connectivity
- Try alternative RPC endpoint
- Check contract size limits

---

## Examples

### Example 1: Simple Token (Solana)

```json
{
  "programName": "simple_token",
  "instructions": [
    {
      "name": "initialize",
      "contextStruct": "Initialize",
      "params": []
    },
    {
      "name": "mint",
      "contextStruct": "Mint",
      "params": [
        {
          "name": "amount",
          "type": "u64"
        }
      ]
    }
  ],
  "accounts": [
    {
      "name": "Initialize",
      "fields": [
        {
          "name": "authority",
          "type": "Signer<'info>"
        }
      ]
    }
  ]
}
```

### Example 2: Simple Storage (Ethereum)

```json
{
  "pragmaVersion": "^0.8.19",
  "name": "SimpleStorage",
  "state": [
    {
      "name": "storedData",
      "type": "uint256",
      "visibility": "private"
    }
  ],
  "functions": [
    {
      "name": "set",
      "params": [
        {
          "name": "x",
          "type": "uint256"
        }
      ],
      "body": ["storedData = x;"]
    },
    {
      "name": "get",
      "returnType": "uint256",
      "body": ["return storedData;"]
    }
  ]
}
```

---

## Security Notes

### For Production Use
- [ ] Audit all generated contracts
- [ ] Test thoroughly on testnet
- [ ] Review generated code manually
- [ ] Use formal verification tools
- [ ] Get professional security audit

### Best Practices
- Start with templates
- Test on testnet first
- Use small amounts initially
- Keep private keys secure
- Monitor deployed contracts

---

## Additional Resources

- [Smart Contract Generator API Docs](../../smart-contract-generator/README.md)
- [Solidity Documentation](https://docs.soliditylang.org/)
- [Anchor Documentation](https://www.anchor-lang.com/)
- [Scrypto Documentation](https://docs.radixdlt.com/docs/scrypto-introduction)

---

## Contributing

Contributions welcome!

1. Fork the repo
2. Create feature branch
3. Add features or fix bugs
4. Test thoroughly
5. Submit pull request

---

## License

MIT License - see LICENSE file for details

---

## Acknowledgments

- Smart Contract Generator API team
- OpenAI for enhanced generation
- Monaco Editor team
- Next.js team

---

**Built for Multi-Chain Smart Contract Development**
