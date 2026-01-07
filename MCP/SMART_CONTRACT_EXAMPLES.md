# Smart Contract Generation Examples

## 🎯 Basic Usage

### Generate Contract from JSON

**You ask Cursor:**
> "Generate a Solana smart contract from this JSON specification: {programName: 'token_vesting', instructions: [{name: 'create_vesting', params: [{name: 'beneficiary', type: 'Pubkey'}]}]}"

**What happens:**
- MCP calls `scgen_generate_contract` tool
- Generates Rust/Anchor contract code
- Returns source code

**Response:**
```json
{
  "sourceCode": "use anchor_lang::prelude::*;\n\n#[program]\npub mod token_vesting {\n    use super::*;\n    \n    pub fn create_vesting(...) -> Result<()> {\n        // Generated code\n    }\n}",
  "filename": "lib.rs"
}
```

### Compile Contract

**You ask Cursor:**
> "Compile this Solidity contract for Ethereum: [contract code]"

**What happens:**
- MCP calls `scgen_compile_contract` tool
- Compiles contract
- Returns ZIP file with compiled bytecode and ABI

**Response:**
```json
{
  "compiledContract": "[binary data]",
  "zipPath": "/tmp/mcp-123456-compiled-contract.zip"
}
```

### Deploy Contract

**You ask Cursor:**
> "Deploy this compiled contract to Solana using wallet keypair at /path/to/keypair.json"

**What happens:**
- MCP calls `scgen_deploy_contract` tool
- Deploys to blockchain
- Returns contract address and transaction hash

**Response:**
```json
{
  "contractAddress": "7xKXtg2...",
  "transactionHash": "5xKXtg2..."
}
```

## 🔄 Complete Workflows

### Generate → Compile → Deploy

**You ask Cursor:**
> "Create a token vesting contract for Solana, compile it, and deploy it"

**What happens:**
1. Generates contract from JSON spec
2. Compiles Rust code
3. Deploys to Solana
4. Returns contract address

### Generate and Compile in One Step

**You ask Cursor:**
> "Generate and compile a DeFi lending contract for Ethereum from this spec: [JSON]"

**What happens:**
- MCP calls `scgen_generate_and_compile` tool
- Generates Solidity code
- Compiles immediately
- Returns both source and compiled contract

## 📋 JSON Specification Format

### Solana (Rust/Anchor) Example

```json
{
  "programName": "token_vesting",
  "instructions": [
    {
      "name": "create_vesting",
      "description": "Create a new vesting schedule",
      "params": [
        {
          "name": "beneficiary",
          "type": "Pubkey",
          "description": "Address receiving vested tokens"
        },
        {
          "name": "total_amount",
          "type": "u64",
          "description": "Total tokens to vest"
        }
      ],
      "body": [
        "// Vesting logic here"
      ]
    },
    {
      "name": "claim",
      "description": "Claim vested tokens",
      "params": [
        {
          "name": "vesting_account",
          "type": "Pubkey"
        }
      ]
    }
  ],
  "accounts": [
    {
      "name": "VestingAccount",
      "description": "Stores vesting information",
      "fields": [
        {
          "name": "beneficiary",
          "type": "Pubkey"
        },
        {
          "name": "total_amount",
          "type": "u64"
        },
        {
          "name": "released",
          "type": "u64"
        }
      ]
    }
  ],
  "constants": [
    {
      "name": "VESTING_DURATION",
      "type": "i64",
      "value": "31536000",
      "description": "Vesting duration in seconds (1 year)"
    }
  ]
}
```

### Ethereum (Solidity) Example

```json
{
  "contractName": "TokenVesting",
  "functions": [
    {
      "name": "createVesting",
      "visibility": "public",
      "params": [
        {
          "name": "beneficiary",
          "type": "address"
        },
        {
          "name": "totalAmount",
          "type": "uint256"
        }
      ]
    }
  ],
  "stateVariables": [
    {
      "name": "vestingSchedules",
      "type": "mapping(address => VestingSchedule)",
      "visibility": "private"
    }
  ]
}
```

## 💡 Pro Tips

1. **Start with JSON Spec:** Define your contract structure first
2. **Test Generation:** Generate and review before compiling
3. **Compile Locally First:** Test compilation before deployment
4. **Use Cache Stats:** Check `scgen_get_cache_stats` for performance

## 🔗 Integration with OASIS

You can combine smart contract operations with OASIS:

**Example:**
> "Create avatar 'bob', create wallet, generate token contract, deploy it, then mint an NFT using the contract address"

**What happens:**
1. Creates avatar
2. Creates wallet
3. Generates contract
4. Deploys contract
5. Mints NFT linked to contract

All through natural language in Cursor! 🚀





















