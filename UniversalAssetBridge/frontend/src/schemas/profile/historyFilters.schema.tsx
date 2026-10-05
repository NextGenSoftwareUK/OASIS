import { SORT_ORDER } from "@/lib/constants";
import { z } from "zod";

export const generalHistoryFiltersSchema = z.object({
  period: z
    .object({
      from: z.string().min(1),
      to: z.string().min(1),
    })
    .optional(),
  sortOrder: z
    .enum(SORT_ORDER.map((item) => item.value) as [string, ...string[]])
    .optional(),
});

export const rwaTypeHistroyFiltersSchema = z.object({
  type: z.array(z.string()).min(1),
});

export type GeneralHistoryFiltersSchemaType = z.infer<
  typeof generalHistoryFiltersSchema
>;

export type RwaTypeHistoryFiltersSchemaType = z.infer<
  typeof rwaTypeHistroyFiltersSchema
>;

export const generalHistoryFiltersSchemaDefaultValues: GeneralHistoryFiltersSchemaType =
  {
    period: {
      from: "",
      to: "",
    },
    sortOrder: "",
  };

export const rwaTypeHistoryFiltersSchemaDefaultValues: RwaTypeHistoryFiltersSchemaType =
  {
    type: ["Buy", "Sell", "Create"],
  };
