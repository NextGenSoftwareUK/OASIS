import axiosInstance from "@/lib/axiosInstance";
import { useQuery } from "@tanstack/react-query";

const getExchangeRate = async (fromToken: string, toToken: string) => {
  const res = await axiosInstance.get("/exchange-rate", {
    params: {
      fromToken: fromToken,
      toToken: toToken,
    },
  });
  return res.data;
};

export const useGetExchangeRate = (fromToken: string, toToken: string) => {
  return useQuery({
    queryKey: [fromToken, toToken, "exchange-rate"],
    queryFn: () => getExchangeRate(fromToken, toToken),
    gcTime: 0,
    // refetchInterval: 300000,
  });
};
