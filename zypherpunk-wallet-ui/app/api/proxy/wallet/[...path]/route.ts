import { NextRequest, NextResponse } from 'next/server';
import { execFile } from 'child_process';
import { promisify } from 'util';

const execFileAsync = promisify(execFile);

const API_BASE_URL = process.env.NEXT_PUBLIC_OASIS_API_URL || 'http://api.oasisplatform.world';
const CURL_TIMEOUT_SECONDS = 30;
const CURL_CONNECT_TIMEOUT_SECONDS = 10;

export async function GET(
  request: NextRequest,
  { params }: { params: { path: string[] } }
) {
  return handleRequest(request, params.path, 'GET');
}

export async function POST(
  request: NextRequest,
  { params }: { params: { path: string[] } }
) {
  return handleRequest(request, params.path, 'POST');
}

export async function PUT(
  request: NextRequest,
  { params }: { params: { path: string[] } }
) {
  return handleRequest(request, params.path, 'PUT');
}

export async function DELETE(
  request: NextRequest,
  { params }: { params: { path: string[] } }
) {
  return handleRequest(request, params.path, 'DELETE');
}

async function handleRequest(
  request: NextRequest,
  path: string[],
  method: string
) {
  try {
    // Reconstruct the full API path
    // path will be like ['avatar', '12345678-...', 'wallets']
    const pathString = path.join('/');
    let url = `${API_BASE_URL}/api/wallet/${pathString}`;
    
    // Get query string from original request
    const searchParams = request.nextUrl.searchParams.toString();
    const fullUrl = searchParams ? `${url}?${searchParams}` : url;

    // Check if this is a localhost HTTPS request (needs curl for self-signed certs)
    const isLocalhostHttps = fullUrl.includes('https://localhost') || fullUrl.includes('https://127.0.0.1');
    
    if (isLocalhostHttps) {
      // Use curl for localhost HTTPS to handle self-signed certificates
      return await handleRequestWithCurl(request, fullUrl, method);
    }

    // For non-localhost or HTTP, use regular fetch
    const authHeader = request.headers.get('authorization');
    
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    if (authHeader) {
      headers['Authorization'] = authHeader;
    }

    const options: RequestInit = {
      method,
      headers: {
        ...headers,
        'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
        'Accept': 'application/json, text/plain, */*',
      },
    };

    // Add body for POST/PUT requests
    if (method === 'POST' || method === 'PUT') {
      const body = await request.text();
      if (body) {
        options.body = body;
      }
    }

    const response = await fetch(fullUrl, options);
    const data = await response.text();
    
    // Check if response is HTML (bot protection or error page)
    if (data.trim().startsWith('<!') || data.trim().startsWith('<html')) {
      console.error('API returned HTML instead of JSON:', data.substring(0, 200));
      return NextResponse.json(
        { 
          success: false, 
          isError: true,
          message: 'API returned HTML response (possible bot protection or server error)',
          data: null
        },
        { status: 502 }
      );
    }
    
    // Try to parse as JSON, fallback to text
    let jsonData;
    try {
      jsonData = JSON.parse(data);
    } catch (e) {
      console.error('Failed to parse JSON response:', data.substring(0, 200));
      return NextResponse.json(
        { 
          success: false, 
          isError: true,
          message: 'Invalid JSON response from API',
          data: null
        },
        { status: 502 }
      );
    }

    return NextResponse.json(jsonData, {
      status: response.status,
      headers: {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
        'Access-Control-Allow-Headers': 'Content-Type, Authorization',
      },
    });
  } catch (error: any) {
    console.error('Proxy error:', error);
    return NextResponse.json(
      { 
        success: false, 
        message: error.message || 'Proxy request failed' 
      },
      { status: 500 }
    );
  }
}

async function handleRequestWithCurl(
  request: NextRequest,
  fullUrl: string,
  method: string
): Promise<NextResponse> {
  try {
    const authHeader = request.headers.get('authorization');
    const curlArgs = [
      '-s',
      '-k', // Ignore SSL certificate errors
      '-X',
      method,
      fullUrl,
      '-H',
      'Content-Type: application/json',
      '-H',
      'Accept: application/json',
      '--max-time',
      String(CURL_TIMEOUT_SECONDS),
      '--connect-timeout',
      String(CURL_CONNECT_TIMEOUT_SECONDS),
    ];

    if (authHeader) {
      curlArgs.push('-H', `Authorization: ${authHeader}`);
      console.log('Wallet API request with Authorization header');
    } else {
      console.warn('Wallet API request without Authorization header - this will likely fail');
    }

    // Add body for POST/PUT requests
    if (method === 'POST' || method === 'PUT') {
      const body = await request.text();
      if (body) {
        curlArgs.push('-d', body);
      }
    }

    let stdout: string | undefined;
    try {
      const result = await execFileAsync('curl', curlArgs, {
        maxBuffer: 10 * 1024 * 1024, // 10 MB
      });
      stdout = result.stdout?.toString();
    } catch (error: any) {
      const execError = error as { stdout?: string | Buffer; stderr?: string | Buffer; message?: string };
      const stderr = execError?.stderr?.toString?.();
      if (stderr) {
        console.warn('Wallet proxy curl stderr', stderr);
      }
      const candidate = execError?.stdout?.toString?.();
      if (candidate && candidate.trim().length > 0) {
        stdout = candidate;
      } else {
        console.error('Wallet proxy curl error', execError?.message ?? error);
        return NextResponse.json(
          { 
            success: false, 
            isError: true,
            message: 'Proxy request failed',
            error: execError?.message || 'Curl execution failed'
          },
          { status: 502 }
        );
      }
    }

    if (!stdout || !stdout.trim()) {
      return NextResponse.json(
        { 
          success: false, 
          isError: true,
          message: 'Empty response from API' 
        },
        { status: 502 }
      );
    }

    // Check if response is HTML (bot protection or error page)
    if (stdout.trim().startsWith('<!') || stdout.trim().startsWith('<html')) {
      console.error('API returned HTML instead of JSON:', stdout.substring(0, 200));
      return NextResponse.json(
        { 
          success: false, 
          isError: true,
          message: 'API returned HTML response (possible bot protection or server error)',
          data: null
        },
        { status: 502 }
      );
    }
    
    // Try to parse as JSON, fallback to text
    let jsonData;
    try {
      jsonData = JSON.parse(stdout);
    } catch (e) {
      console.error('Failed to parse JSON response:', stdout.substring(0, 200));
      return NextResponse.json(
        { 
          success: false, 
          isError: true,
          message: 'Invalid JSON response from API',
          data: null
        },
        { status: 502 }
      );
    }

    return NextResponse.json(jsonData, {
      status: 200,
      headers: {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
        'Access-Control-Allow-Headers': 'Content-Type, Authorization',
      },
    });
  } catch (error: any) {
    console.error('Curl proxy error:', error);
    return NextResponse.json(
      { 
        success: false, 
        isError: true,
        message: error.message || 'Curl proxy request failed' 
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
      'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type, Authorization',
    },
  });
}

