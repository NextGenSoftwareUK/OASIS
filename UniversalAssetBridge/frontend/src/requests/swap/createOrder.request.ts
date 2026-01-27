import axiosInstance from "@/lib/axiosInstance";
import { SwapFormData } from "@/types/crypto/swap.type";
import { useMutation } from "@tanstack/react-query";

const createOrder = async (req: SwapFormData) => {
  const res = await axiosInstance.post("/orders", req);
  return res.data;
};

export const useCreateOrder = () => {
  return useMutation({
    mutationFn: (req: SwapFormData) => createOrder(req),
  });
};
