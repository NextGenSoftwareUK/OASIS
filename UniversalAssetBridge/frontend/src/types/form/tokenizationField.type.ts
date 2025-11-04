import { ZodType } from "zod";

export interface TokenizationField {
  name: string;
  placeholder: string;
  type: string;
  validation: ZodType;
  group?: number;
  defaultValue?:
    | string
    | { latitude: number | string; longitude: number | string };
  HTMLType?: string;
  selectItems?: string[];
}
