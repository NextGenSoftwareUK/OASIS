import { FormField, FormFieldGroup } from "@/types/form/formField.type";
import { z } from "zod";

export const trustSetupSchema = z.object({
  // Trust Details
  trustName: z
    .string()
    .min(1, { message: "Trust name is required" })
    .max(100, { message: "Trust name must be less than 100 characters" })
    .regex(/^[A-Z0-9-]+$/, { message: "Trust name must contain only uppercase letters, numbers, and hyphens" }),
  
  settlorName: z
    .string()
    .min(1, { message: "Settlor name is required" })
    .max(255, { message: "Settlor name must be less than 255 characters" }),
  
  trusteeName: z
    .string()
    .min(1, { message: "Trustee name is required" })
    .max(255, { message: "Trustee name must be less than 255 characters" }),
  
  trustProtector: z
    .string()
    .max(255, { message: "Trust protector name must be less than 255 characters" })
    .optional(),
  
  qualifiedCustodian: z
    .string()
    .min(1, { message: "Qualified custodian is required" })
    .max(255, { message: "Qualified custodian must be less than 255 characters" }),
  
  // Legal Framework
  jurisdiction: z.literal("Wyoming", { message: "Jurisdiction must be Wyoming" }),
  trustDuration: z.literal(1000, { message: "Trust duration must be 1000 years" }),
  trustType: z.literal("Statutory Trust", { message: "Trust type must be Statutory Trust" }),
  
  // Financial Terms
  minimumPropertyValue: z.literal(25000000, { message: "Minimum property value must be $25,000,000" }),
  annualDistributionRate: z
    .number()
    .min(0, { message: "Annual distribution rate must be at least 0%" })
    .max(100, { message: "Annual distribution rate must be no more than 100%" }),
  
  reserveFundRate: z
    .number()
    .min(0, { message: "Reserve fund rate must be at least 0%" })
    .max(100, { message: "Reserve fund rate must be no more than 100%" }),
  
  trusteeFeeRate: z
    .number()
    .min(0, { message: "Trustee fee rate must be at least 0%" })
    .max(10, { message: "Trustee fee rate must be no more than 10%" }),
});

export type TrustSetupSchema = z.infer<typeof trustSetupSchema>;

export const trustSetupSchemaDefaultValues: TrustSetupSchema = {
  trustName: "",
  settlorName: "Quantum Street, Inc.",
  trusteeName: "",
  trustProtector: "",
  qualifiedCustodian: "",
  jurisdiction: "Wyoming",
  trustDuration: 1000,
  trustType: "Statutory Trust",
  minimumPropertyValue: 25000000,
  annualDistributionRate: 90,
  reserveFundRate: 10,
  trusteeFeeRate: 1,
};

export const trustSetupSchemaFields: FormFieldGroup[] = [
  {
    title: "Trust Information",
    fields: [
      {
        name: "trustName",
        placeholder: "Trust Name (e.g., SFR-AAA-TRUST-FH001)",
        type: "text",
        description: "Auto-generated based on property details. Use uppercase letters, numbers, and hyphens only.",
      },
      {
        name: "settlorName",
        placeholder: "Settlor Name",
        type: "text",
        description: "The entity creating the trust (e.g., Quantum Street, Inc.)",
      },
      {
        name: "trusteeName",
        placeholder: "Trustee Name",
        type: "text",
        description: "The entity managing the trust assets",
      },
      {
        name: "trustProtector",
        placeholder: "Trust Protector (Optional)",
        type: "text",
        description: "Entity overseeing technological evolution and compliance",
      },
      {
        name: "qualifiedCustodian",
        placeholder: "Qualified Custodian",
        type: "text",
        description: "Wyoming-regulated entity for digital asset custody",
      },
    ],
  },
  {
    title: "Legal Framework",
    fields: [
      {
        name: "jurisdiction",
        placeholder: "Jurisdiction",
        type: "text",
        disabled: true,
        description: "Fixed to Wyoming for statutory trusts",
      },
      {
        name: "trustDuration",
        placeholder: "Trust Duration (Years)",
        type: "number",
        disabled: true,
        description: "Fixed to 1000 years (Wyoming perpetuity period)",
      },
      {
        name: "trustType",
        placeholder: "Trust Type",
        type: "text",
        disabled: true,
        description: "Fixed to Statutory Trust",
      },
    ],
  },
  {
    title: "Financial Terms",
    fields: [
      {
        name: "minimumPropertyValue",
        placeholder: "Minimum Property Value ($)",
        type: "number",
        disabled: true,
        description: "Fixed to $25,000,000 minimum",
      },
      {
        name: "annualDistributionRate",
        placeholder: "Annual Distribution Rate (%)",
        type: "number",
        description: "Percentage of net profits distributed to beneficiaries annually",
      },
      {
        name: "reserveFundRate",
        placeholder: "Reserve Fund Rate (%)",
        type: "number",
        description: "Percentage of net profits allocated to reserve fund",
      },
      {
        name: "trusteeFeeRate",
        placeholder: "Trustee Fee Rate (%)",
        type: "number",
        description: "Annual fee rate based on property value",
      },
    ],
  },
];
