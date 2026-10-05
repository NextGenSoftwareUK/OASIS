"use client";

import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Check, ChevronDown, Loader2, RotateCcw } from "lucide-react";
import { useRouter } from "nextjs-toploader/app";

import {
  swapSchema,
  SwapSchema,
  swapSchemaDefaultValues,
  swapSchemaFields,
} from "@/schemas/swap/swap.schema";

import { useUserStore } from "@/store/useUserStore";
import { useSwap } from "@/hooks/swapHooks";
import { useGetExchangeRate } from "@/requests/swap/getExchangeRate.request";
import { useCreateOrder } from "@/requests/swap/createOrder.request";
import { getTokenUSDPrice } from "@/lib/exchangeRateService";

import { Form } from "@/components/ui/form";
import { Button } from "@/components/ui/button";
import SwapInput from "@/components/form/SwapInput";
import CryptoModal from "@/components/CryptoModal";
import StatusModal from "@/components/StatusModal";
import CryptoAddressModal from "@/components/CryptoAddressModal";

import { SwapFormData, SwapResponse } from "@/types/crypto/swap.type";
import SwapMicroInfo from "@/components/SwapMicroInfo";
import SwapTimer from "@/components/SwapTimer";
import { useSwapRate } from "@/hooks/useSwapRate";

export default function SwapForm() {
  const router = useRouter();
  const { user } = useUserStore();

  const form = useForm<SwapSchema>({
    resolver: zodResolver(swapSchema),
    defaultValues: swapSchemaDefaultValues,
  });

  const {
    selectedFrom,
    selectedTo,
    selectedNetwork,
    isCryptoModalOpen,
    openCryptoModal,
    closeCryptoModal,
    setSelectedFrom,
    setSelectedTo,
    selectNetwork,
    selectCrypto,
  } = useSwap();

  const [orderError, setOrderError] = useState("");
  // const [isLoading, setIsLoading] = useState(false);
  const [isOrderCompleted, setIsOrderCompleted] = useState(false);
  const [isCryptoAddressModalOpen, setCryptoAddressModalOpen] = useState(false);
  const [isStatusModalOpen, setStatusModalOpen] = useState(false);

  const [formData, setFormData] = useState<SwapFormData | null>(null);
  const [orderResponse, setOrderResponse] = useState<SwapResponse | null>(null);
  const [fromUSDPrice, setFromUSDPrice] = useState<number>(0);
  const [toUSDPrice, setToUSDPrice] = useState<number>(0);

  const prevFrom = useRef(selectedFrom);
  const prevTo = useRef(selectedTo);

  const {
    data: exchangeRate,
    isFetching,
    isError,
    isSuccess,
    refetch,
  } = useGetExchangeRate(selectedFrom.token, selectedTo.token);
  const submitOrder = useCreateOrder();

  const { handleFromChange, handleToChange, fromAmount, toAmount } =
    useSwapRate(form, exchangeRate?.data.rate);

  useEffect(() => {
    if (selectedFrom.token === selectedTo.token) {
      setSelectedFrom(prevTo.current);
      setSelectedTo(prevFrom.current);
    }
    prevFrom.current = selectedFrom;
    prevTo.current = selectedTo;
  }, [selectedFrom, selectedTo]);

  // Fetch USD prices when tokens change
  useEffect(() => {
    const fetchPrices = async () => {
      const fromPrice = await getTokenUSDPrice(selectedFrom.token);
      const toPrice = await getTokenUSDPrice(selectedTo.token);
      setFromUSDPrice(fromPrice);
      setToUSDPrice(toPrice);
    };
    fetchPrices();
  }, [selectedFrom.token, selectedTo.token]);

  useEffect(() => {
    if (isOrderCompleted) {
      setOrderError("");
      setFormData(null);
      setOrderResponse(null);
      setCryptoAddressModalOpen(false);
      setStatusModalOpen(false);
    }
  }, [isOrderCompleted]);

  const onSubmit = (values: SwapSchema) => {
    if (!user?.Id) {
      router.push("/signin");
      return;
    }

    // setIsLoading(true);
    const payload = {
      userId: user.Id,
      fromToken: selectedFrom.token,
      toToken: selectedTo.token,
      amount: values.fromAmount,
      fromNetwork: selectedFrom.network,
      toNetwork: selectedTo.network,
      destinationAddress: values.destinationAddress,
    };

    setFormData(payload);
    submitOrder.mutate(payload, {
      onSuccess: (res) => {
        setOrderResponse(res);
      },
      onError: (err: any) => {
        setOrderError(
          err.response?.data?.error?.message || "An error occurred"
        );
      },
    });

    setCryptoAddressModalOpen(true);
  };

  const handleSwap = () => {
    const temp = selectedFrom;
    setSelectedFrom(selectedTo);
    setSelectedTo(temp);
  };

  const closeStatusModal = () => {
    setIsOrderCompleted(true);
    setStatusModalOpen(false);
  };

  return (
    <div className="">
      <div className="flex justify-between items-start">
        <h3 className="h3 mb-[10px]">Swap</h3>
        <SwapTimer isSuccess={isSuccess} refetch={refetch} />
      </div>
      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(onSubmit)}
          className="flex flex-col gap-2 sm:gap-3 text-black"
        >
          {/* Input Fields */}
          <div className="flex flex-col gap-2 relative sm:gap-3">
            <SwapInput
              form={form}
              input={swapSchemaFields[0]}
              description={`$${(fromAmount * fromUSDPrice).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`}
              token={selectedFrom}
              openCryptoModal={openCryptoModal}
              changeLastChanged={handleFromChange}
            />

            <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2">
              <button
                type="button"
                className="bg-white w-10 h-10 flex justify-center items-center rounded-md border border-[#878787] shadow-xs group sm:w-8 sm:h-8"
                onClick={handleSwap}
              >
                <ChevronDown
                  size={20}
                  className="group-hover:rotate-180 transition-transform duration-200 sm:w-3"
                />
              </button>
            </div>

            <SwapInput
              form={form}
              input={swapSchemaFields[1]}
              description={`$${(toAmount * toUSDPrice).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`}
              token={selectedTo}
              openCryptoModal={openCryptoModal}
              changeLastChanged={handleToChange}
            />
          </div>

          <SwapInput
            form={form}
            input={swapSchemaFields[2]}
            description={
              <>
                <span className="flex items-center gap-1">
                  <Check size={14} />
                  Address is valid
                </span>
              </>
            }
          />

          {isError && (
            <div className="flex gap-3 items-center">
              <p className="p-sm text-red-500">Failed to get Exchange Rate</p>
              <p
                className="p-sm flex gap-2 items-center text-white cursor-pointer transition-all hover:opacity-70"
                onClick={() => refetch()}
              >
                Retry
                <RotateCcw color="#fff" size={14} />
              </p>
            </div>
          )}

          <Button
            type="submit"
            variant="default"
            size="xxl"
            disabled={isFetching || isError}
            onClick={() => setIsOrderCompleted(false)}
          >
            {isFetching ? (
              <>
                <Loader2 className="animate-spin" />
                Loading Rate...
              </>
            ) : (
              <>{isError ? "Failed" : "Swap"}</>
            )}
          </Button>
        </form>

        <SwapMicroInfo
          fromToken={selectedFrom.token}
          toToken={selectedTo.token}
          rootRate={exchangeRate?.data.rate}
          fee={0.25}
          fromAmount={fromAmount}
          toAmount={toAmount}
        />

        {/* --- MODALS --- */}
        <CryptoModal
          isOpen={isCryptoModalOpen}
          onClose={closeCryptoModal}
          selectNetwork={selectNetwork}
          selectedNetwork={selectedNetwork}
          onSelect={selectCrypto}
          selectedFrom={selectedFrom}
          selectedTo={selectedTo}
          setSelectedFrom={setSelectedFrom}
          setSelectedTo={setSelectedTo}
        />

        {isCryptoAddressModalOpen && (
          <CryptoAddressModal
            isOrderCompleted={isOrderCompleted}
            setIsOrderCompleted={setIsOrderCompleted}
            fromNetwork={selectedFrom}
            fromAmount={fromAmount}
            formData={formData}
            isOpen={isCryptoAddressModalOpen}
            setIsOpen={setCryptoAddressModalOpen}
            setIsStatusModalOpen={setStatusModalOpen}
            orderId={orderResponse?.data?.orderId}
            orderError={orderError}
          />
        )}

        {isStatusModalOpen && (
          <StatusModal
            isOrderCompleted={isOrderCompleted}
            orderId={orderResponse?.data?.orderId || ""}
            isOpen={isStatusModalOpen}
            onClose={closeStatusModal}
          />
        )}
      </Form>
    </div>
  );
}
