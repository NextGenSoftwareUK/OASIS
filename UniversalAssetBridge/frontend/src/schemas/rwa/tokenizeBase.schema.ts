import { ASSET_TYPES, MIN_NUMBER, NETWORKS } from "@/lib/constants";
import { FormField, FormFieldGroup } from "@/types/form/formField.type";
import { z } from "zod";

export const tokenizeBaseSchema = z.object({
  image: z.string().url({ message: "Please provide a valid image" }),
  title: z.string().min(1, { message: "Title is required" }).max(32, {
    message: "Title must be less than 32 characters",
  }),
  assetDescription: z
    .string()
    .min(1, { message: "Asset Description is required" })
    .max(1000, { message: "Description must be less than 1000 characters" }),
  proofOfOwnershipDocument: z.string().url({
    message: "Please provide a valid image of the ownership document",
  }),
  uniqueIdentifier: z
    .string()
    .min(1, { message: "Unique identifier is required" })
    .max(10, {
      message: "Identifier must be less than 10 characters",
    }),
  network: z.enum(NETWORKS.map((item) => item.value) as [string, ...string[]], {
    message: "Network is required",
  }),
  royalty: z.coerce
    .number({ invalid_type_error: "Royalty must be a number" })
    .min(MIN_NUMBER, { message: "Royalty must be more than 0%" })
    .max(100, { message: "Royalty must be no more than 100%" }),
  price: z.coerce
    .number({ invalid_type_error: "Price must be a number" })
    .min(0.0001, { message: "The price must be greater than 0.0001" }),
  ownerContact: z.string().min(1, { message: "Owner contact is required" }),
  assetType: z.enum(
    ASSET_TYPES.map((item) => item.value) as [string, ...string[]],
    { message: "Asset type is required" }
  ),
});

export type TokenizeBaseSchema = z.infer<typeof tokenizeBaseSchema>;

export const tokenizeBaseSchemaDefaultValues: TokenizeBaseSchema = {
  image: "",
  title: "",
  assetDescription: "",
  proofOfOwnershipDocument: "",
  uniqueIdentifier: "",
  network: "",
  royalty: "" as unknown as number,
  price: "" as unknown as number,
  ownerContact: "",
  assetType: "",
};

export const tokenizeBaseSchemaFields: FormFieldGroup[] = [
  {
    title: "General information",
    fields: [
      {
        name: "title",
        placeholder: "Title",
        type: "text",
        description: "This title will be the main name of your RWA",
      },
      {
        name: "assetDescription",
        placeholder: "Description",
        type: "text",
      },
      {
        name: "uniqueIdentifier",
        placeholder: "Unique identifier",
        type: "text",
      },
      {
        name: "assetType",
        placeholder: "Asset Type",
        type: "select",
        selectItems: ASSET_TYPES,
      },
      {
        name: "proofOfOwnershipDocument",
        placeholder: "Proof of ownership document",
        type: "file",
      },
    ],
  },
  {
    title: "Network and Price",
    fields: [
      {
        name: "network",
        placeholder: "Network",
        type: "select",
        selectItems: NETWORKS,
      },
      {
        name: "price",
        placeholder: "Price",
        type: "number",
        description: "Min price: 0,0001 zBTC",
      },
      {
        name: "royalty",
        placeholder: "Royalty",
        type: "number",
        description:
          "You earn this from every resale — even after you sell the asset",
      },
      {
        name: "netAmount",
        placeholder: "Net amount",
        disabled: true,
        type: "number",
        description: "Calculated royalty in zBTC",
      },
    ],
  },
  {
    title: "Contacts",
    fields: [
      {
        name: "ownerContact",
        placeholder: "Owner contact",
        type: "text",
        description: "Users will be able to contact you if they need",
      },
    ],
  },
];
