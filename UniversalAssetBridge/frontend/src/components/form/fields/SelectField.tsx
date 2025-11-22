import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { FieldProps } from "@/types/form/formProps.type";

export default function SelectField({
  input,
  selectTriggerClasses,
  withSelectLabel = true,
  field,
}: FieldProps) {
  if (!field) return null;

  return (
    <Select
      onValueChange={(val) => {
        if (val === "") {
          return;
        }
        field.onChange(val);
      }}
      value={field.value}
      defaultValue={field.value}
    >
      <FormControl>
        <SelectTrigger className={`${selectTriggerClasses}`}>
          <SelectValue placeholder={input.placeholder} />
        </SelectTrigger>
      </FormControl>
      <SelectContent>
        <SelectGroup>
          {withSelectLabel && <SelectLabel>{input.placeholder}</SelectLabel>}
          {input?.selectItems?.map((item, i) => (
            <SelectItem key={i} value={item.value}>
              {item.name}
            </SelectItem>
          ))}
        </SelectGroup>
      </SelectContent>
    </Select>
  );
}
