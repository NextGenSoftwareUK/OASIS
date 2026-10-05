import axiosInstance from "@/lib/axiosInstance";
import { useMutation } from "@tanstack/react-query";

type SendSignedTransactionReq = {
  transactionHash: string;
  transactionSignature: string;
};

const sendSignedTransaction = async (req: SendSignedTransactionReq) => {
  const res = await axiosInstance.post("/nft-purchase/send", req);
  return res.data;
};

export const useSendSignedTransaction = () => {
  return useMutation({
    mutationFn: (req: SendSignedTransactionReq) => sendSignedTransaction(req),
  });
};
