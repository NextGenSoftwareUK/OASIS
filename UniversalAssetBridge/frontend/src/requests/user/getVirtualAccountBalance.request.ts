import axiosInstance from "@/lib/axiosInstance";
import { useQuery } from "@tanstack/react-query";

const getVirtualAccountBalance = async (orderId: string) => {
  const res = await axiosInstance.get(`/orders/${orderId}/check-balance`);
  return res.data;
};

export const useGetVirtualAccountBalance = (
  orderId: string,
  completed: string | boolean
) => {
  return useQuery({
    queryKey: [orderId, "virtual-account-balance"],
    queryFn: () => getVirtualAccountBalance(orderId),
    gcTime: 0,
    enabled: !!orderId && !!completed,
  });
};
