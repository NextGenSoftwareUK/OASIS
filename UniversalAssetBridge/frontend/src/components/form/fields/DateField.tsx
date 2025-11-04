import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { FormControl } from "@/components/ui/form";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import { FieldProps } from "@/types/form/formProps.type";
import { format } from "date-fns";
import { CalendarIcon } from "lucide-react";

export default function DateField({ input, field }: FieldProps) {
  if (!field) return null;

  return (
    <Popover>
      <PopoverTrigger asChild>
        <FormControl>
          <Button
            variant="empty"
            size="xl"
            className={cn(
              "py-1 px-[20px] w-full",
              !field.value && "text-muted-foreground"
            )}
          >
            {field.value ? (
              format(new Date(field.value), "PPP")
            ) : (
              <span>{input.placeholder}</span>
            )}
            <CalendarIcon className="ml-auto h-4 w-4 opacity-50" />
          </Button>
        </FormControl>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start">
        <Calendar
          mode="single"
          selected={field.value ? new Date(field.value) : undefined}
          onSelect={(date) => {
            if (date) {
              field.onChange(format(date, "yyyy-MM-dd"));
            }
          }}
          disabled={(date) =>
            date > new Date() || date < new Date("1900-01-01")
          }
          initialFocus
        />
      </PopoverContent>
    </Popover>
  );
}
