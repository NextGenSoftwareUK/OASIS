import axiosInstance from "@/lib/axiosInstance";
import { useMutation } from "@tanstack/react-query";
import { LinkWallet } from "@/types/crypto/wallet.type";

const linkWallet = async (req: LinkWallet) => {
  const res = await axiosInstance.post("/linked-accounts", req);
  return res.data;
};

export const useLinkWallet = () => {
  return useMutation({
    mutationFn: (req: LinkWallet) => linkWallet(req),
  });
};
