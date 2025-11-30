import axiosInstance from "@/lib/axiosInstance";
import { SignInSchema } from "@/schemas/auth/signIn.schema";
import { useMutation } from "@tanstack/react-query";

const signIn = async (req: SignInSchema) => {
  const res = await axiosInstance.post("/auth/login", req);
  return res.data;
};

export const useSignIn = () => {
  return useMutation({
    mutationFn: (req: SignInSchema) => signIn(req),
  });
};
