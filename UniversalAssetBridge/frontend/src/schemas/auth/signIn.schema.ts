import { FormField } from "@/types/form/formField.type";
import { z } from "zod";

export const signInSchema = z.object({
  email: z.string().email({
    message: "Invalid email address",
  }),
  password: z.string().min(8, {
    message: "Password must be at least 8 characters long",
  }),
});

export type SignInSchema = z.infer<typeof signInSchema>;

export const signInSchemaDefaultValues: SignInSchema = {
  email: "",
  password: "",
};

export const signInSchemaFields: FormField[] = [
  {
    name: "email",
    label: "Email",
    placeholder: "example@example.com",
    type: "email",
  },
  {
    name: "password",
    label: "Password",
    placeholder: "Password",
    type: "password",
  },
];
