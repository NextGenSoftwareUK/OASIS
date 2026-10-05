import { Checkbox } from "@/components/ui/checkbox";
import { FormField as FormFieldType } from "@/types/form/formField.type";
import { FieldProps } from "@/types/form/formProps.type";

interface CheckboxFieldProps extends FieldProps {
  field: any;
}

export default function CheckboxField({ form, input, field }: CheckboxFieldProps) {
  return (
    <Checkbox
      checked={field.value}
      onCheckedChange={field.onChange}
      disabled={input.disabled}
    />
  );
}
