import axiosInstance from "@/lib/axiosInstance";
import { useMutation } from "@tanstack/react-query";

type BuyRwa = {
  rwaId: string;
  buyerPubKey: string;
};

const buyRwa = async (req: BuyRwa) => {
  const res = await axiosInstance.post("/nft-purchase", req);
  return res.data;
};

export const useBuyRwa = () => {
  return useMutation({
    mutationFn: (req: BuyRwa) => buyRwa(req),
  });
};
