"use client";

import dynamic from "next/dynamic";
import { useState } from "react";
import { FormControl } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { useCoords } from "@/hooks/useCoords";
import { FieldProps } from "@/types/form/formProps.type";

const LocationPickerModal = dynamic(
  () => import("@/components/LocationPickerModal"),
  {
    ssr: false,
  }
);

export default function MapPickerField({ form, input, field }: FieldProps) {
  const [isMapOpen, setIsMapOpen] = useState(false);
  const { coords, setCoords } = useCoords(form);

  if (!field) return null;

  return (
    <>
      <FormControl>
        <Input
          onClick={() => {
            if (setIsMapOpen) setIsMapOpen(true);
          }}
          placeholder={input.placeholder}
          className="cursor-pointer"
          {...field}
          value={coords ? `${coords.latitude} ${coords.longitude}` : ""}
          onChange={field.onChange}
        />
      </FormControl>
      {isMapOpen && (
        <LocationPickerModal
          onSelect={(newCoords) => {
            setCoords(newCoords);
            // setIsMapOpen(false);
          }}
          setIsOpen={setIsMapOpen}
        />
      )}
    </>
  );
}
