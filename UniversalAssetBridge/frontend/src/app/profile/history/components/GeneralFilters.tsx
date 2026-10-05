"use client";

import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Form, FormControl, FormField, FormItem } from "@/components/ui/form";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { SORT_ORDER } from "@/lib/constants";
import { cn } from "@/lib/utils";
import {
  generalHistoryFiltersSchema,
  generalHistoryFiltersSchemaDefaultValues,
  GeneralHistoryFiltersSchemaType,
} from "@/schemas/profile/historyFilters.schema";
import { zodResolver } from "@hookform/resolvers/zod";
import { format } from "date-fns";
import { CalendarIcon } from "lucide-react";
import { useForm } from "react-hook-form";

interface GeneralFiltersProps {
  className?: string;
}

export default function GeneralFilters({ className }: GeneralFiltersProps) {
  const form = useForm<GeneralHistoryFiltersSchemaType>({
    resolver: zodResolver(generalHistoryFiltersSchema),
    defaultValues: generalHistoryFiltersSchemaDefaultValues,
  });

  return (
    <div className={cn("", className)}>
      <Form {...form}>
        <form className="flex items-center gap-7.5">
          <FormField
            control={form.control}
            name="period"
            render={({ field }) => (
              <FormItem>
                <Popover>
                  <PopoverTrigger asChild>
                    <FormControl>
                      <Button
                        variant="empty"
                        size="normal"
                        className={cn(
                          "pl-3 text-left bg-transparent !text-white p-0",
                          !field.value?.from && "text-muted-foreground"
                        )}
                      >
                        {field.value?.from ? (
                          field.value.to ? (
                            <>
                              {format(field.value.from, "PPP")} –{" "}
                              {format(field.value.to, "PPP")}
                            </>
                          ) : (
                            format(field.value.from, "PPP")
                          )
                        ) : (
                          <span>Period</span>
                        )}
                        <CalendarIcon className="ml-auto h-4 w-4" />
                      </Button>
                    </FormControl>
                  </PopoverTrigger>

                  <PopoverContent className="w-auto p-0" align="start">
                    <Calendar
                      mode="range"
                      selected={{
                        from: field.value?.from
                          ? new Date(field.value.from)
                          : undefined,
                        to: field.value?.to
                          ? new Date(field.value.to)
                          : undefined,
                      }}
                      onSelect={field.onChange}
                      disabled={(date) =>
                        date > new Date() || date < new Date("1900-01-01")
                      }
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="sortOrder"
            render={({ field }) => (
              <FormItem>
                <Select
                  onValueChange={field.onChange}
                  defaultValue={field.value}
                >
                  <FormControl>
                    <SelectTrigger className="bg-transparent gap-2 text-sm p-0 !text-white border-none focus:ring-0">
                      <SelectValue placeholder="Sort Order" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectGroup>
                      {/* <SelectLabel>Sort Order</SelectLabel> */}
                      {SORT_ORDER.map((item, i) => (
                        <SelectItem key={i} value={item.value}>
                          {item.name}
                        </SelectItem>
                      ))}
                    </SelectGroup>
                  </SelectContent>
                </Select>
              </FormItem>
            )}
          />
        </form>
      </Form>
    </div>
  );
}
