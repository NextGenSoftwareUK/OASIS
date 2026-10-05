import { create } from "zustand";
import { UseFormReturn } from "react-hook-form";

type FiltersFormStore = {
  form: UseFormReturn<any> | null;
  setForm: (form: UseFormReturn<any>) => void;
};

export const useFiltersFormStore = create<FiltersFormStore>((set) => ({
  form: null,
  setForm: (form) => set({ form }),
}));
