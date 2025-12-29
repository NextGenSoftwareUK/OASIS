import { Injectable, Logger, HttpException, HttpStatus } from '@nestjs/common';
import axios, { AxiosError } from 'axios';
import * as fs from 'fs';
import * as path from 'path';

// Use global FormData (Node.js 18+) or fallback to form-data package
let FormDataClass: any;
try {
  // Try to use global FormData first (Node.js 18+)
  if (typeof globalThis.FormData !== 'undefined') {
    FormDataClass = globalThis.FormData;
  } else {
    // Fallback to form-data package
    const formDataModule = require('form-data');
    FormDataClass = formDataModule.default || formDataModule;
  }
} catch (e) {
  // If form-data package is not available, use global FormData
  if (typeof globalThis.FormData !== 'undefined') {
    FormDataClass = globalThis.FormData;
  } else {
    throw new Error(
      'FormData is required. Please ensure Node.js 18+ or install form-data: npm install form-data',
    );
  }
}

export interface RwaTokenSpec {
  name: string;
  symbol: string;
  totalSupply: number;
  metadataUri: string;
  issuerWallet: string;
  decimals?: number;
}

export interface DeployResult {
  contractAddress: string;
  transactionHash?: string;
  programId?: string;
}

/**
 * Smart Contract Service
 * Integrates with SmartContractGenerator API to generate, compile, and deploy contracts
 * 
 * See: /Volumes/Storage/OASIS_CLEAN/pangea/task-briefs/05-smart-contract-generator-integration.md
 */
@Injectable()
export class SmartContractService {
  private readonly logger = new Logger(SmartContractService.name);
  private readonly baseUrl =
    process.env.SMART_CONTRACT_GENERATOR_URL || 'http://localhost:5000';
  private readonly maxRetries = 3;
  private readonly retryDelay = 5000; // 5 seconds

  /**
   * Generate and deploy an RWA token contract
   * @param spec RWA token specification
   * @returns Contract address
   */
  async generateRwaToken(spec: RwaTokenSpec): Promise<DeployResult> {
    this.logger.log(`Generating RWA token contract for ${spec.name} (${spec.symbol})`);

    try {
      // 1. Create JSON spec
      const contractSpec = {
        contract_type: 'rwa_token',
        blockchain: 'solana',
        language: 'Rust',
        framework: 'Anchor',
        spec: {
          name: spec.name,
          symbol: spec.symbol,
          total_supply: spec.totalSupply,
          decimals: spec.decimals ?? 0,
          metadata_uri: spec.metadataUri || '',
          features: ['mint', 'burn', 'transfer', 'freeze'],
          authority: spec.issuerWallet,
          freeze_authority: spec.issuerWallet,
        },
      };

      // 2. Generate → Compile → Deploy
      const result = await this.generateCompileDeploy(
        contractSpec,
        'Rust',
        'RWA Token',
      );

      this.logger.log(
        `✅ RWA Token contract deployed: ${result.contractAddress}`,
      );
      return result;
    } catch (error) {
      this.logger.error(
        `Failed to generate RWA token contract: ${error.message}`,
        error.stack,
      );
      throw new HttpException(
        `Failed to deploy RWA token contract: ${error.message}`,
        HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }

  /**
   * Deploy order book contract
   */
  async deployOrderBook(): Promise<DeployResult> {
    this.logger.log('Deploying Order Book contract');

    try {
      const contractSpec = {
        contract_type: 'order_book',
        blockchain: 'solana',
        language: 'Rust',
        framework: 'Anchor',
        spec: {
          features: [
            'create_buy_order',
            'create_sell_order',
            'cancel_order',
            'match_orders',
            'get_order_book',
            'get_user_orders',
            'get_order_by_id',
          ],
          order_types: ['limit', 'market'],
          matching_algorithm: 'price_time_priority',
        },
      };

      const result = await this.generateCompileDeploy(
        contractSpec,
        'Rust',
        'Order Book',
      );

      this.logger.log(
        `✅ Order Book contract deployed: ${result.contractAddress}`,
      );
      return result;
    } catch (error) {
      this.logger.error(
        `Failed to deploy Order Book contract: ${error.message}`,
        error.stack,
      );
      throw new HttpException(
        `Failed to deploy Order Book contract: ${error.message}`,
        HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }

  /**
   * Deploy trade execution contract
   */
  async deployTradeExecution(): Promise<DeployResult> {
    this.logger.log('Deploying Trade Execution contract');

    try {
      const contractSpec = {
        contract_type: 'trade_execution',
        blockchain: 'solana',
        language: 'Rust',
        framework: 'Anchor',
        spec: {
          features: [
            'execute_trade',
            'settle_trade',
            'get_trade_history',
            'calculate_fees',
          ],
          fee_structure: {
            platform_fee_percentage: 0.01,
            fee_recipient: process.env.PLATFORM_WALLET || '',
          },
        },
      };

      const result = await this.generateCompileDeploy(
        contractSpec,
        'Rust',
        'Trade Execution',
      );

      this.logger.log(
        `✅ Trade Execution contract deployed: ${result.contractAddress}`,
      );
      return result;
    } catch (error) {
      this.logger.error(
        `Failed to deploy Trade Execution contract: ${error.message}`,
        error.stack,
      );
      throw new HttpException(
        `Failed to deploy Trade Execution contract: ${error.message}`,
        HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }

  /**
   * Deploy vault contract
   */
  async deployVault(): Promise<DeployResult> {
    this.logger.log('Deploying Vault contract');

    try {
      const contractSpec = {
        contract_type: 'vault',
        blockchain: 'solana',
        language: 'Rust',
        framework: 'Anchor',
        spec: {
          features: [
            'deposit',
            'withdraw',
            'get_balance',
            'transfer_to_order',
            'lock_balance',
            'unlock_balance',
          ],
          supported_tokens: ['USDC', 'SOL'],
        },
      };

      const result = await this.generateCompileDeploy(
        contractSpec,
        'Rust',
        'Vault',
      );

      this.logger.log(`✅ Vault contract deployed: ${result.contractAddress}`);
      return result;
    } catch (error) {
      this.logger.error(
        `Failed to deploy Vault contract: ${error.message}`,
        error.stack,
      );
      throw new HttpException(
        `Failed to deploy Vault contract: ${error.message}`,
        HttpStatus.INTERNAL_SERVER_ERROR,
      );
    }
  }

  /**
   * Core method: Generate → Compile → Deploy flow
   */
  private async generateCompileDeploy(
    contractSpec: any,
    language: 'Rust' | 'Solidity' | 'Scrypto',
    contractName: string,
  ): Promise<DeployResult> {
    let lastError: Error | null = null;

    for (let attempt = 1; attempt <= this.maxRetries; attempt++) {
      try {
        this.logger.log(
          `[${contractName}] Attempt ${attempt}/${this.maxRetries}: Starting generation...`,
        );

        // Step 1: Generate contract
        const generatedBlob = await this.generateContract(contractSpec, language);
        this.logger.log(`[${contractName}] ✅ Contract generated`);

        // Step 2: Compile contract
        const compiledBlob = await this.compileContract(
          generatedBlob,
          language,
          contractName,
        );
        this.logger.log(`[${contractName}] ✅ Contract compiled`);

        // Step 3: Deploy contract
        const deployResult = await this.deployContract(
          compiledBlob,
          language,
          contractName,
        );
        this.logger.log(`[${contractName}] ✅ Contract deployed`);

        return deployResult;
      } catch (error) {
        lastError = error as Error;
        this.logger.warn(
          `[${contractName}] Attempt ${attempt} failed: ${error.message}`,
        );

        if (attempt < this.maxRetries) {
          const delay = this.retryDelay * attempt;
          this.logger.log(
            `[${contractName}] Retrying in ${delay / 1000}s...`,
          );
          await this.sleep(delay);
        }
      }
    }

    throw new Error(
      `Failed after ${this.maxRetries} attempts: ${lastError?.message}`,
    );
  }

  /**
   * Step 1: Generate contract from JSON spec
   */
  private async generateContract(
    spec: any,
    language: 'Rust' | 'Solidity' | 'Scrypto',
  ): Promise<Buffer> {
    const formData = new FormDataClass();
    const jsonBuffer = Buffer.from(JSON.stringify(spec, null, 2));
    
    // Handle both form-data package and global FormData
    if (formData.append) {
      // form-data package
      formData.append('JsonFile', jsonBuffer, {
        filename: 'spec.json',
        contentType: 'application/json',
      });
      formData.append('Language', language);
    } else {
      // Global FormData (Node.js 18+)
      const blob = new Blob([jsonBuffer], { type: 'application/json' });
      formData.append('JsonFile', blob, 'spec.json');
      formData.append('Language', language);
    }

    try {
      const headers = formData.getHeaders ? formData.getHeaders() : {};
      const response = await axios.post(
        `${this.baseUrl}/api/v1/contracts/generate`,
        formData,
        {
          headers,
          responseType: 'arraybuffer',
          timeout: 300000, // 5 minutes for generation
        },
      );

      return Buffer.from(response.data);
    } catch (error) {
      const axiosError = error as AxiosError;
      throw new Error(
        `Contract generation failed: ${this.getErrorMessage(axiosError)}`,
      );
    }
  }

  /**
   * Step 2: Compile contract
   */
  private async compileContract(
    sourceBlob: Buffer,
    language: 'Rust' | 'Solidity' | 'Scrypto',
    contractName: string,
  ): Promise<Buffer> {
    const formData = new FormDataClass();
    
    // Handle both form-data package and global FormData
    if (formData.append && typeof formData.append === 'function' && formData.append.length > 2) {
      // form-data package
      formData.append('Source', sourceBlob, {
        filename: 'contract.zip',
        contentType: 'application/zip',
      });
      formData.append('Language', language);
    } else {
      // Global FormData (Node.js 18+)
      const blobArray = Buffer.isBuffer(sourceBlob)
        ? new Uint8Array(sourceBlob)
        : sourceBlob;
      const blob = new Blob([blobArray], { type: 'application/zip' });
      formData.append('Source', blob, 'contract.zip');
      formData.append('Language', language);
    }

    try {
      this.logger.log(
        `[${contractName}] Compiling contract (this may take 20+ minutes for first build)...`,
      );

      const headers = formData.getHeaders ? formData.getHeaders() : {};
      const response = await axios.post(
        `${this.baseUrl}/api/v1/contracts/compile`,
        formData,
        {
          headers,
          responseType: 'arraybuffer',
          timeout: 1200000, // 20 minutes for compilation
        },
      );

      return Buffer.from(response.data);
    } catch (error) {
      const axiosError = error as AxiosError;
      if (axiosError.code === 'ECONNABORTED') {
        throw new Error(
          'Compilation timeout. The API may be under heavy load. Please try again.',
        );
      }
      throw new Error(
        `Contract compilation failed: ${this.getErrorMessage(axiosError)}`,
      );
    }
  }

  /**
   * Step 3: Deploy contract to blockchain
   */
  private async deployContract(
    compiledBlob: Buffer,
    language: 'Rust' | 'Solidity' | 'Scrypto',
    contractName: string,
  ): Promise<DeployResult> {
    const formData = new FormDataClass();

    // Determine file extension based on language
    const extension = language === 'Rust' ? 'so' : 'bin';
    const filename = `program.${extension}`;

    // Handle both form-data package and global FormData
    const isFormDataPackage = formData.append && typeof formData.append === 'function' && formData.append.length > 2;
    
    if (isFormDataPackage) {
      // form-data package
      formData.append('Language', language);
      formData.append('CompiledContractFile', compiledBlob, {
        filename,
        contentType: 'application/octet-stream',
      });

      // Add wallet keypair if available (for Solana)
      const walletKeypairPath = process.env.SOLANA_WALLET_KEYPAIR_PATH;
      if (language === 'Rust' && walletKeypairPath) {
        try {
          const keypairContent = fs.readFileSync(walletKeypairPath, 'utf-8');
          const keypairBuffer = Buffer.from(keypairContent);
          formData.append('WalletKeypair', keypairBuffer, {
            filename: 'wallet.json',
            contentType: 'application/json',
          });
          this.logger.log(`[${contractName}] Using wallet keypair from ${walletKeypairPath}`);
        } catch (error) {
          this.logger.warn(
            `[${contractName}] Could not read wallet keypair: ${error.message}`,
          );
        }
      }

      // Schema is required by the API
      const schemaContent = '{}';
      formData.append('Schema', Buffer.from(schemaContent), {
        filename: 'schema.json',
        contentType: 'application/json',
      });
    } else {
      // Global FormData (Node.js 18+)
      const bufferArray = Buffer.isBuffer(compiledBlob) 
        ? new Uint8Array(compiledBlob)
        : compiledBlob;
      const compiledBlob_obj = new Blob([bufferArray], { type: 'application/octet-stream' });
      formData.append('Language', language);
      formData.append('CompiledContractFile', compiledBlob_obj, filename);

      // Add wallet keypair if available (for Solana)
      const walletKeypairPath = process.env.SOLANA_WALLET_KEYPAIR_PATH;
      if (language === 'Rust' && walletKeypairPath) {
        try {
          const keypairContent = fs.readFileSync(walletKeypairPath, 'utf-8');
          const keypairBlob = new Blob([keypairContent], { type: 'application/json' });
          formData.append('WalletKeypair', keypairBlob, 'wallet.json');
          this.logger.log(`[${contractName}] Using wallet keypair from ${walletKeypairPath}`);
        } catch (error) {
          this.logger.warn(
            `[${contractName}] Could not read wallet keypair: ${error.message}`,
          );
        }
      }

      // Schema is required by the API
      const schemaContent = '{}';
      const schemaBlob = new Blob([schemaContent], { type: 'application/json' });
      formData.append('Schema', schemaBlob, 'schema.json');
    }

    try {
      const headers = formData.getHeaders ? formData.getHeaders() : {};
      const response = await axios.post(
        `${this.baseUrl}/api/v1/contracts/deploy`,
        formData,
        {
          headers,
          timeout: 300000, // 5 minutes for deployment
        },
      );

      const result = response.data;
      return {
        contractAddress: result.contractAddress || result.programId || '',
        transactionHash: result.transactionHash || result.signature,
        programId: result.programId || result.contractAddress,
      };
    } catch (error) {
      const axiosError = error as AxiosError;
      throw new Error(
        `Contract deployment failed: ${this.getErrorMessage(axiosError)}`,
      );
    }
  }

  /**
   * Get cache statistics
   */
  async getCacheStats(): Promise<any> {
    try {
      const response = await axios.get(
        `${this.baseUrl}/api/v1/contracts/cache-stats`,
      );
      return response.data;
    } catch (error) {
      this.logger.warn(`Failed to get cache stats: ${error.message}`);
      return { enabled: false, message: 'API error' };
    }
  }

  /**
   * Helper: Extract error message from Axios error
   */
  private getErrorMessage(error: AxiosError): string {
    if (error.response) {
      const data = error.response.data;
      if (typeof data === 'string') {
        return data;
      }
      if (data && typeof data === 'object' && 'message' in data) {
        return (data as any).message;
      }
      return `HTTP ${error.response.status}: ${error.response.statusText}`;
    }
    if (error.request) {
      return 'No response from server. Is SmartContractGenerator running?';
    }
    return error.message || 'Unknown error';
  }

  /**
   * Helper: Sleep for specified milliseconds
   */
  private sleep(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}












