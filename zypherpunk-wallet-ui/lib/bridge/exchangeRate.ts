const COINGECKO_API = 'https://api.coingecko.com/api/v3';

const TOKEN_IDS: Record<string, string> = {
  SOL: 'solana',
  ETH: 'ethereum',
  MATIC: 'matic-network',
  ARB: 'arbitrum',
  AVAX: 'avalanche-2',
  BNB: 'binancecoin',
  FTM: 'fantom',
  ADA: 'cardano',
  DOT: 'polkadot',
  BTC: 'bitcoin',
  NEAR: 'near',
  SUI: 'sui',
  APT: 'aptos',
  ATOM: 'cosmos',
  EOS: 'eos',
  TLOS: 'telos',
};

export async function fetchExchangeRate(fromSymbol: string, toSymbol: string): Promise<number> {
  const fromId = TOKEN_IDS[fromSymbol.toUpperCase()];
  const toId = TOKEN_IDS[toSymbol.toUpperCase()];

  if (!fromId || !toId) {
    return 1;
  }

  try {
    const url = `${COINGECKO_API}/simple/price?ids=${fromId},${toId}&vs_currencies=usd`;
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`Failed to fetch prices: ${response.status}`);
    }
    const data = await response.json();
    const fromPrice = data[fromId]?.usd;
    const toPrice = data[toId]?.usd;

    if (!fromPrice || !toPrice) {
      throw new Error('Missing price data');
    }

    return fromPrice / toPrice;
  } catch (error) {
    console.warn('Exchange rate fallback triggered', error);
    return getFallbackRate(fromSymbol, toSymbol);
  }
}

export async function fetchUsdPrice(symbol: string): Promise<number> {
  const id = TOKEN_IDS[symbol.toUpperCase()];
  if (!id) return 1;
  try {
    const response = await fetch(`${COINGECKO_API}/simple/price?ids=${id}&vs_currencies=usd`);
    if (!response.ok) throw new Error('Price fetch failed');
    const data = await response.json();
    return data[id]?.usd || 1;
  } catch {
    return getFallbackUsdPrice(symbol);
  }
}

function getFallbackRate(fromSymbol: string, toSymbol: string): number {
  const key = `${fromSymbol.toUpperCase()}-${toSymbol.toUpperCase()}`;
  const table: Record<string, number> = {
    'SOL-ETH': 0.05,
    'ETH-SOL': 20,
    'SOL-MATIC': 2.5,
    'MATIC-SOL': 0.4,
    'ETH-MATIC': 50,
    'MATIC-ETH': 0.02,
    'SOL-ARB': 0.05,
    'ARB-SOL': 20,
    'SOL-BNB': 0.2,
    'BNB-SOL': 5,
  };
  return table[key] || 1;
}

function getFallbackUsdPrice(symbol: string): number {
  const table: Record<string, number> = {
    SOL: 150,
    ETH: 3200,
    MATIC: 0.8,
    ARB: 1.2,
    AVAX: 35,
    BNB: 600,
    FTM: 0.6,
    ADA: 0.45,
    DOT: 6.5,
    BTC: 65000,
    NEAR: 6,
    SUI: 1.4,
    APT: 9,
    ATOM: 10,
    EOS: 0.8,
    TLOS: 0.2,
  };
  return table[symbol.toUpperCase()] || 1;
}

