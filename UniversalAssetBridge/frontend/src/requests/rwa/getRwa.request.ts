import axiosInstance from "@/lib/axiosInstance";
import { useQueries, useQuery } from "@tanstack/react-query";

const getRwa = async (tokenId: string) => {
  const res = await axiosInstance.get(`/rwa/${tokenId}`);
  return res.data;
};

export const useGetRwa = (tokenId: string) => {
  return useQuery({
    queryKey: ["rwa", tokenId],
    queryFn: () => getRwa(tokenId),
    gcTime: 0,
  });
};

export const useGetRwaMultiple = (tokenIds: string[]) => {
  return useQueries({
    queries: tokenIds.map((id) => ({
      queryKey: ["rwa", "multiple", id],
      queryFn: () => getRwa(id),
      gcTime: 0,
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
