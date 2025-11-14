import { Textarea } from "@/components/ui/textarea";
import { FormField as FormFieldType } from "@/types/form/formField.type";
import { FieldProps } from "@/types/form/formProps.type";

interface TextareaFieldProps extends FieldProps {
  field: any;
}

export default function TextareaField({ form, input, field }: TextareaFieldProps) {
  return (
    <Textarea
      placeholder={input.placeholder}
      disabled={input.disabled}
      {...field}
      className="min-h-[100px]"
    />
  );
}
