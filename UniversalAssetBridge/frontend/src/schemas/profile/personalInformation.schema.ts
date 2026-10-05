import { z } from "zod";

export const personalInformationSchema = z.object({
  fullName: z.string().min(1, { message: "Full name is required" }),
  userName: z.string().min(1, { message: "Username is required" }),
  email: z.string().email({
    message: "Invalid email address",
  }),
  aboutYou: z.string().min(1, { message: "About you is required" }),
  websiteUrl: z.string().url({ message: "Please provide a valid URL" }),
});

export type PersonalInformationSchema = z.infer<
  typeof personalInformationSchema
>;

export const personalInformationSchemaDefaultValues: PersonalInformationSchema =
  {
    fullName: "",
    userName: "",
    email: "",
    aboutYou: "",
    websiteUrl: "",
  };
