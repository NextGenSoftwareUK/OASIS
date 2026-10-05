"use client";

import { cn } from "@/lib/utils";
import GeneralFilters from "@/app/profile/history/components/GeneralFilters";
import {
  rwaTypeHistoryFiltersSchemaDefaultValues,
  RwaTypeHistoryFiltersSchemaType,
  rwaTypeHistroyFiltersSchema,
} from "@/schemas/profile/historyFilters.schema";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Form } from "@/components/ui/form";

interface FiltersProps {
  className?: string;
}

export default function Filters({ className }: FiltersProps) {
  const form = useForm<RwaTypeHistoryFiltersSchemaType>({
    resolver: zodResolver(rwaTypeHistroyFiltersSchema),
    defaultValues: rwaTypeHistoryFiltersSchemaDefaultValues,
  });

  const selected = form.watch("type");

  const toggleType = (value: string) => {
    if (selected.includes(value)) {
      form.setValue(
        "type",
        selected.filter((t) => t !== value),
        { shouldValidate: true }
      );
    } else {
      form.setValue("type", [...selected, value], { shouldValidate: true });
    }
  };

  return (
    <div className={cn("flex gap-7.5", className)}>
      <Form {...form}>
        <form className="flex gap-4">
          <p className="p-sm">Type: </p>
          <ul className="flex gap-3.5">
            {rwaTypeHistoryFiltersSchemaDefaultValues.type.map((item, i) => (
              <li
                onClick={() => toggleType(item)}
                className={`p-sm cursor-pointer transition-all hover:opacity-70 ${
                  selected.includes(item) && "border-b"
                }`}
                key={i}
              >
                {item}
              </li>
            ))}
          </ul>
        </form>
      </Form>
      <GeneralFilters />
    </div>
  );
}
