import { FormField, FormFieldGroup } from "@/types/form/formField.type";
import { z } from "zod";

export const propertyDetailsSchema = z.object({
  // Basic Information
  propertyAddress: z
    .string()
    .min(1, { message: "Property address is required" })
    .max(500, { message: "Property address must be less than 500 characters" })
    .refine(
      (address) => {
        // More flexible address validation - just check for basic components
        const trimmedAddress = address.trim();
        
        // Must have at least a number (street number) and some text
        const hasNumber = /\d/.test(trimmedAddress);
        const hasText = /[a-zA-Z]/.test(trimmedAddress);
        const hasMinimumLength = trimmedAddress.length >= 10; // Reasonable minimum length
        
        return hasNumber && hasText && hasMinimumLength;
      },
      { 
        message: "Please enter a complete address with street number and street name (e.g., '123 Main Street, City, State 12345')" 
      }
    ),
  
  propertyType: z.literal("Single Family Residential", { 
    message: "Property type must be Single Family Residential" 
  }),
  
  propertyValue: z
    .number()
    .min(25000000, { message: "Property value must be at least $25,000,000" })
    .max(1000000000, { message: "Property value must be less than $1,000,000,000" }),
  
  totalSquareFootage: z
    .number()
    .min(1, { message: "Total square footage must be at least 1" })
    .max(100000, { message: "Total square footage must be less than 100,000" }),
  
  // Financial Information
  annualRentalIncome: z
    .number()
    .min(0, { message: "Annual rental income must be at least 0" })
    .max(10000000, { message: "Annual rental income must be less than $10,000,000" }),
  
  annualExpenses: z
    .number()
    .min(0, { message: "Annual expenses must be at least 0" })
    .max(5000000, { message: "Annual expenses must be less than $5,000,000" }),
  
  netIncome: z
    .number()
    .min(0, { message: "Net income must be at least 0" })
    .optional(),
  
  // Documentation
  titleDocument: z
    .string()
    .url({ message: "Please provide a valid title document URL" })
    .optional(),
  
  appraisalDocument: z
    .string()
    .url({ message: "Please provide a valid appraisal document URL" })
    .optional(),
  
  insuranceDocument: z
    .string()
    .url({ message: "Please provide a valid insurance document URL" })
    .optional(),
  
  surveyDocument: z
    .string()
    .url({ message: "Please provide a valid survey document URL" })
    .optional(),
  
  // Tokenization Parameters
  tokenSupply: z
    .number()
    .min(1, { message: "Token supply must be at least 1" })
    .optional(),
  
  tokenPrice: z
    .number()
    .min(0.01, { message: "Token price must be at least $0.01" })
    .optional(),
  
  tokenNaming: z
    .string()
    .min(1, { message: "Token naming convention is required" })
    .optional(),
});

export type PropertyDetailsSchema = z.infer<typeof propertyDetailsSchema>;

export const propertyDetailsSchemaDefaultValues: PropertyDetailsSchema = {
  propertyAddress: "",
  propertyType: "Single Family Residential",
  propertyValue: 0,
  totalSquareFootage: 0,
  annualRentalIncome: 0,
  annualExpenses: 0,
  netIncome: 0,
  titleDocument: undefined,
  appraisalDocument: undefined,
  insuranceDocument: undefined,
  surveyDocument: undefined,
  tokenSupply: 0,
  tokenPrice: 0,
  tokenNaming: "",
};

export const propertyDetailsSchemaFields: FormFieldGroup[] = [
  {
    title: "Property Information",
    fields: [
      {
        name: "propertyAddress",
        placeholder: "123 Main Street, City, State 12345",
        type: "text",
        description: "Complete property address with street number and street name",
      },
      {
        name: "propertyType",
        placeholder: "Property Type",
        type: "text",
        disabled: true,
        description: "Fixed to Single Family Residential for Wyoming trusts",
      },
      {
        name: "propertyValue",
        placeholder: "Property Value ($)",
        type: "number",
        description: "Fair market value of the property (minimum $25,000,000)",
      },
      {
        name: "totalSquareFootage",
        placeholder: "Total Square Footage",
        type: "number",
        description: "Total square footage of property + structures (1 token = 1 sq ft)",
      },
    ],
  },
  {
    title: "Financial Information",
    fields: [
      {
        name: "annualRentalIncome",
        placeholder: "Annual Rental Income ($)",
        type: "number",
        description: "Expected annual rental income from the property",
      },
      {
        name: "annualExpenses",
        placeholder: "Annual Expenses ($)",
        type: "number",
        description: "Annual expenses including taxes, insurance, maintenance",
      },
      {
        name: "netIncome",
        placeholder: "Net Income ($)",
        type: "number",
        disabled: true,
        description: "Calculated automatically: Rental Income - Expenses",
      },
    ],
  },
  {
    title: "Documentation",
    fields: [
      {
        name: "titleDocument",
        placeholder: "Title Document",
        type: "file",
        description: "Deed, warranty deed, or other title document",
      },
      {
        name: "appraisalDocument",
        placeholder: "Appraisal Document",
        type: "file",
        description: "Third-party professional appraisal",
      },
      {
        name: "insuranceDocument",
        placeholder: "Insurance Document",
        type: "file",
        description: "Property insurance policy",
      },
      {
        name: "surveyDocument",
        placeholder: "Survey Document",
        type: "file",
        description: "Property survey and boundary information",
      },
    ],
  },
  {
    title: "Tokenization Preview",
    fields: [
      {
        name: "tokenSupply",
        placeholder: "Token Supply",
        type: "number",
        disabled: true,
        description: "Calculated automatically: Total Square Footage",
      },
      {
        name: "tokenPrice",
        placeholder: "Token Price ($)",
        type: "number",
        disabled: true,
        description: "Calculated automatically: Property Value ÷ Token Supply",
      },
      {
        name: "tokenNaming",
        placeholder: "Token Naming Convention",
        type: "text",
        disabled: true,
        description: "Auto-generated based on property details",
      },
    ],
  },
];
