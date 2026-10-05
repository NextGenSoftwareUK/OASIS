"use client";

import TokenizationModal from "./TokenizationModal";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Form } from "@/components/ui/form";
import { Button } from "@/components/ui/button";
import { useEffect, useState } from "react";
import { DragAndDropUpload } from "@/app/rwa/create/components/DragAndDropUpload";
import { useCreateRwa } from "@/requests/rwa/createRwa.request";
import { useTokenizationFields } from "@/hooks/useTokenizationFields";
import { getFieldsByAssetType } from "@/utils/getFieldsByAssetType.util";
import { MoveLeft, MoveRight } from "lucide-react";
import { FormFieldRenderer } from "@/components/form/FormFieldRenderer";
import { useNetAmount } from "@/hooks/useNetAmount";

export default function CreateRwa() {
  const [isSecondStep, setIsSecondStep] = useState(false);
  const [selectedAssetType, setSelectedAssetType] = useState("");
  const [isTokenized, setIsTokenized] = useState(false);
  const [isSuccessfullyDone, setIsSuccessfullyDone] = useState(false);
  const [isError, setIsError] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [tokenId, setTokenId] = useState("");

  const submit = useCreateRwa();

  const { tokenizeSchema, tokenizeFields, defaultTokenizeValues } =
    useTokenizationFields(selectedAssetType);

  const form = useForm<z.infer<typeof tokenizeSchema>>({
    resolver: zodResolver(tokenizeSchema),
    defaultValues: defaultTokenizeValues,
  });

  const assetType = form.watch("assetType");
  const price = form.watch("price");
  const royalty = form.watch("royalty");

  const netAmount = useNetAmount(price, royalty);

  const onSubmit = (data: z.infer<typeof tokenizeSchema>) => {
    setIsTokenized(true);
    setIsError(false);
    setErrorMessage("");
    submit.mutate(data, {
      onSuccess: (res: any) => {
        setIsSuccessfullyDone(true);
        setTokenId(res.data.tokenId);
      },
      onError: (error: any) => {
        setIsError(true);
        setErrorMessage(
          error.response?.data?.error?.message ||
            "Something went wrong. Please try again later."
        );
      },
    });
  };

  const checkFirstStep = async () => {
    const isValid = await form.trigger([
      "title",
      "assetDescription",
      "uniqueIdentifier",
      "network",
      "price",
      "royalty",
      "ownerContact",
      "image",
      "proofOfOwnershipDocument",
      "assetType",
    ]);
    if (isValid) {
      setIsSecondStep(true);
    }
  };

  useEffect(() => {
    if (assetType) {
      setSelectedAssetType(assetType);
    }
  }, [assetType]);

  return (
    <>
      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(onSubmit)}
          className="grid grid-cols-11 gap-20 items-start lg:gap-5 md:flex-col"
        >
          <DragAndDropUpload
            control={form.control}
            name="image"
            isSuccessfullyDone={isSuccessfullyDone}
            className="col-span-5"
          />
          <div className="col-span-6 md:w-full max-w-[600px]">
            <div className={`firstStep ${isSecondStep ? "hidden" : "block"}`}>
              {tokenizeFields.map((item, i) => (
                <div key={i}>
                  <h3
                    className={`h3 mb-5 text-secondary ${i === 0 ? "mt-0" : "mt-7"}`}
                  >
                    {item.title}
                  </h3>
                  <div className="flex flex-col gap-[14px]">
                    {item.fields.map((formField, i) => (
                      <FormFieldRenderer
                        fieldType={formField.type}
                        key={i}
                        form={form}
                        input={formField}
                      />
                    ))}
                  </div>
                </div>
              ))}
            </div>
            <div
              className={`flex flex-col gap-[14px] isSecondStep ${
                isSecondStep ? "block" : "hidden"
              }`}
            >
              <h2 className="h2 mb-2 text-white border-b border-text-gray pb-2">
                Additional fields for {assetType}
              </h2>
              {getFieldsByAssetType(selectedAssetType).fields.map((item, i) => (
                <div key={i}>
                  <h3
                    className={`h3 mb-5 text-secondary ${i === 0 ? "mt-0" : "mt-7"}`}
                  >
                    {item.title}
                  </h3>
                  <div className="flex flex-col gap-[14px]">
                    {item.fields.map((formField, i) => (
                      <FormFieldRenderer
                        fieldType={formField.type}
                        key={i}
                        form={form}
                        input={formField}
                      />
                    ))}
                  </div>
                </div>
              ))}
            </div>

            <div className="flex justify-end gap-2 mt-10">
              <Button
                onClick={() => {
                  setIsSecondStep(false);
                }}
                variant="default"
                type="button"
                size="lg"
                className={`${isSecondStep ? "flex gap-5 pl-5" : "hidden"}`}
              >
                <MoveLeft />
                Back
              </Button>
              <Button
                onClick={checkFirstStep}
                variant="default"
                type="button"
                size="lg"
                className={`${isSecondStep ? "hidden" : "flex gap-5 pr-5"}`}
              >
                Continue
                <MoveRight />
              </Button>
              <Button
                variant="default"
                type="submit"
                size="lg"
                className={`${isSecondStep ? "flex gap-5 pr-5" : "hidden"}`}
              >
                Mint RWA
                <MoveRight />
              </Button>
            </div>
          </div>
        </form>
      </Form>
      {isTokenized && (
        <TokenizationModal
          tokenId={tokenId}
          form={form}
          errorMessage={errorMessage}
          isError={isError}
          isSuccessfullyDone={isSuccessfullyDone}
          setIsSuccessfullyDone={setIsSuccessfullyDone}
          setIsOpen={setIsTokenized}
          setIsSecondStep={setIsSecondStep}
        />
      )}
    </>
  );
}
