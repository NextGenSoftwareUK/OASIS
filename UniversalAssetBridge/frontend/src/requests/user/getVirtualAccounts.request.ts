import axiosInstance from "@/lib/axiosInstance";
import { useQuery } from "@tanstack/react-query";

const getVirtualAccounts = async () => {
  const res = await axiosInstance.get("/accounts/list");
  return res.data;
};

export const useGetVirtualAccounts = (isEnabled: boolean, token: string) => {
  return useQuery({
    queryKey: [token, "user-accounts"],
    queryFn: () => getVirtualAccounts(),
    gcTime: 0,
    enabled: isEnabled,
  });
};
