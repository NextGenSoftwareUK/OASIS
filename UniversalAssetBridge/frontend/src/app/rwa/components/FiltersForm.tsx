"use client";

import { Button } from "@/components/ui/button";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import {
  FiltersSchema,
  filtersSchema,
  filtersSchemaDefaultValues,
  filtersSchemaFields,
} from "@/schemas/rwa/rwaFilters.schema";
import { RwaFiltersParams } from "@/types/rwa/rwa.type";
import { zodResolver } from "@hookform/resolvers/zod";
import { Dispatch, SetStateAction, useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import InputFilterField from "@/app/rwa/components/form/InputFilterField";
import SelectFilterField from "@/app/rwa/components/form/SelectFilterField";
import { RangeSlider } from "@/components/ui/range-slider";
import { Input } from "@/components/ui/input";
import { ASSET_TYPES } from "@/lib/constants";
import { Checkbox } from "@/components/ui/checkbox";
import { Search } from "lucide-react";
import { useFiltersFormStore } from "@/store/useFiltersFormStore";

interface FiltersFormProps {
  setReqParams: Dispatch<SetStateAction<RwaFiltersParams>>;
  setIsFiltersOpen?: Dispatch<SetStateAction<boolean>>;
}

export default function FiltersForm({
  setReqParams,
  setIsFiltersOpen,
}: FiltersFormProps) {
  const [inputClasses] = useState(
    "px-3.5 border-text-gray rounded-sm text-sm w-full lg:text-base lg:text-black"
  );
  const [search, setSearch] = useState("");
  const { setForm } = useFiltersFormStore();

  const form = useForm<FiltersSchema>({
    resolver: zodResolver(filtersSchema),
    defaultValues: filtersSchemaDefaultValues,
  });

  const priceMin = form.watch("priceMin");
  const priceMax = form.watch("priceMax");

  const onSubmit = (data: z.infer<typeof filtersSchema>) => {
    setReqParams((prevState: RwaFiltersParams) => {
      return {
        ...prevState,
        ...data,
      };
    });
  };

  useEffect(() => {
    setForm(form);
  }, [form, setForm]);

  return (
    <Form {...form}>
      <form
        className="flex flex-col gap-3 text-sm text-nowrap lg:text-base"
        onSubmit={form.handleSubmit(onSubmit)}
      >
        <p className="p mb-7">Price</p>
        <RangeSlider
          min={0}
          max={1000}
          step={10}
          value={[priceMin || 0, priceMax || 1000]}
          onValueChange={(values) => {
            form.setValue("priceMin", values[0]);
            form.setValue("priceMax", values[1]);
          }}
        />

        <div className="flex gap-1 mt-0">
          <FormField
            control={form.control}
            name="priceMin"
            render={({ field }) => (
              <FormItem className="w-full">
                <FormLabel>From</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    className={`${inputClasses}`}
                    placeholder="Min"
                    {...field}
                    value={field.value === null ? "" : field.value}
                  />
                </FormControl>
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="priceMax"
            render={({ field }) => (
              <FormItem className="w-full">
                <FormLabel>To</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    className={`${inputClasses}`}
                    placeholder="Max"
                    {...field}
                    value={field.value === null ? "" : field.value}
                  />
                </FormControl>
              </FormItem>
            )}
          />
        </div>

        <hr className="my-5 border-muted/30" />

        <FormField
          control={form.control}
          name="assetType"
          render={() => (
            <FormItem>
              <div className="mb-4">
                <FormLabel className="text-base">Asset Type</FormLabel>
                <Input
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  icon={
                    <Search
                      color="var(--primary)"
                      size={20}
                      className="!w-5 !h-5"
                    />
                  }
                  className={inputClasses}
                  iconPosition="right"
                  type="text"
                  placeholder="Search for type"
                />
              </div>
              {ASSET_TYPES.filter((item) =>
                item.name.toLowerCase().includes(search.toLowerCase())
              ).map((item) => (
                <FormField
                  key={item.value}
                  control={form.control}
                  name="assetType"
                  render={({ field }) => {
                    return (
                      <FormItem
                        key={item.value}
                        className="flex flex-row items-center gap-2"
                      >
                        <FormControl>
                          <Checkbox
                            id={item.value}
                            // checked={field.value?.includes(item.value)}
                            // onCheckedChange={(checked) => {
                            //   return checked
                            //     ? field.onChange([...field.value, item.value])
                            //     : field.onChange(
                            //         field.value?.filter(
                            //           (value) => value !== item.value
                            //         )
                            //       );
                            // }}
                          />
                        </FormControl>
                        <FormLabel className="text-sm font-normal">
                          {item.name}
                        </FormLabel>
                      </FormItem>
                    );
                  }}
                />
              ))}
              <FormMessage />
            </FormItem>
          )}
        />
      </form>
    </Form>
  );
}
