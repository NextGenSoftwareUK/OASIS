import axiosInstance from "@/lib/axiosInstance";
import { useQuery } from "@tanstack/react-query";

const getRwaPurchaseHistory = async (tokenId: string) => {
  const res = await axiosInstance.get(
    `/nft-purchase-ownership-histories/${tokenId}`
  );
  return res.data;
};

export const useGetRwaPurchaseHistory = (tokenId: string) => {
  return useQuery({
    queryKey: ["rwa-purchase-history", tokenId],
    queryFn: () => getRwaPurchaseHistory(tokenId),
  });
};
