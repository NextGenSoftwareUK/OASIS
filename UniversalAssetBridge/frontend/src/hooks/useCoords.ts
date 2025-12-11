import { Coords } from "@/types/form/coords.type";
import { useEffect, useState } from "react";
import { UseFormReturn } from "react-hook-form";

export const useCoords = (form: UseFormReturn<any>) => {
  const [coords, setCoords] = useState<Coords>(null);

  useEffect(() => {
    if (coords) {
      form.setValue("geolocation", {
        latitude: coords.latitude,
        longitude: coords.longitude,
      });
    }
  }, [coords]);

  return {
    coords,
    setCoords,
  };
};
