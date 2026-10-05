import axiosInstance from "@/lib/axiosInstance";
import { useQuery } from "@tanstack/react-query";

const getTransactionStatus = async (transactionId: string) => {
  const res = await axiosInstance.get("/transaction-status", {
    params: {
      transactionId: transactionId,
    },
  });
  return res.data;
};

export const useGetTransactionStatus = (transactionId: string) => {
  return useQuery({
    queryKey: [transactionId, "transaction-id"],
    queryFn: () => getTransactionStatus(transactionId),
    enabled: false,
  });
};
