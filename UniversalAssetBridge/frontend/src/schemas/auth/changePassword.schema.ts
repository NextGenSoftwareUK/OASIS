import { FormField } from "@/types/form/formField.type";
import { z } from "zod";

export const changePasswordSchema = z
  .object({
    oldPassword: z.string().min(8, {
      message: "Password must be at least 8 characters long",
    }),
    newPassword: z.string().min(8, {
      message: "Password must be at least 8 characters long",
    }),
    confirmPassword: z.string(),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

export type ChangePasswordSchema = z.infer<typeof changePasswordSchema>;

export const changePasswordSchemaDefaultValues: ChangePasswordSchema = {
  oldPassword: "",
  newPassword: "",
  confirmPassword: "",
};

export const changePasswordSchemaFileds: FormField[] = [
  {
    name: "oldPassword",
    placeholder: "Old Password",
    type: "password",
  },
  {
    name: "newPassword",
    placeholder: "New Password",
    type: "password",
  },
  {
    name: "confirmPassword",
    placeholder: "Confirm Password",
    type: "password",
  },
];
