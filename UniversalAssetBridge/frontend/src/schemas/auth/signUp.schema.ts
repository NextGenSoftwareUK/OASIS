import { FormField } from "@/types/form/formField.type";
import { z } from "zod";

export const signUpSchema = z
  .object({
    userName: z.string().min(4, {
      message: "Username must be at least 4 characters long",
    }),
    emailAddress: z.string().email({
      message: "Invalid email address",
    }),
    password: z.string().min(8, {
      message: "Password must be at least 8 characters long",
    }),
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

export type SignUpSchema = z.infer<typeof signUpSchema>;

export const signUpSchemaDefaultValues: SignUpSchema = {
  userName: "",
  emailAddress: "",
  password: "",
  confirmPassword: "",
};

export const signUpSchemaFields: FormField[] = [
  { name: "userName", type: "text", placeholder: "Username" },
  { name: "emailAddress", type: "email", placeholder: "Email Address" },
  { name: "password", type: "password", placeholder: "Password" },
  {
    name: "confirmPassword",
    type: "password",
    placeholder: "Confirm Password",
  },
];
