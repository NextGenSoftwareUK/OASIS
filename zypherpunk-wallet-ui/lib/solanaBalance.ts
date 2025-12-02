/**
 * Fetch Solana balance directly from blockchain RPC
 */

const SOLANA_RPC_URLS = {
  devnet: 'https://api.devnet.solana.com',
  testnet: 'https://api.testnet.solana.com',
  mainnet: 'https://api.mainnet-beta.solana.com',
};

export async function fetchSolanaBalance(
  address: string,
  network: 'devnet' | 'testnet' | 'mainnet' = 'devnet'
): Promise<number> {
  try {
    const rpcUrl = SOLANA_RPC_URLS[network];
    
    const response = await fetch(rpcUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        jsonrpc: '2.0',
        id: 1,
        method: 'getBalance',
        params: [address],
      }),
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    
    if (data.error) {
      throw new Error(data.error.message || 'Failed to fetch balance');
    }

    // Convert lamports to SOL (1 SOL = 1,000,000,000 lamports)
    const lamports = data.result?.value || 0;
    const sol = lamports / 1_000_000_000;
    
    return sol;
  } catch (error) {
    console.error('Error fetching Solana balance:', error);
    throw error;
  }
}

