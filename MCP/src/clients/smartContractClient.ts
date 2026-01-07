import axios, { AxiosInstance } from 'axios';
import FormData from 'form-data';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { config } from '../config.js';

export class SmartContractClient {
  private client: AxiosInstance;
  private apiUrl: string;

  constructor() {
    this.apiUrl = process.env.SMART_CONTRACT_API_URL || 'http://localhost:5000';
    this.client = axios.create({
      baseURL: this.apiUrl,
      timeout: 300000, // 5 minutes for compilation
      maxContentLength: Infinity,
      maxBodyLength: Infinity,
    });
  }

  /**
   * Map blockchain name to API language enum value
   */
  private mapBlockchainToLanguage(blockchain: 'Ethereum' | 'Solana' | 'Radix'): string {
    const mapping: Record<string, string> = {
      Ethereum: 'Solidity',
      Solana: 'Rust',
      Radix: 'Scrypto',
    };
    return mapping[blockchain] || blockchain;
  }

  /**
   * Generate smart contract from JSON specification
   */
  async generateContract(
    jsonSpec: any,
    language: 'Ethereum' | 'Solana' | 'Radix'
  ): Promise<{ sourceCode: string; filename: string }> {
    // Create temporary JSON file
    const tempJsonPath = this.createTempFile(
      JSON.stringify(jsonSpec, null, 2),
      'contract-spec.json'
    );

    try {
      // Create form data
      const formData = new FormData();
      formData.append('JsonFile', fs.createReadStream(tempJsonPath));
      formData.append('Language', this.mapBlockchainToLanguage(language));

      const response = await this.client.post('/api/v1/contracts/generate', formData, {
        headers: formData.getHeaders(),
        responseType: 'text',
      });

      return {
        sourceCode: response.data,
        filename: this.getSourceFilename(language),
      };
    } finally {
      // Clean up temp file
      this.deleteTempFile(tempJsonPath);
    }
  }

  /**
   * Compile smart contract source code
   */
  async compileContract(
    sourceCode: string,
    language: 'Ethereum' | 'Solana' | 'Radix',
    filename?: string
  ): Promise<{ compiledContract: Buffer; abi?: any; zipPath?: string }> {
    const sourceFilename = filename || this.getSourceFilename(language);
    const tempSourcePath = this.createTempFile(sourceCode, sourceFilename);

    try {
      const formData = new FormData();
      formData.append('Source', fs.createReadStream(tempSourcePath));
      formData.append('Language', this.mapBlockchainToLanguage(language));

      const response = await this.client.post('/api/v1/contracts/compile', formData, {
        headers: formData.getHeaders(),
        responseType: 'arraybuffer',
      });

      // Save ZIP file
      const zipPath = this.saveTempFile(
        Buffer.from(response.data),
        'compiled-contract.zip'
      );

      return {
        compiledContract: Buffer.from(response.data),
        zipPath,
      };
    } finally {
      this.deleteTempFile(tempSourcePath);
    }
  }

  /**
   * Deploy compiled smart contract
   */
  async deployContract(
    compiledContractPath: string,
    language: 'Ethereum' | 'Solana' | 'Radix',
    abiPath?: string,
    walletKeypairPath?: string,
    oasisJwtToken?: string
  ): Promise<{ contractAddress: string; transactionHash: string }> {
    const formData = new FormData();
    formData.append(
      'CompiledContractFile',
      fs.createReadStream(compiledContractPath)
    );
    formData.append('Language', this.mapBlockchainToLanguage(language));

    // If OASIS JWT token is provided, use it instead of manual keypair/schema
    if (oasisJwtToken) {
      formData.append('OasisJwtToken', oasisJwtToken);
    } else {
      // Solana uses keypair, others use ABI/Schema
      if (language === 'Solana' && walletKeypairPath) {
        formData.append('WalletKeypair', fs.createReadStream(walletKeypairPath));
      } else if (abiPath) {
        formData.append('Schema', fs.createReadStream(abiPath));
      }
    }

    const response = await this.client.post('/api/v1/contracts/deploy', formData, {
      headers: formData.getHeaders(),
    });

    return {
      contractAddress: response.data.contractAddress || response.data.address,
      transactionHash: response.data.transactionHash || response.data.txHash,
    };
  }

  /**
   * Get compilation cache statistics
   */
  async getCacheStats() {
    const response = await this.client.get('/api/v1/contracts/cache-stats');
    return response.data;
  }

  /**
   * Helper: Create temporary file
   */
  private createTempFile(content: string, filename: string): string {
    const tempDir = os.tmpdir();
    const tempPath = path.join(tempDir, `mcp-${Date.now()}-${filename}`);
    fs.writeFileSync(tempPath, content);
    return tempPath;
  }

  /**
   * Helper: Save temporary file
   */
  private saveTempFile(data: Buffer, filename: string): string {
    const tempDir = os.tmpdir();
    const tempPath = path.join(tempDir, `mcp-${Date.now()}-${filename}`);
    fs.writeFileSync(tempPath, data);
    return tempPath;
  }

  /**
   * Helper: Delete temporary file
   */
  private deleteTempFile(filePath: string) {
    try {
      if (fs.existsSync(filePath)) {
        fs.unlinkSync(filePath);
      }
    } catch (error) {
      // Ignore cleanup errors
    }
  }

  /**
   * Helper: Get source filename based on language
   */
  private getSourceFilename(language: string): string {
    const extensions: Record<string, string> = {
      Ethereum: 'contract.sol',
      Solana: 'lib.rs',
      Radix: 'lib.rs',
    };
    return extensions[language] || 'contract.sol';
  }
}













