import { FormField, FormFieldGroup } from "@/types/form/formField.type";
import { z } from "zod";

export const tokenConfigurationSchema = z.object({
  // Token Details
  tokenName: z
    .string()
    .min(1, { message: "Token name is required" })
    .max(50, { message: "Token name must be less than 50 characters" })
    .regex(/^[A-Z0-9-]+$/, { message: "Token name must contain only uppercase letters, numbers, and hyphens" }),
  
  tokenSymbol: z
    .string()
    .min(1, { message: "Token symbol is required" })
    .max(10, { message: "Token symbol must be less than 10 characters" })
    .regex(/^[A-Z0-9]+$/, { message: "Token symbol must contain only uppercase letters and numbers" }),
  
  tokenStandard: z.literal("ERC721", { message: "Token standard must be ERC721" }),
  
  // Supply & Pricing
  totalSupply: z
    .number()
    .min(1, { message: "Total supply must be at least 1" })
    .max(100000, { message: "Total supply must be less than 100,000" }),
  
  tokenPrice: z
    .number()
    .min(0.01, { message: "Token price must be at least $0.01" })
    .max(1000000, { message: "Token price must be less than $1,000,000" }),
  
  minimumPurchase: z
    .number()
    .min(1, { message: "Minimum purchase must be at least 1 token" })
    .max(1000, { message: "Minimum purchase must be less than 1,000 tokens" }),
  
  // Transfer Rules
  requiresNotarization: z.boolean(),
  transferRestrictions: z
    .array(z.string())
    .min(1, { message: "At least one transfer restriction is required" }),
  
  // Metadata
  tokenMetadata: z.object({
    name: z.string().min(1, { message: "Metadata name is required" }),
    description: z.string().min(1, { message: "Metadata description is required" }),
    image: z.string().url({ message: "Please provide a valid image URL" }),
    attributes: z.array(z.object({
      trait_type: z.string(),
      value: z.union([z.string(), z.number()]),
    })).min(1, { message: "At least one attribute is required" }),
  }),
  
  // Blockchain Configuration
  blockchain: z.literal("Kadena", { message: "Blockchain must be Kadena" }),
  contractVersion: z.string(),
});

export type TokenConfigurationSchema = z.infer<typeof tokenConfigurationSchema>;

export const tokenConfigurationSchemaDefaultValues: TokenConfigurationSchema = {
  tokenName: "",
  tokenSymbol: "",
  tokenStandard: "ERC721",
  totalSupply: 0,
  tokenPrice: 0,
  minimumPurchase: 1,
  requiresNotarization: true,
  transferRestrictions: ["Wyoming notarization required", "Beneficiary verification required"],
  tokenMetadata: {
    name: "",
    description: "",
    image: "",
    attributes: [],
  },
  blockchain: "Kadena",
  contractVersion: "1.0.0",
};

export const tokenConfigurationSchemaFields: FormFieldGroup[] = [
  {
    title: "Token Details",
    fields: [
      {
        name: "tokenName",
        placeholder: "Token Name (e.g., FH001-SFR-RPT-NFT)",
        type: "text",
        description: "Unique identifier for the token (uppercase letters, numbers, hyphens only)",
      },
      {
        name: "tokenSymbol",
        placeholder: "Token Symbol (e.g., PROP)",
        type: "text",
        description: "Short symbol for the token (uppercase letters and numbers only)",
      },
      {
        name: "tokenStandard",
        placeholder: "Token Standard",
        type: "text",
        disabled: true,
        description: "Fixed to ERC721 for non-fungible tokens",
      },
    ],
  },
  {
    title: "Supply & Pricing",
    fields: [
      {
        name: "totalSupply",
        placeholder: "Total Supply",
        type: "number",
        description: "Total number of tokens (equals total square footage)",
      },
      {
        name: "tokenPrice",
        placeholder: "Token Price ($)",
        type: "number",
        description: "Price per token in USD",
      },
      {
        name: "minimumPurchase",
        placeholder: "Minimum Purchase (Tokens)",
        type: "number",
        description: "Minimum number of tokens required per purchase",
      },
    ],
  },
  {
    title: "Transfer Rules",
    fields: [
      {
        name: "requiresNotarization",
        placeholder: "Requires Notarization",
        type: "checkbox",
        description: "Wyoming law requires notarization for token transfers",
      },
      {
        name: "transferRestrictions",
        placeholder: "Transfer Restrictions",
        type: "multiselect",
        description: "Additional restrictions on token transfers",
        selectItems: [
          { name: "Wyoming notarization required", value: "Wyoming notarization required" },
          { name: "Beneficiary verification required", value: "Beneficiary verification required" },
          { name: "KYC/AML compliance required", value: "KYC/AML compliance required" },
          { name: "Trustee approval required", value: "Trustee approval required" },
        ],
      },
    ],
  },
  {
    title: "Token Metadata",
    fields: [
      {
        name: "tokenMetadata.name",
        placeholder: "Metadata Name",
        type: "text",
        description: "Display name for the token",
      },
      {
        name: "tokenMetadata.description",
        placeholder: "Metadata Description",
        type: "textarea",
        description: "Detailed description of the tokenized property",
      },
      {
        name: "tokenMetadata.image",
        placeholder: "Metadata Image",
        type: "file",
        description: "Image representing the tokenized property",
      },
    ],
  },
  {
    title: "Blockchain Configuration",
    fields: [
      {
        name: "blockchain",
        placeholder: "Target Blockchain",
        type: "text",
        disabled: true,
        description: "Fixed to Kadena for Wyoming trusts",
      },
      {
        name: "contractVersion",
        placeholder: "Contract Version",
        type: "text",
        disabled: true,
        description: "Smart contract version",
      },
    ],
  },
];
