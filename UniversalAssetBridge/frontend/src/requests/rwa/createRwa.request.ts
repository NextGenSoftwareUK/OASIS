import axiosInstance from "@/lib/axiosInstance";
import { useMutation } from "@tanstack/react-query";

const createRwa = async (req: any) => {
  const res = await axiosInstance.post("/rwa/tokenize", req);
  return res.data;
};

export const useCreateRwa = () => {
  return useMutation({
    mutationFn: (req: any) => createRwa(req),
  });
};
