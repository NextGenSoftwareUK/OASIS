import axiosInstance from "@/lib/axiosInstance";
import { useMutation } from "@tanstack/react-query";

const logout = async (req: any) => {
  const res = await axiosInstance.post("/logout", req);
  return res.data;
};

export const useLogout = () => {
  return useMutation({
    mutationFn: (req: any) => logout(req),
  });
};
