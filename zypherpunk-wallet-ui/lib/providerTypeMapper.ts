import { ProviderType } from './types';

/**
 * Maps numeric providerType values from the API to ProviderType enum strings
 * Based on C# ProviderType enum order (0-indexed)
 */
const numericToProviderTypeMap: Record<number, ProviderType> = {
  0: ProviderType.None,
  1: ProviderType.All,
  2: ProviderType.Default,
  3: ProviderType.SolanaOASIS,
  4: ProviderType.RadixOASIS,
  5: ProviderType.ArbitrumOASIS,
  6: ProviderType.AvalancheOASIS,
  7: ProviderType.BaseOASIS,
  8: ProviderType.MonadOASIS,
  9: ProviderType.EthereumOASIS,
  10: ProviderType.PolygonOASIS,
  11: ProviderType.EOSIOOASIS,
  12: ProviderType.TelosOASIS,
  13: ProviderType.SEEDSOASIS,
  14: ProviderType.LoomOASIS,
  15: ProviderType.TONOASIS,
  16: ProviderType.StellarOASIS,
  17: ProviderType.BlockStackOASIS,
  18: ProviderType.HashgraphOASIS,
  19: ProviderType.ElrondOASIS,
  20: ProviderType.TRONOASIS,
  21: ProviderType.CosmosBlockChainOASIS,
  22: ProviderType.RootstockOASIS,
  23: ProviderType.ChainLinkOASIS,
  24: ProviderType.CardanoOASIS,
  25: ProviderType.PolkadotOASIS,
  26: ProviderType.BitcoinOASIS,
  27: ProviderType.NEAROASIS,
  28: ProviderType.SuiOASIS,
  29: ProviderType.StarknetOASIS,
  30: ProviderType.AptosOASIS,
  31: ProviderType.AztecOASIS,
  32: ProviderType.ZcashOASIS,
  33: ProviderType.MidenOASIS,
  34: ProviderType.OptimismOASIS,
  35: ProviderType.BNBChainOASIS,
  36: ProviderType.FantomOASIS,
  37: ProviderType.MoralisOASIS,
  38: ProviderType.IPFSOASIS,
  39: ProviderType.PinataOASIS,
  40: ProviderType.HoloOASIS,
  41: ProviderType.MongoDBOASIS,
  42: ProviderType.Neo4jOASIS,
  43: ProviderType.SQLLiteDBOASIS,
  44: ProviderType.SQLServerDBOASIS,
  45: ProviderType.OracleDBOASIS,
  46: ProviderType.GoogleCloudOASIS,
  47: ProviderType.AzureStorageOASIS,
  48: ProviderType.AzureCosmosDBOASIS,
  49: ProviderType.AWSOASIS,
  50: ProviderType.UrbitOASIS,
  51: ProviderType.ThreeFoldOASIS,
  52: ProviderType.PLANOASIS,
  53: ProviderType.HoloWebOASIS,
  54: ProviderType.SOLIDOASIS,
  55: ProviderType.ActivityPubOASIS,
  56: ProviderType.ScuttlebuttOASIS,
  57: ProviderType.LocalFileOASIS,
};

/**
 * Normalize providerType from API response (handles both numeric and string values)
 */
export function normalizeProviderType(providerType: any): ProviderType {
  if (!providerType) {
    return ProviderType.Default;
  }
  
  // If it's already a valid string enum value, return it
  if (typeof providerType === 'string' && Object.values(ProviderType).includes(providerType as ProviderType)) {
    return providerType as ProviderType;
  }
  
  // If it's a number, map it to the enum string
  if (typeof providerType === 'number') {
    const mapped = numericToProviderTypeMap[providerType];
    if (mapped) {
      return mapped;
    }
    console.warn(`Unknown numeric providerType: ${providerType}, defaulting to Default`);
    return ProviderType.Default;
  }
  
  // Try to convert string number to enum
  if (typeof providerType === 'string' && !isNaN(Number(providerType))) {
    const num = Number(providerType);
    const mapped = numericToProviderTypeMap[num];
    if (mapped) {
      return mapped;
    }
  }
  
  console.warn(`Could not normalize providerType: ${providerType}, defaulting to Default`);
  return ProviderType.Default;
}

