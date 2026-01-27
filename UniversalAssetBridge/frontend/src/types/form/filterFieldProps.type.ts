import { FormField } from "@/types/form/formField.type";
import { UseFormReturn } from "react-hook-form";

export interface FilterFieldProps {
  form: UseFormReturn<any>;
  inputClasses: string;
  input: FormField;
}
