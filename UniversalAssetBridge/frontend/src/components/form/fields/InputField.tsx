import { FormControl } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { FieldProps } from "@/types/form/formProps.type";

export default function InputField({
  field,
  input,
  inputFieldClasses,
}: FieldProps) {
  if (!field) return null;

  return (
    <FormControl>
      <Input
        disabled={input.disabled}
        type={input.type}
        className={`${inputFieldClasses}`}
        placeholder={input.placeholder}
        {...field}
        value={field.value ?? ""}
        onChange={(e) => {
          const value = parseFloat(e.target.value) || e.target.value;
          field.onChange(value);
        }}
      />
    </FormControl>
  );
}
