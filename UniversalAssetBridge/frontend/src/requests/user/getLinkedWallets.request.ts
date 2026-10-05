import axiosInstance from "@/lib/axiosInstance";
import { useQuery } from "@tanstack/react-query";

const getLinkedWallets = async () => {
  const res = await axiosInstance.get("/linked-accounts/me");
  return res.data;
};

export const useGetLinkedWallets = (token: string) => {
  return useQuery({
    queryKey: [token, "linked-wallets"],
    queryFn: () => getLinkedWallets(),
  });
};
