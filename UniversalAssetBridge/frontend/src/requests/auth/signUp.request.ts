import axiosInstance from "@/lib/axiosInstance";
import { SignUpSchema } from "@/schemas/auth/signUp.schema";
import { useMutation } from "@tanstack/react-query";

const signUp = async (req: SignUpSchema) => {
  const res = await axiosInstance.post("/auth/register", req);
  return res.data;
};

export const useSignUp = () => {
  return useMutation({
    mutationFn: (req: SignUpSchema) => signUp(req),
  });
};
