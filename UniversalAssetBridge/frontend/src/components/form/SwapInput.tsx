"use client";

import { Button } from "@/components/ui/button";
import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { SelectedCrypto } from "@/types/crypto/crypto.type";
import { FormField as FormFieldType } from "@/types/form/formField.type";
import Image from "next/image";
import React, { KeyboardEvent } from "react";
import { UseFormReturn } from "react-hook-form";

interface SwapInputProps {
  form: UseFormReturn<any>;
  input: FormFieldType;
  description?: string | React.ReactNode;
  token?: SelectedCrypto;
  disabled?: boolean;
  openCryptoModal?: (value: "from" | "to") => void;
  changeLastChanged?: (value: number) => void;
}

export default function SwapInput({
  form,
  input,
  token,
  disabled = false,
  description,
  openCryptoModal,
  changeLastChanged,
}: SwapInputProps) {
  const tokenInputs =
    (input.name === "fromAmount" || input.name === "toAmount") &&
    openCryptoModal;
  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (tokenInputs) {
      const { key } = event;
      if (!/^[0-9.]+$/.test(key) && key !== "Backspace" && key !== "Delete") {
        event.preventDefault();
      }
    }
  };

  return (
    <div className="flex gap-2 items-center bg-white rounded-md p-5">
      <FormField
        control={form.control}
        name={input.name}
        render={({ field }) => (
          <FormItem className="flex flex-col w-full">
            <FormLabel className="text-primary">{input.placeholder}</FormLabel>
            <FormControl>
              <input
                className="input-swap mb-1 sm:pb-2!"
                step="0.00001"
                placeholder={tokenInputs ? token?.token : input.placeholder}
                onKeyDown={handleKeyDown}
                disabled={disabled}
                {...field}
                value={field.value ?? ""}
                onChange={(e) => {
                  const value = parseFloat(e.target.value) || e.target.value;
                  field.onChange(value === "" ? null : value);
                  if (changeLastChanged) {
                    changeLastChanged(value as number);
                  }
                }}
              />
            </FormControl>
            <FormDescription className="font-mono text-primary">
              {description}
            </FormDescription>
            <FormMessage />
          </FormItem>
        )}
      />
      {tokenInputs ? (
        <Button
          variant="secondary"
          size="lg"
          type="button"
          className="text-lg rounded-full px-4 flex justify-center gap-[7px] items-center sm:text-base xxs:py-1 transition-all"
          onClick={() =>
            openCryptoModal(input.placeholder.toLowerCase() as "from" | "to")
          }
        >
          <Image
            src={`/${token?.token}.png`}
            alt=""
            width={24}
            height={24}
            className="sm:w-5"
          />
          {token?.token}
        </Button>
      ) : null}
    </div>
  );
}
