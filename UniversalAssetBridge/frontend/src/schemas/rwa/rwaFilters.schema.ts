import { ASSET_TYPES, SORT_BY, SORT_ORDER } from "@/lib/constants";
import { FormField } from "@/types/form/formField.type";
import { z } from "zod";

export const filtersSchema = z.object({
  priceMin: z.coerce.number().optional().nullable(),
  priceMax: z.coerce.number().optional().nullable(),
  assetType: z
    .enum([
      ...(ASSET_TYPES.map((item) => item.value) as [string, ...string[]]),
      "",
    ])
    .optional(),
  sortBy: z
    .enum([...(SORT_BY.map((item) => item.value) as [string, ...string[]]), ""])
    .optional(),
  sortOrder: z
    .enum([
      ...(SORT_ORDER.map((item) => item.value) as [string, ...string[]]),
      "",
    ])
    .optional(),
});

export type FiltersSchema = z.infer<typeof filtersSchema>;

export const filtersSchemaDefaultValues: FiltersSchema = {
  assetType: "",
  priceMin: null,
  priceMax: null,
  sortBy: "",
  sortOrder: "",
};

export const filtersSchemaFields: FormField[] = [
  {
    name: "priceMin",
    placeholder: "min",
    type: "number",
  },
  {
    name: "priceMax",
    placeholder: "max",
    type: "number",
  },
  {
    name: "assetType",
    placeholder: "Asset type",
    type: "text",
    selectItems: ASSET_TYPES,
  },
  {
    name: "sortBy",
    placeholder: "Sort by",
    type: "text",
    selectItems: SORT_BY,
  },
  {
    name: "sortOrder",
    placeholder: "Sort order",
    type: "text",
    selectItems: SORT_ORDER,
  },
];
