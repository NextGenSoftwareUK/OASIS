"use client";

import PageTitle from "@/components/PageTitle";
import SelectField from "@/components/form/fields/SelectField";
import InputField from "@/components/form/fields/InputField";
import InfoRow from "@/app/rwa/components/InfoRow";
import CopyIpfsButton from "@/app/rwa/components/CopyIpfsButton";
import Loading from "@/components/Loading";
import UpdatingModal from "./UpdatingModal";
import AllRwaData from "@/components/AllRwaData";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Form } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { useEffect, useState } from "react";
import { useUserStore } from "@/store/useUserStore";
import { redirect, useParams } from "next/navigation";
import { useGetRwa } from "@/requests/rwa/getRwa.request";
import { useUpdateRwa } from "@/requests/rwa/updateRwa.request";
import {
  tokenizeBaseSchema,
  TokenizeBaseSchema,
  tokenizeBaseSchemaDefaultValues,
  tokenizeBaseSchemaFields,
} from "@/schemas/rwa/tokenizeBase.schema";
import { useNetAmount } from "@/hooks/useNetAmount";
import { Rwa } from "@/types/rwa/rwa.type";

export default function ChangeRwa() {
  const params = useParams<{ id: string }>();
  const { user } = useUserStore();

  const [initialData, setInitialData] = useState<Rwa | null>(null);
  const [isDataChanged, setIsDataChanged] = useState(false);
  const [isUpdated, setIsUpdated] = useState(false);
  const [isSuccessfullyDone, setIsSuccessfullyDone] = useState(false);
  const [isError, setIsError] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [isAlldataOpen, setIsAlldataOpen] = useState(false);

  const {
    data,
    isFetching,
    isFetched,
    isError: isRwaError,
  } = useGetRwa(params.id);
  const submit = useUpdateRwa(params.id);

  const form = useForm<TokenizeBaseSchema>({
    resolver: zodResolver(tokenizeBaseSchema),
    defaultValues: tokenizeBaseSchemaDefaultValues,
  });

  const price = form.watch("price");
  const royalty = form.watch("royalty");

  const netAmount = useNetAmount(price, royalty);
  const existedNetAmount = useNetAmount(data?.data?.price, data?.data?.royalty);

  const onSubmit = (data: TokenizeBaseSchema) => {
    setIsError(false);
    setErrorMessage("");
    setIsDataChanged(false);

    if (!initialData) return;

    const isChanged = Object.entries(data).some(
      ([key, value]) => initialData[key as keyof Rwa] !== value
    );

    if (!isChanged) {
      setIsDataChanged(true);
      return;
    }

    setIsUpdated(true);

    submit.mutate(data, {
      onSuccess: () => {
        setIsSuccessfullyDone(true);
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

  useEffect(() => {
    if (data) {
      const values = data.data;
      Object.entries(values).forEach(([key, value]: [string, any]) => {
        form.setValue(key as keyof TokenizeBaseSchema, value);
      });
      setInitialData(values);
    }
  }, [data]);

  useEffect(() => {
    if (isFetched && data) {
      if (data.data.ownerUsername === user?.UserName) {
        return;
      } else {
        redirect("/");
      }
    }
  }, [data, isFetched, user?.UserName]);

  if (isFetching || !isFetched) {
    return (
      <Loading
        className="flex justify-center mt-14"
        classNameLoading="border-white! border-r-transparent! w-14! h-14!"
      />
    );
  }

  if (!data || isRwaError) {
    return null;
  }

  const ipfsCID = data.data.image.replace("https://ipfs.io/ipfs/", "");

  return (
    <>
      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(onSubmit)}
          className="flex gap-10 items-start lg:gap-5 md:flex-col-reverse"
        >
          <div className="w-1/2 md:w-full">
            <PageTitle title="Update RWA" />
            <div className="flex flex-col gap-2 firstStep">
              {tokenizeBaseSchemaFields
                .flatMap(group => group.fields)
                .filter(
                  (item) =>
                    item.name === "title" ||
                    item.name === "assetDescription" ||
                    item.name === "uniqueIdentifier" ||
                    item.name === "network"
                )
                .map((item, i) => (
                  <div key={i}>
                    {item?.selectItems ? (
                      <SelectField form={form} input={item} />
                    ) : (
                      <InputField form={form} input={item} />
                    )}
                  </div>
                ))}

              <div className="flex justify-between gap-2">
                {tokenizeBaseSchemaFields
                  .flatMap(group => group.fields)
                  .filter(
                    (item) => item.name === "price" || item.name === "royalty"
                  )
                  .map((item, i) => (
                    <div key={i}>
                      <InputField form={form} input={item} />
                    </div>
                  ))}
                <div className="w-1/3">
                  <Input
                    type="number"
                    placeholder="Net amount"
                    disabled={true}
                    value={netAmount}
                  />
                </div>
              </div>

              {tokenizeBaseSchemaFields
                .flatMap(group => group.fields)
                .filter(
                  (item) =>
                    item.name === "ownerContact" ||
                    item.name === "proofOfOwnershipDocument" ||
                    item.name === "assetType"
                )
                .map((item, i) => (
                  <div key={i}>
                    {item?.selectItems ? (
                      <SelectField form={form} input={item} />
                    ) : (
                      <InputField
                        isFileField={item.name === "proofOfOwnershipDocument"}
                        withFormLabel={item.name === "proofOfOwnershipDocument"}
                        formLabelClasses="text-white"
                        form={form}
                        input={item}
                      />
                    )}
                  </div>
                ))}
            </div>
            {isDataChanged && (
              <p className="p-sm text-destructive mt-2">
                You haven't changed anything.
              </p>
            )}
            <Button
              variant="gray"
              type="submit"
              size="xl"
              className="w-full mt-2"
            >
              Update
            </Button>
          </div>
          <div className="w-1/2 aspect-3/2 rounded-2xl md:w-full md:aspect-auto">
            <div className="relative aspect-3/2 w-full max-w-full bg-neutral-700/50 rounded-2xl p-5 flex items-center justify-center overflow-hidden">
              <img
                src={data?.data.image}
                alt={data?.data.title}
                className="object-contain max-h-full! w-auto! rounded-2xl"
              />
            </div>
            <div className="flex flex-col gap-2 mt-2">
              <InfoRow label="Version" value={data?.data.version} />
              <InfoRow
                label="IPFS CID"
                value={<CopyIpfsButton cid={ipfsCID} />}
              />
              <InfoRow label="Price" value={`${data?.data.price} zBTC`} />
              <InfoRow label="Royalty" value={`${data?.data.royalty}%`} />
              <InfoRow label="Net Amout" value={existedNetAmount} />
              <InfoRow label="State" value="Listed" />

              <Button
                variant="gray"
                size="default"
                type="button"
                onClick={() => setIsAlldataOpen(true)}
                className="px-5! w-full! flex justify-center flex-wrap cursor-pointer"
              >
                Show all information
              </Button>
            </div>
          </div>
        </form>
      </Form>
      {isUpdated && (
        <UpdatingModal
          errorMessage={errorMessage}
          isError={isError}
          isSuccessfullyDone={isSuccessfullyDone}
          setIsSuccessfullyDone={setIsSuccessfullyDone}
          setIsUpdated={setIsUpdated}
          tokenId={data?.data.tokenId}
        />
      )}
      {isAlldataOpen && (
        <AllRwaData data={data?.data} setIsOpen={setIsAlldataOpen} />
      )}
    </>
  );
}
