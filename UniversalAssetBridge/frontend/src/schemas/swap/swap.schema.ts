import { MIN_NUMBER } from "@/lib/constants";
import { FormField } from "@/types/form/formField.type";
import { z } from "zod";

export const swapSchema = z.object({
  fromAmount: z.coerce
    .number()
    .min(MIN_NUMBER, { message: "Amount is required" }),
  toAmount: z.coerce
    .number()
    .min(MIN_NUMBER, { message: "Amount is required" }),
  destinationAddress: z
    .string()
    .min(1, { message: "Recipient Address is required" }),
});

export type SwapSchema = z.infer<typeof swapSchema>;

export const swapSchemaDefaultValues: SwapSchema = {
  fromAmount: 1,
  toAmount: "" as unknown as number,
  destinationAddress: "",
};

export const swapSchemaFields: FormField[] = [
  {
    name: "fromAmount",
    placeholder: "From",
    type: "number",
  },
  {
    name: "toAmount",
    placeholder: "To",
    type: "number",
  },
  {
    name: "destinationAddress",
    placeholder: "Recipient Address",
    type: "text",
  },
];
