import { FormField, FormFieldGroup } from "@/types/form/formField.type";
import { z } from "zod";

export const beneficiaryRightsSchema = z.object({
  // Rights & Restrictions
  occupancyRights: z.boolean(),
  
  visitationRights: z.object({
    threshold: z
      .number()
      .min(0, { message: "Visitation threshold must be at least 0%" })
      .max(100, { message: "Visitation threshold must be no more than 100%" }),
    duration: z
      .number()
      .min(0, { message: "Visitation duration must be at least 0 days" })
      .max(365, { message: "Visitation duration must be no more than 365 days" }),
    restrictions: z.array(z.string()),
  }),
  
  // Governance
  votingRights: z.boolean(),
  decisionMaking: z.boolean(),
  
  // Distributions
  distributionFrequency: z.enum(["Annual", "Quarterly", "Monthly"], {
    message: "Distribution frequency must be Annual, Quarterly, or Monthly"
  }),
  
  distributionMethod: z.enum(["Proportional", "Equal", "Weighted"], {
    message: "Distribution method must be Proportional, Equal, or Weighted"
  }),
  
  distributionTiming: z
    .string()
    .regex(/^\d{2}-\d{2}$/, { message: "Distribution timing must be in MM-DD format" }),
  
  // Additional Rights
  informationRights: z.boolean(),
  auditRights: z.boolean(),
  transferRights: z.boolean(),
  
  // Restrictions
  transferRestrictions: z.array(z.string()),
  
  governanceRestrictions: z.array(z.string()),
});

export type BeneficiaryRightsSchema = z.infer<typeof beneficiaryRightsSchema>;

export const beneficiaryRightsSchemaDefaultValues: BeneficiaryRightsSchema = {
  occupancyRights: false,
  visitationRights: {
    threshold: 30,
    duration: 3,
    restrictions: ["No overnight stays", "Advance notice required", "Trustee supervision required"],
  },
  votingRights: false,
  decisionMaking: false,
  distributionFrequency: "Annual",
  distributionMethod: "Proportional",
  distributionTiming: "01-01",
  informationRights: true,
  auditRights: false,
  transferRights: true,
  transferRestrictions: ["Wyoming notarization required", "Beneficiary verification required"],
  governanceRestrictions: ["No management authority", "No voting rights", "No decision making"],
};

export const beneficiaryRightsSchemaFields: FormFieldGroup[] = [
  {
    title: "Occupancy & Visitation Rights",
    fields: [
      {
        name: "occupancyRights",
        placeholder: "Occupancy Rights",
        type: "checkbox",
        description: "Whether beneficiaries can occupy the property (Wyoming trusts: No)",
      },
      {
        name: "visitationRights.threshold",
        placeholder: "Visitation Threshold (%)",
        type: "number",
        description: "Minimum ownership percentage required for visitation rights",
      },
      {
        name: "visitationRights.duration",
        placeholder: "Visitation Duration (Days)",
        type: "number",
        description: "Maximum number of days per year for visitation",
      },
      {
        name: "visitationRights.restrictions",
        placeholder: "Visitation Restrictions",
        type: "multiselect",
        description: "Additional restrictions on property visitation",
        selectItems: [
          { name: "No overnight stays", value: "No overnight stays" },
          { name: "Advance notice required", value: "Advance notice required" },
          { name: "Trustee supervision required", value: "Trustee supervision required" },
          { name: "Insurance coverage required", value: "Insurance coverage required" },
        ],
      },
    ],
  },
  {
    title: "Governance Rights",
    fields: [
      {
        name: "votingRights",
        placeholder: "Voting Rights",
        type: "checkbox",
        description: "Whether beneficiaries have voting rights (Wyoming trusts: No)",
      },
      {
        name: "decisionMaking",
        placeholder: "Decision Making Rights",
        type: "checkbox",
        description: "Whether beneficiaries can make management decisions (Wyoming trusts: No)",
      },
      {
        name: "governanceRestrictions",
        placeholder: "Governance Restrictions",
        type: "multiselect",
        description: "Additional restrictions on beneficiary governance",
        selectItems: [
          { name: "No management authority", value: "No management authority" },
          { name: "No voting rights", value: "No voting rights" },
          { name: "No decision making", value: "No decision making" },
          { name: "Trustee approval required", value: "Trustee approval required" },
        ],
      },
    ],
  },
  {
    title: "Distribution Rights",
    fields: [
      {
        name: "distributionFrequency",
        placeholder: "Distribution Frequency",
        type: "select",
        description: "How often distributions are made to beneficiaries",
        selectItems: [
          { name: "Annual", value: "Annual" },
          { name: "Quarterly", value: "Quarterly" },
          { name: "Monthly", value: "Monthly" },
        ],
      },
      {
        name: "distributionMethod",
        placeholder: "Distribution Method",
        type: "select",
        description: "How distributions are calculated",
        selectItems: [
          { name: "Proportional", value: "Proportional" },
          { name: "Equal", value: "Equal" },
          { name: "Weighted", value: "Weighted" },
        ],
      },
      {
        name: "distributionTiming",
        placeholder: "Distribution Timing (MM-DD)",
        type: "text",
        description: "Date when distributions are made (e.g., 01-01 for January 1st)",
      },
    ],
  },
  {
    title: "Additional Rights",
    fields: [
      {
        name: "informationRights",
        placeholder: "Information Rights",
        type: "checkbox",
        description: "Whether beneficiaries can access trust information",
      },
      {
        name: "auditRights",
        placeholder: "Audit Rights",
        type: "checkbox",
        description: "Whether beneficiaries can request audits",
      },
      {
        name: "transferRights",
        placeholder: "Transfer Rights",
        type: "checkbox",
        description: "Whether beneficiaries can transfer their tokens",
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
];
