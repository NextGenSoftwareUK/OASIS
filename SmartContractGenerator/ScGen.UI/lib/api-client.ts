/**
 * API Client for Smart Contract Generator
 * Integrates with your existing API at localhost:5000
 */

export interface ContractSpec {
  programName?: string;
  programId?: string;
  instructions?: Array<{
    name: string;
    params?: Array<{ name: string; type: string }>;
    description?: string;
  }>;
  [key: string]: any;
}

export interface GeneratedContract {
  code: string;
  zipBlob: Blob;
  language: 'Solidity' | 'Rust' | 'Scrypto';
  programName?: string;
}

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

// Import OASIS auth helper
let getOASISToken: (() => string | null) | null = null;
if (typeof window !== 'undefined') {
  import('./oasis-auth').then(module => {
    getOASISToken = module.getOASISToken;
  });
}

/**
 * Generate contract from JSON specification (Template-based)
 */
export async function generateContract(
  spec: ContractSpec,
  language: 'Solidity' | 'Rust' | 'Scrypto',
  paymentToken?: string,
  walletAddress?: string
): Promise<GeneratedContract> {
  const jsonBlob = new Blob([JSON.stringify(spec, null, 2)], { type: 'application/json' });
  const jsonFile = new File([jsonBlob], 'spec.json', { type: 'application/json' });
  
  const formData = new FormData();
  formData.append('JsonFile', jsonFile);
  formData.append('Language', language);

  const headers: HeadersInit = {};
  if (paymentToken) {
    headers['X-Payment-Token'] = paymentToken;
    headers['X-Payment-Protocol'] = 'x402-solana';
  }
  if (walletAddress) {
    headers['X-Wallet-Address'] = walletAddress;
  }
  // Add OASIS JWT token if available
  const oasisToken = getOASISToken?.();
  if (oasisToken) {
    headers['Authorization'] = `Bearer ${oasisToken}`;
    headers['X-OASIS-Token'] = oasisToken;
  }

  const response = await fetch(`${API_BASE_URL}/api/v1/contracts/generate`, {
    method: 'POST',
    body: formData,
    headers,
  });

  if (response.status === 402) {
    const paymentInfo = await response.json();
    throw new PaymentRequiredError(paymentInfo);
  }

  if (!response.ok) {
    const error = await response.text().catch(() => 'Unknown error');
    throw new Error(`Contract generation failed: ${response.status} - ${error}`);
  }

  const zipBlob = await response.blob();
  
  // Extract code from ZIP
  const code = await extractCodeFromZip(zipBlob, language);
  
  return {
    code,
    zipBlob,
    language,
    programName: spec.programName,
  };
}

/**
 * Payment Required Error
 */
export class PaymentRequiredError extends Error {
  constructor(public paymentInfo: any) {
    super('Payment required');
    this.name = 'PaymentRequiredError';
  }
}

/**
 * Generate contract from AI description (AI-powered)
 */
export async function generateWithAI(
  description: string,
  blockchain: 'solana' | 'ethereum' | 'radix',
  additionalContext?: string
): Promise<GeneratedContract> {
  const response = await fetch(`${API_BASE_URL}/api/v1/contracts/generate-ai`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      description,
      blockchain,
      additionalContext,
    }),
  });

  if (!response.ok) {
    const error = await response.text().catch(() => 'Unknown error');
    throw new Error(`AI generation failed: ${response.status} - ${error}`);
  }

  const result = await response.json();
  
  return {
    code: result.code,
    zipBlob: new Blob([result.code]),
    language: blockchain === 'solana' ? 'Rust' : blockchain === 'ethereum' ? 'Solidity' : 'Scrypto',
  };
}

/**
 * Compile contract
 */
export async function compileContract(
  source: string | Blob,
  language: 'Solidity' | 'Rust' | 'Scrypto',
  filename: string = 'contract.zip',
  timeoutMs: number = 1200000, // 20 minutes (first build with empty cache needs to download all dependencies)
  paymentToken?: string,
  walletAddress?: string
): Promise<Blob> {
  const formData = new FormData();
  formData.append('Language', language);
  
  // Handle both string (code) and Blob (ZIP) inputs
  if (source instanceof Blob) {
    formData.append('Source', source, filename);
  } else {
    const contractBlob = new Blob([source], { type: 'text/plain' });
    formData.append('Source', contractBlob, filename);
  }

  const headers: HeadersInit = {};
  if (paymentToken) {
    headers['X-Payment-Token'] = paymentToken;
    headers['X-Payment-Protocol'] = 'x402-solana';
  }
  if (walletAddress) {
    headers['X-Wallet-Address'] = walletAddress;
  }
  // Add OASIS JWT token if available
  const oasisToken = getOASISToken?.();
  if (oasisToken) {
    headers['Authorization'] = `Bearer ${oasisToken}`;
    headers['X-OASIS-Token'] = oasisToken;
  }

  // Create an AbortController for timeout
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch(`${API_BASE_URL}/api/v1/contracts/compile`, {
      method: 'POST',
      body: formData,
      headers,
      signal: controller.signal,
    });

    clearTimeout(timeoutId);

    if (response.status === 402) {
      const paymentInfo = await response.json();
      throw new PaymentRequiredError(paymentInfo);
    }

    if (!response.ok) {
      const error = await response.text().catch(() => 'Unknown error');
      throw new Error(`Compilation failed: ${response.status} - ${error}`);
    }

    return await response.blob();
  } catch (err: any) {
    clearTimeout(timeoutId);
    if (err.name === 'AbortError') {
      throw new Error(`Compilation timeout after ${timeoutMs / 1000}s. The API may be under heavy load.`);
    }
    throw err;
  }
}

/**
 * Deploy contract
 */
export async function deployContract(
  compiledBlob: Blob,
  language: 'Solidity' | 'Rust' | 'Scrypto',
  walletKeypair?: string | Blob,
  schema?: string,
  filename?: string,
  paymentToken?: string
): Promise<any> {
  const formData = new FormData();
  formData.append('Language', language);
  
  // Ensure the compiled file has the correct extension for Solana
  const defaultFilename = language === 'Rust' ? 'contract.so' : 'contract.bin';
  formData.append('CompiledContractFile', compiledBlob, filename || defaultFilename);
  
  // Add wallet keypair if provided
  if (walletKeypair) {
    if (walletKeypair instanceof Blob) {
      formData.append('WalletKeypair', walletKeypair, 'wallet.json');
    } else {
      const keypairBlob = new Blob([walletKeypair], { type: 'application/json' });
      formData.append('WalletKeypair', keypairBlob, 'wallet.json');
    }
  }
  
  // Schema is required by the API, provide empty object if not available
  const schemaContent = schema || '{}';
  const schemaBlob = new Blob([schemaContent], { type: 'application/json' });
  formData.append('Schema', schemaBlob, 'schema.json');

  const headers: HeadersInit = {};
  if (paymentToken) {
    headers['X-Payment-Token'] = paymentToken;
    headers['X-Payment-Protocol'] = 'x402-solana';
  }

  const response = await fetch(`${API_BASE_URL}/api/v1/contracts/deploy`, {
    method: 'POST',
    body: formData,
    headers,
  });

  if (response.status === 402) {
    const paymentInfo = await response.json();
    throw new PaymentRequiredError(paymentInfo);
  }

  if (!response.ok) {
    const error = await response.text().catch(() => 'Unknown error');
    throw new Error(`Deployment failed: ${response.status} - ${error}`);
  }

  return await response.json();
}

/**
 * Extract code from ZIP blob
 */
async function extractCodeFromZip(zipBlob: Blob, language: 'Solidity' | 'Rust' | 'Scrypto'): Promise<string> {
  // Dynamic import to avoid SSR issues
  const JSZip = (await import('jszip')).default;
  const zip = await JSZip.loadAsync(zipBlob);
  
  // Determine file paths based on language
  const possiblePaths = 
    language === 'Rust' 
      ? ['programs/rust-main-template/src/lib.rs', 'src/lib.rs', 'lib.rs']
      : language === 'Solidity'
      ? ['contracts/contract.sol', 'contract.sol', 'Contract.sol']
      : ['src/lib.rs', 'lib.rs']; // Scrypto
  
  for (const path of possiblePaths) {
    const file = zip.file(path);
    if (file) {
      return await file.async('string');
    }
  }
  
  // If no match, try to find any .rs or .sol file
  const files = Object.keys(zip.files);
  const extension = language === 'Solidity' ? '.sol' : '.rs';
  const matchingFile = files.find(f => f.endsWith(extension) && !f.includes('test'));
  
  if (matchingFile) {
    const file = zip.file(matchingFile);
    if (file) {
      return await file.async('string');
    }
  }
  
  throw new Error('Could not find contract code in ZIP');
}

/**
 * Cache stats interface
 */
export interface CacheStats {
  enabled: boolean;
  compileRequests?: number;
  cacheHits?: number;
  cacheMisses?: number;
  cacheHitRate?: number;
  compilations?: number;
  averageCompileTime?: number;
  averageCacheReadTime?: number;
  cacheSize?: string;
  message?: string;
}

/**
 * Get compilation cache statistics (sccache)
 */
export async function getCacheStats(): Promise<CacheStats> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/v1/contracts/cache-stats`, {
      method: 'GET',
    });

    if (!response.ok) {
      return { enabled: false, message: 'API error' };
    }

    return await response.json();
  } catch (error) {
    return { enabled: false, message: 'Network error' };
  }
}

