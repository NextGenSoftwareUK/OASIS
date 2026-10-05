import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { FormField as FormFieldType } from "@/types/form/formField.type";
import { FieldProps } from "@/types/form/formProps.type";
import { ChevronDown } from "lucide-react";

interface MultiselectFieldProps extends FieldProps {
  field: any;
}

export default function MultiselectField({ form, input, field }: MultiselectFieldProps) {
  const [open, setOpen] = useState(false);
  const selectedValues = field.value || [];

  const handleSelect = (value: string) => {
    const newValues = selectedValues.includes(value)
      ? selectedValues.filter((v: string) => v !== value)
      : [...selectedValues, value];
    field.onChange(newValues);
  };

  const selectedLabels = selectedValues.map((value: string) => 
    input.selectItems?.find(item => item.value === value)?.name || value
  );

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="w-full justify-between"
          disabled={input.disabled}
        >
          {selectedLabels.length > 0 
            ? `${selectedLabels.length} selected` 
            : input.placeholder
          }
          <ChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-full p-0">
        <div className="max-h-60 overflow-auto">
          {input.selectItems?.map((item) => (
            <div
              key={item.value}
              className="flex items-center space-x-2 p-2 hover:bg-gray-100 cursor-pointer"
              onClick={() => handleSelect(item.value)}
            >
              <Checkbox
                checked={selectedValues.includes(item.value)}
              />
              <span className="text-sm">{item.name}</span>
            </div>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  );
}
