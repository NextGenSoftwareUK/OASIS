import { FormControl, FormField, FormItem } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { FilterFieldProps } from "@/types/form/filterFieldProps.type";

export default function InputFilterField({
  form,
  inputClasses,
  input,
}: FilterFieldProps) {
  return (
    <FormField
      control={form.control}
      name={input.name}
      render={({ field }) => (
        <>
          <FormItem className="w-full">
            <FormControl>
              <Input
                type={input.type}
                className={`${inputClasses} text-right`}
                placeholder={input.placeholder}
                {...field}
                value={field.value === null ? "" : field.value}
              />
            </FormControl>
          </FormItem>
        </>
      )}
    />
  );
}
