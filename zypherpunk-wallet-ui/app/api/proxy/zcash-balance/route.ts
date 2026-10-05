import { NextRequest, NextResponse } from 'next/server';

/**
 * Proxy route for fetching Zcash balances from CipherScan
 * Handles CORS issues by proxying requests through Next.js
 */
export async function GET(request: NextRequest) {
  try {
    const searchParams = request.nextUrl.searchParams;
    const address = searchParams.get('address');
    const network = searchParams.get('network') || 'testnet';

    console.log(`[Zcash Balance Proxy] Request received: address=${address}, network=${network}`);

    if (!address) {
      console.error('[Zcash Balance Proxy] Missing address parameter');
      return NextResponse.json(
        { error: 'Address parameter is required' },
        { status: 400 }
      );
    }

    // Determine the CipherScan API URL based on network
    const baseUrl = network === 'testnet' 
      ? 'https://testnet.cipherscan.app'
      : 'https://cipherscan.app';
    
    const apiUrl = `${baseUrl}/api/address/${address}`;
    console.log(`[Zcash Balance Proxy] Fetching from: ${apiUrl}`);

    // Fetch from CipherScan
    const response = await fetch(apiUrl, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
      // Add timeout
      signal: AbortSignal.timeout(10000), // 10 second timeout
    });

    console.log(`[Zcash Balance Proxy] Response status: ${response.status}`);

    if (!response.ok) {
      const errorText = await response.text();
      console.error(`[Zcash Balance Proxy] CipherScan API error (${response.status}):`, errorText);
      return NextResponse.json(
        { 
          error: `Failed to fetch balance: ${response.statusText}`,
          details: errorText,
          status: response.status
        },
        { status: response.status }
      );
    }

    const data = await response.json();
    console.log(`[Zcash Balance Proxy] Success: balance=${data.balance}, totalReceived=${data.totalReceived}`);
    
    return NextResponse.json(data, {
      headers: {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Methods': 'GET, OPTIONS',
        'Access-Control-Allow-Headers': 'Content-Type',
      },
    });
  } catch (error: any) {
    console.error('[Zcash Balance Proxy] Error proxying request:', error);
    return NextResponse.json(
      { 
        error: 'Internal server error',
        message: error.message || 'Unknown error',
        details: error.toString()
      },
      { status: 500 }
    );
  }
}

export async function OPTIONS() {
  return new NextResponse(null, {
    status: 200,
    headers: {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type',
    },
  });
}

