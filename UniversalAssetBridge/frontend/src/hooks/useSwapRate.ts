import { useEffect, useState } from "react";
import { UseFormReturn } from "react-hook-form";

export const useSwapRate = (form: UseFormReturn<any>, exchangeRate: number) => {
  const [lastChanged, setLastChanged] = useState<"from" | "to">("from");

  const fromAmount = form.watch("fromAmount");
  const toAmount = form.watch("toAmount");

  const handleFromChange = (value: number) => {
    setLastChanged("from");
    form.setValue("fromAmount", value);
  };

  const handleToChange = (value: number) => {
    setLastChanged("to");
    form.setValue("toAmount", value);
  };

  useEffect(() => {
    if (!exchangeRate) return;

    if (lastChanged === "from" && fromAmount) {
      form.setValue("toAmount", fromAmount * exchangeRate);
    } else if (lastChanged === "to" && toAmount) {
      form.setValue("fromAmount", toAmount / exchangeRate);
    }
  }, [fromAmount, toAmount, lastChanged, exchangeRate]);

  return {
    handleFromChange,
    handleToChange,
    fromAmount,
    toAmount,
    lastChanged,
    setLastChanged,
  };
};
