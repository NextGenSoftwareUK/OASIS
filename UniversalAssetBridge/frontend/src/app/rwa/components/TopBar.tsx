"use client";

import { Button } from "@/components/ui/button";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { SORT_BY, SORT_ORDER } from "@/lib/constants";
import { useFiltersFormStore } from "@/store/useFiltersFormStore";
import {
  ArrowDownNarrowWide,
  ArrowDownWideNarrow,
  ChevronLeft,
  Funnel,
  Search,
} from "lucide-react";
import { Dispatch, SetStateAction, useState } from "react";

interface TopBarProps {
  hideFilters: boolean;
  setHideFilers: Dispatch<SetStateAction<boolean>>;
}

export default function TopBar({ hideFilters, setHideFilers }: TopBarProps) {
  const { form } = useFiltersFormStore();
  const [search, setSearch] = useState("");

  if (!form) return null;

  return (
    <div className="flex justify-between items-center">
      <div className="flex gap-4">
        <Button
          variant="default"
          size="lg"
          onClick={() => setHideFilers((prevState) => !prevState)}
        >
          {hideFilters ? <Funnel /> : <ChevronLeft />}
          Filters
        </Button>

        <div className="w-[380px]">
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            icon={
              <Search color="var(--primary)" size={20} className="!w-5 !h-5" />
            }
            iconPosition="right"
            className="py-[9px] text-sm"
            type="text"
            placeholder="Search for type"
          />
        </div>
      </div>

      <div className="">
        <Form {...form}>
          <form className="flex gap-6">
            <FormField
              control={form.control}
              name="sortOrder"
              render={({ field }) => (
                <FormItem className="flex gap-2 items-center lg:justify-between">
                  <FormControl>
                    <p
                      className="p-sm cursor-pointer flex gap-2 items-center transition-all hover:opacity-70"
                      onClick={() => {
                        field.onChange(field.value === "asc" ? "desc" : "asc");
                      }}
                    >
                      Sort order
                      {field.value === "asc" && (
                        <ArrowDownNarrowWide size={16} />
                      )}
                      {field.value === "desc" && (
                        <ArrowDownWideNarrow size={16} />
                      )}
                    </p>
                  </FormControl>
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="sortBy"
              render={({ field }) => (
                <FormItem className="flex gap-2 items-center lg:justify-between">
                  <Select
                    onValueChange={field.onChange}
                    value={field.value}
                    defaultValue={field.value}
                  >
                    <FormControl>
                      <SelectTrigger
                        className="space-x-2 !text-white p-0 bg-transparent border-none focus:ring-0 
                    text-sm mt-0! transition-all hover:opacity-70 lg:max-w-[300px] sm:max-w-[160px]!"
                      >
                        <SelectValue placeholder="Sort by" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectGroup>
                        {SORT_BY.map((item, i) => (
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
    </div>
  );
}
