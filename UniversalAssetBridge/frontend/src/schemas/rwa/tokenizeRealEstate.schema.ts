import {
  INSURANSE_STATUSES,
  MIN_NUMBER,
  PROPERTY_TYPES,
} from "@/lib/constants";
import { FormField, FormFieldGroup } from "@/types/form/formField.type";
import { z } from "zod";

export const tokenizeRealEstateSchema = z.object({
  geolocation: z.object({
    latitude: z
      .number({
        required_error: "Latitude is requred",
        invalid_type_error: "Latitude must be a number",
      })
      .min(-90, { message: "Latitude must be between -90 and 90" })
      .max(90, { message: "Latitude must be between -90 and 90" }),
    longitude: z
      .number({
        required_error: "Longitude is requred",
        invalid_type_error: "Longitude must be a number",
      })
      .min(-180, { message: "Longitude must be between -180 and 180" })
      .max(180, { message: "Longitude must be between -180 and 180" }),
  }),
  valuationDate: z
    .string({ required_error: "Valuation Date is required" })
    .min(1, { message: "Valuation Date is required" })
    .regex(/^\d{4}-\d{2}-\d{2}$/, {
      message: "Valuation Date must be in YYYY-MM-DD format",
    }),
  area: z.coerce
    .number({ required_error: "Area is required" })
    .min(MIN_NUMBER, { message: "Area must be greater than 0" }),
  propertyType: z.enum(
    PROPERTY_TYPES.map((item) => item.value) as [string, ...string[]],
    {
      message: "Please select a valid property type",
    }
  ),
  constructionYear: z.coerce
    .number({ required_error: "Construction Year is required" })
    .min(1, { message: "Construction Year is required" })
    .max(9999, { message: "Construction Year must be a 4-digit year" }),
  insuranceStatus: z.enum(
    INSURANSE_STATUSES.map((item) => item.value) as [string, ...string[]],
    {
      message: "Please select a valid insurance status",
    }
  ),
});

export type TokenizeRealEstateSchema = z.infer<typeof tokenizeRealEstateSchema>;

export const tokenizeRealEstateSchemaDefaultValues: TokenizeRealEstateSchema = {
  geolocation: {
    latitude: "" as unknown as number,
    longitude: "" as unknown as number,
  },
  valuationDate: "",
  area: "" as unknown as number,
  propertyType: "",
  constructionYear: "" as unknown as number,
  insuranceStatus: "",
};

export const tokenizeRealEstateSchemaFields: FormFieldGroup[] = [
  {
    title: "",
    fields: [
      {
        name: "geolocation",
        placeholder: "Asset Location (Geolocation)",
        type: "map",
      },
      {
        name: "valuationDate",
        placeholder: "Valuation Date",
        type: "date",
      },
      {
        name: "area",
        placeholder: "Area (in square meters)",
        type: "number",
      },
      {
        name: "propertyType",
        placeholder: "Property Type",
        type: "select",
        selectItems: PROPERTY_TYPES,
      },
      {
        name: "constructionYear",
        placeholder: "Construction Year",
        type: "number",
      },
      {
        name: "insuranceStatus",
        placeholder: "Insurance Status",
        type: "select",
        selectItems: INSURANSE_STATUSES,
      },
    ],
  },
];
