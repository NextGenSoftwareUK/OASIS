import { useQuery } from "@tanstack/react-query";
import { getExchangeRate as getExchangeRateFromService } from "@/lib/exchangeRateService";

const getExchangeRate = async (fromToken: string, toToken: string) => {
  // Use client-side CoinGecko API (with fallback to mock rates)
  return await getExchangeRateFromService(fromToken, toToken);
};

export const useGetExchangeRate = (fromToken: string, toToken: string) => {
  return useQuery({
    queryKey: [fromToken, toToken, "exchange-rate"],
    queryFn: () => getExchangeRate(fromToken, toToken),
    gcTime: 0,
    refetchInterval: 30000, // Refresh every 30 seconds
    retry: 3, // Retry failed requests
    staleTime: 10000, // Consider data stale after 10 seconds
  });
};
