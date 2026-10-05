import axiosInstance from "@/lib/axiosInstance";
import { useQueries, useQuery } from "@tanstack/react-query";

const getRwaChanges = async (tokenId: string) => {
  const res = await axiosInstance.get(`/rwa-price-histories/${tokenId}`);
  return res.data;
};

export const useGetRwaChanges = (tokenId: string) => {
  return useQuery({
    queryKey: ["rwa-changes", tokenId],
    queryFn: () => getRwaChanges(tokenId),
    gcTime: 0,
  });
};

export const useGetRwaChangesMultiple = (tokenIds: string[]) => {
  return useQueries({
    queries: tokenIds.map((id) => ({
      queryKey: ["rwa-changes", "multiple", id],
      queryFn: () => getRwaChanges(id),
      enabled: !!tokenIds,
    })),
    combine: (results) => {
      return {
        data: results.map((result) => result.data),
        isFetching: results.map((result) => result.isFetching),
      };
    },
  });
};
