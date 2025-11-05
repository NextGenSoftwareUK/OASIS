/**
 * Contract Template Library
 * Pre-defined contract specifications for common patterns
 */

export interface ContractTemplate {
  id: string;
  name: string;
  description: string;
  blockchain: 'solana' | 'ethereum' | 'radix';
  spec: any;
}

export const SOLANA_TEMPLATES: ContractTemplate[] = [
  {
    id: 'token_vesting',
    name: 'Token Vesting',
    description: 'Lock and gradually release tokens over time',
    blockchain: 'solana',
    spec: {
      imports: ["anchor_lang::prelude::*"],
      programId: "Fg6PaFpoGXkYsidMpWTK6W2BeZ7FEfcYkg476zPFsLnS",
      programName: "token_vesting",
      instructions: [
        {
          name: "initialize",
          contextStruct: "Initialize",
          params: [
            { name: "amount", type: "u64" }
          ],
          description: "Initialize vesting schedule",
          body: ["Ok(())"]
        }
      ],
      accounts: [
        {
          name: "Initialize",
          fields: [
            { name: "authority", type: "Signer<'info>" },
            { name: "system_program", type: "Program<'info, System>" }
          ]
        }
      ],
      errors: [
        {
          name: "InvalidAmount",
          message: "Invalid amount provided",
          code: 6000
        }
      ]
    }
  },
  {
    id: 'nft_marketplace',
    name: 'NFT Marketplace',
    description: 'Buy, sell, and trade NFTs with escrow',
    blockchain: 'solana',
    spec: {
      imports: ["anchor_lang::prelude::*"],
      programId: "Fg6PaFpoGXkYsidMpWTK6W2BeZ7FEfcYkg476zPFsLnS",
      programName: "nft_marketplace",
      instructions: [
        {
          name: "list_nft",
          contextStruct: "ListNft",
          params: [{ name: "price", type: "u64" }],
          description: "List NFT for sale",
          body: ["Ok(())"]
        }
      ],
      accounts: [
        {
          name: "ListNft",
          fields: [
            { name: "seller", type: "Signer<'info>" },
            { name: "system_program", type: "Program<'info, System>" }
          ]
        }
      ],
      errors: [
        {
          name: "InvalidPrice",
          message: "Invalid price",
          code: 6000
        }
      ]
    }
  },
  {
    id: 'dao_governance',
    name: 'DAO Governance',
    description: 'Voting and proposal system for DAOs',
    blockchain: 'solana',
    spec: {
      imports: ["anchor_lang::prelude::*"],
      programId: "Fg6PaFpoGXkYsidMpWTK6W2BeZ7FEfcYkg476zPFsLnS",
      programName: "dao_governance",
      instructions: [
        {
          name: "create_proposal",
          contextStruct: "CreateProposal",
          params: [],
          description: "Create a new proposal",
          body: ["Ok(())"]
        }
      ],
      accounts: [
        {
          name: "CreateProposal",
          fields: [
            { name: "authority", type: "Signer<'info>" },
            { name: "system_program", type: "Program<'info, System>" }
          ]
        }
      ],
      errors: [
        {
          name: "Unauthorized",
          message: "Unauthorized access",
          code: 6000
        }
      ]
    }
  },
  {
    id: 'staking_pool',
    name: 'Staking Pool',
    description: 'Stake tokens and earn rewards',
    blockchain: 'solana',
    spec: {
      imports: ["anchor_lang::prelude::*"],
      programId: "Fg6PaFpoGXkYsidMpWTK6W2BeZ7FEfcYkg476zPFsLnS",
      programName: "staking_pool",
      instructions: [
        {
          name: "initialize_pool",
          contextStruct: "InitializePool",
          params: [{ name: "reward_rate", type: "u64" }],
          description: "Initialize staking pool",
          body: ["Ok(())"]
        }
      ],
      accounts: [
        {
          name: "InitializePool",
          fields: [
            { name: "authority", type: "Signer<'info>" },
            { name: "system_program", type: "Program<'info, System>" }
          ]
        }
      ],
      errors: [
        {
          name: "InvalidRate",
          message: "Invalid reward rate",
          code: 6000
        }
      ]
    }
  },
  {
    id: 'escrow',
    name: 'Escrow Contract',
    description: 'Secure peer-to-peer trading with escrow',
    blockchain: 'solana',
    spec: {
      imports: ["anchor_lang::prelude::*"],
      programId: "Fg6PaFpoGXkYsidMpWTK6W2BeZ7FEfcYkg476zPFsLnS",
      programName: "escrow",
      instructions: [
        {
          name: "initialize_escrow",
          contextStruct: "InitializeEscrow",
          params: [{ name: "amount", type: "u64" }],
          description: "Initialize escrow trade",
          body: ["Ok(())"]
        }
      ],
      accounts: [
        {
          name: "InitializeEscrow",
          fields: [
            { name: "initializer", type: "Signer<'info>" },
            { name: "system_program", type: "Program<'info, System>" }
          ]
        }
      ],
      errors: [
        {
          name: "InvalidAmount",
          message: "Invalid escrow amount",
          code: 6000
        }
      ]
    }
  }
];

/**
 * Get template by ID
 */
export function getTemplate(id: string): ContractTemplate | undefined {
  return SOLANA_TEMPLATES.find(t => t.id === id);
}

/**
 * Get all templates for a blockchain
 */
export function getTemplatesByBlockchain(blockchain: 'solana' | 'ethereum' | 'radix'): ContractTemplate[] {
  return SOLANA_TEMPLATES.filter(t => t.blockchain === blockchain);
}

