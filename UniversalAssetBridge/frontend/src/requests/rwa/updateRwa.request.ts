import axiosInstance from "@/lib/axiosInstance";
import { useMutation } from "@tanstack/react-query";

const updateRwa = async (tokenId: string, req: any) => {
  const res = await axiosInstance.put(`/rwa/${tokenId}`, req);
  return res.data;
};

export const useUpdateRwa = (tokenId: string) => {
  return useMutation({
    mutationFn: (req: any) => updateRwa(tokenId, req),
  });
};
