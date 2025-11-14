import { useGetRwa } from "@/requests/rwa/getRwa.request";
import { useGetRwaChanges } from "@/requests/rwa/getRwaChanges.request";

export const useRwaData = (tokenId: string) => {
  const rwa = useGetRwa(tokenId);
  const rwaChanges = useGetRwaChanges(tokenId);

  const isLoading = rwa.isFetching || rwaChanges.isFetching;

  return {
    rwaData: rwa.data?.data,
    rwaChanges: rwaChanges.data?.data,
    isLoading,
  };
};
