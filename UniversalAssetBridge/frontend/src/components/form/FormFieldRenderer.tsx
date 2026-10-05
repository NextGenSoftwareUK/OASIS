import DateField from "@/components/form/fields/DateField";
import FileDropField from "@/components/form/fields/FileDropField";
import InputField from "@/components/form/fields/InputField";
import MapPickerField from "@/components/form/fields/MapPickerField";
import SelectField from "@/components/form/fields/SelectField";
import CheckboxField from "@/components/form/fields/CheckboxField";
import TextareaField from "@/components/form/fields/TextareaField";
import MultiselectField from "@/components/form/fields/MultiselectField";
import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { FormField as FormFieldType } from "@/types/form/formField.type";
import { FieldProps } from "@/types/form/formProps.type";
import { MAX_FILE_SIZE } from "@/lib/constants";

type FieldConfig = FormFieldType["type"];

interface FormFieldRendererProps extends FieldProps {
  fieldType: FieldConfig;
}

export function FormFieldRenderer({
  form,
  input,
  formMessageClasses,
  formLabelClasses,
  inputFieldClasses,
  withFormLabel = true,
  coords,
  fieldType,
}: FormFieldRendererProps) {
  const isInputField = ["text", "number", "email", "password"].includes(
    fieldType
  );
  return (
    <>
      <FormField
        control={form.control}
        name={input.name}
        render={({ field: rhfField }) => {
          return (
            <>
              <FormItem className="w-full">
                {withFormLabel && (
                  <FormLabel className={formLabelClasses}>
                    {input.placeholder}
                  </FormLabel>
                )}
                {isInputField && (
                  <InputField
                    inputFieldClasses={inputFieldClasses}
                    form={form}
                    input={input}
                    field={rhfField}
                  />
                )}
                {fieldType === "select" && (
                  <SelectField form={form} input={input} field={rhfField} />
                )}
                {fieldType === "date" && (
                  <DateField form={form} input={input} field={rhfField} />
                )}
                {fieldType === "map" && (
                  <MapPickerField
                    form={form}
                    input={input}
                    coords={coords}
                    field={rhfField}
                  />
                )}
                {fieldType === "file" && (
                  <FormControl>
                    <FileDropField
                      form={form}
                      field={rhfField}
                      maxSize={MAX_FILE_SIZE}
                      accept={{ "image/*": [], "application/pdf": [] }}
                    />
                  </FormControl>
                )}
                {fieldType === "checkbox" && (
                  <CheckboxField form={form} input={input} field={rhfField} />
                )}
                {fieldType === "textarea" && (
                  <TextareaField form={form} input={input} field={rhfField} />
                )}
                {fieldType === "multiselect" && (
                  <MultiselectField form={form} input={input} field={rhfField} />
                )}
                {input.description && (
                  <FormDescription>{input.description}</FormDescription>
                )}
                <FormMessage className={formMessageClasses} />
              </FormItem>
            </>
          );
        }}
      />
    </>
  );
}
