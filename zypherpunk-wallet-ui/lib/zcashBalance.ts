/**
 * Fetch Zcash balance using block explorer API
 * Uses CipherScan for reliable balance queries
 * Proxied through Next.js API route to avoid CORS issues
 */

export async function fetchZcashBalance(
  address: string,
  network: 'testnet' | 'mainnet' = 'testnet'
): Promise<number> {
  try {
    // Use Next.js API proxy route to avoid CORS issues
    const apiUrl = `/api/proxy/zcash-balance?address=${encodeURIComponent(address)}&network=${network}`;
    console.log(`[Zcash Balance] Fetching balance for ${address} from ${apiUrl}`);
    
    const response = await fetch(apiUrl, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    });

    console.log(`[Zcash Balance] Response status: ${response.status} for ${address}`);

    if (!response.ok) {
      let errorData;
      try {
        errorData = await response.json();
        console.error(`[Zcash Balance] Error response:`, errorData);
      } catch (e) {
        const errorText = await response.text();
        console.error(`[Zcash Balance] Error text:`, errorText);
        errorData = { error: errorText || `HTTP error! status: ${response.status}` };
      }
      throw new Error(errorData.error || errorData.message || `HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    console.log(`[Zcash Balance] Response data for ${address}:`, data);
    
    // CipherScan API returns: { balance, totalReceived, totalSent, txCount, ... }
    if (data.balance !== undefined) {
      const balance = parseFloat(data.balance) || 0;
      console.log(`💰 Fetched Zcash balance for ${address}: ${balance} ZEC`);
      return balance;
    }
    
    // Fallback to totalReceived if balance is not available
    if (data.totalReceived !== undefined) {
      const received = parseFloat(data.totalReceived) || 0;
      const sent = parseFloat(data.totalSent || 0) || 0;
      const balance = received - sent; // Calculate balance from received - sent
      console.log(`💰 Fetched Zcash balance for ${address}: ${balance} ZEC (calculated)`);
      return balance;
    }
    
    console.warn(`[Zcash Balance] No balance data found for address ${address}`, data);
    return 0;
  } catch (error: any) {
    console.error(`[Zcash Balance] Error fetching Zcash balance for ${address}:`, error);
    console.error(`[Zcash Balance] Error details:`, error.message, error.stack);
    return 0; // Return 0 on error rather than throwing
  }
}
