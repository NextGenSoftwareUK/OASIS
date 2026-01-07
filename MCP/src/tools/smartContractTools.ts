import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { SmartContractClient } from '../clients/smartContractClient.js';

const scClient = new SmartContractClient();

export const smartContractTools: Tool[] = [
  {
    name: 'scgen_generate_contract',
    description: 'Generate smart contract source code from JSON specification',
    inputSchema: {
      type: 'object',
      properties: {
        jsonSpec: {
          type: 'object',
          description: 'JSON contract specification (programName, instructions, accounts, constants, etc.)',
        },
        blockchain: {
          type: 'string',
          enum: ['Ethereum', 'Solana', 'Radix'],
          description: 'Target blockchain platform',
        },
      },
      required: ['jsonSpec', 'blockchain'],
    },
  },
  {
    name: 'scgen_compile_contract',
    description: 'Compile smart contract source code into deployable bytecode',
    inputSchema: {
      type: 'object',
      properties: {
        sourceCode: {
          type: 'string',
          description: 'Smart contract source code',
        },
        blockchain: {
          type: 'string',
          enum: ['Ethereum', 'Solana', 'Radix'],
          description: 'Target blockchain platform',
        },
        filename: {
          type: 'string',
          description: 'Source filename (optional, auto-detected if not provided)',
        },
      },
      required: ['sourceCode', 'blockchain'],
    },
  },
  {
    name: 'scgen_deploy_contract',
    description: 'Deploy a compiled smart contract to the blockchain. Can use OASIS avatar wallet via JWT token, or provide manual keypair/schema file.',
    inputSchema: {
      type: 'object',
      properties: {
        compiledContractPath: {
          type: 'string',
          description: 'Path to compiled contract file (ZIP for Solana/Radix, .bin for Ethereum)',
        },
        blockchain: {
          type: 'string',
          enum: ['Ethereum', 'Solana', 'Radix'],
          description: 'Target blockchain platform',
        },
        abiPath: {
          type: 'string',
          description: 'Path to ABI/Schema file (required for Ethereum/Radix if not using OASIS)',
        },
        walletKeypairPath: {
          type: 'string',
          description: 'Path to wallet keypair file (required for Solana if not using OASIS)',
        },
        oasisJwtToken: {
          type: 'string',
          description: 'Optional OASIS JWT token to use avatar wallet automatically (for Solana, fetches wallet from OASIS)',
        },
      },
      required: ['compiledContractPath', 'blockchain'],
    },
  },
  {
    name: 'scgen_generate_and_compile',
    description: 'Generate and compile a smart contract in one step',
    inputSchema: {
      type: 'object',
      properties: {
        jsonSpec: {
          type: 'object',
          description: 'JSON contract specification',
        },
        blockchain: {
          type: 'string',
          enum: ['Ethereum', 'Solana', 'Radix'],
          description: 'Target blockchain platform',
        },
      },
      required: ['jsonSpec', 'blockchain'],
    },
  },
  {
    name: 'scgen_get_cache_stats',
    description: 'Get compilation cache statistics',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
];

export async function handleSmartContractTool(
  name: string,
  args: any
): Promise<any> {
  try {
    switch (name) {
      case 'scgen_generate_contract': {
        if (!args.jsonSpec || !args.blockchain) {
          throw new Error('jsonSpec and blockchain are required');
        }
        const language = args.blockchain as 'Ethereum' | 'Solana' | 'Radix';
        return await scClient.generateContract(args.jsonSpec, language);
      }

      case 'scgen_compile_contract': {
        if (!args.sourceCode || !args.blockchain) {
          throw new Error('sourceCode and blockchain are required');
        }
        const language = args.blockchain as 'Ethereum' | 'Solana' | 'Radix';
        return await scClient.compileContract(
          args.sourceCode,
          language,
          args.filename
        );
      }

      case 'scgen_deploy_contract': {
        if (!args.compiledContractPath || !args.blockchain) {
          throw new Error('compiledContractPath and blockchain are required');
        }
        const language = args.blockchain as 'Ethereum' | 'Solana' | 'Radix';
        return await scClient.deployContract(
          args.compiledContractPath,
          language,
          args.abiPath,
          args.walletKeypairPath,
          args.oasisJwtToken
        );
      }

      case 'scgen_generate_and_compile': {
        if (!args.jsonSpec || !args.blockchain) {
          throw new Error('jsonSpec and blockchain are required');
        }
        const language = args.blockchain as 'Ethereum' | 'Solana' | 'Radix';
        
        // Generate
        const generated = await scClient.generateContract(args.jsonSpec, language);
        
        // Compile
        const compiled = await scClient.compileContract(
          generated.sourceCode,
          language,
          generated.filename
        );

        return {
          sourceCode: generated.sourceCode,
          compiledContract: {
            zipPath: compiled.zipPath,
            size: compiled.compiledContract.length,
          },
        };
      }

      case 'scgen_get_cache_stats': {
        return await scClient.getCacheStats();
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (error: any) {
    return {
      error: true,
      message: error.message || 'Unknown error',
      details: error.response?.data || error.stack,
    };
  }
}














