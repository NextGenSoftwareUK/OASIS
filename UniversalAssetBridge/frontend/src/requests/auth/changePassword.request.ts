import axiosInstance from "@/lib/axiosInstance";
import { ChangePasswordSchema } from "@/schemas/auth/changePassword.schema";
import { useMutation } from "@tanstack/react-query";

const changePassword = async (req: ChangePasswordSchema) => {
  const res = await axiosInstance.post("/auth/change-password", req);
  return res.data;
};

export const useChangePassword = () => {
  return useMutation({
    mutationFn: (req: ChangePasswordSchema) => changePassword(req),
  });
};
