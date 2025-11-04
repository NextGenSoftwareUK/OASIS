import { SelectItems } from "@/types/form/formField.type";

export const API = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5233/api/v1";
export const SOLANA_ENVIRONMENT = process.env.NEXT_PUBLIC_SOLANA_ENVIRONMENT;

export const SOL_EXPLORER_URL = "https://explorer.solana.com";

export const MAX_FILE_SIZE = 100 * 1024 * 1024;
export const MIN_NUMBER = 0.0000000001;

export const ACCEPTED_DOCUMENT_TYPES = [
  "image/jpeg",
  "image/jpg",
  "image/png",
  "image/webp",
];
export const ASSET_TYPES: SelectItems[] = [
  {
    name: "Real Estate",
    value: "RealEstate",
  },
  // "Automobiles",
  // "Industrial Equipment",
  // "Jewelry and Precious Metals",
  // "Collectibles",
  // "Other",
] as const;
export const NETWORKS: SelectItems[] = [
  {
    name: "Solana",
    value: "Solana",
  },
] as const;
export const SORT_BY: SelectItems[] = [
  {
    name: "Price",
    value: "Price",
  },
  {
    name: "Date of Creation",
    value: "CreatedAt",
  },
] as const;
export const SORT_ORDER: SelectItems[] = [
  {
    name: "Asc",
    value: "Asc",
  },
  {
    name: "Desc",
    value: "Desc",
  },
] as const;
export const PROPERTY_TYPES: SelectItems[] = [
  {
    name: "Residential",
    value: "Residential",
  },
  {
    name: "Commercial",
    value: "Commercial",
  },
  {
    name: "Industrial",
    value: "Industrial",
  },
  {
    name: "Agricultural",
    value: "Agricultural",
  },
  {
    name: "MixedUse",
    value: "MixedUse",
  },
  {
    name: "Other",
    value: "Other",
  },
] as const;
export const INSURANSE_STATUSES: SelectItems[] = [
  {
    name: "Active",
    value: "Active",
  },
  {
    name: "Expired",
    value: "Expired",
  },
  {
    name: "Pending",
    value: "Pending",
  },
  {
    name: "Cancelled",
    value: "Cancelled",
  },
] as const;
