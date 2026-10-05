"use client";

import Modal from "@/components/Modal";
import Loading from "@/components/Loading";
import CountdownTimer from "@/components/CountdownTimer";
import QRCodeDisplay from "@/components/QrCode";
import CopyBtn from "@/components/CopyBtn";
import { useEffect, useState } from "react";
import { shortAddress } from "@/utils/shortSomething";
import { VirtualAddressMesages } from "@/lib/helpers/virtualAddressMessages";
import { useUserStore } from "@/store/useUserStore";
import { useGetVirtualAccounts } from "@/requests/user/getVirtualAccounts.request";
import { useGetVirtualAccountBalance } from "@/requests/user/getVirtualAccountBalance.request";
import { buttonVariants } from "@/components/ui/button";
import { SelectedCrypto } from "@/types/crypto/crypto.type";
import { SwapFormData } from "@/types/crypto/swap.type";
import { Loader2, ShieldCheck } from "lucide-react";
import BlankLink from "@/components/BlankLink";
import { Skeleton } from "@/components/ui/skeleton";

interface StatusModalProps {
  isOpen: boolean;
  setIsOpen: (open: boolean) => void;
  setIsStatusModalOpen: (open: boolean) => void;
  fromNetwork: SelectedCrypto;
  fromAmount: number | null;
  formData: SwapFormData | null;
  orderId?: string;
  orderError: string;
  isOrderCompleted: boolean;
  setIsOrderCompleted: (value: boolean) => void;
}

export default function CryptoAddressModal({
  fromNetwork,
  fromAmount,
  formData,
  isOpen,
  setIsOpen,
  setIsStatusModalOpen,
  orderId,
  orderError,
  isOrderCompleted,
  setIsOrderCompleted,
}: StatusModalProps) {
  const { user } = useUserStore();
  const [address, setAddress] = useState("");
  const [timeLeft, setTimeLeft] = useState(600);
  const [message, setMessage] = useState("");
  const [showAddress, setShowAddress] = useState(false);
  const [isCanceled, setIsCanceled] = useState(false);

  const { data: accountData } = useGetVirtualAccounts(
    showAddress,
    user?.token || ""
  );
  const {
    data: balanceData,
    refetch: refetchBalance,
    isError: balanceError,
    isFetching: balanceFetching,
  } = useGetVirtualAccountBalance(orderId!, isOpen);

  useEffect(() => {
    if (formData) {
      const found = accountData?.data.find(
        (item: { token: string; address: string }) =>
          item.token === formData.fromToken
      );
      if (found) setAddress(found.address);
    }
  }, [accountData]);

  useEffect(() => {
    if (!balanceData) return;

    const status = balanceData.data.status;

    if (status === "InsufficientFunds") {
      setShowAddress(true);
      setIsStatusModalOpen(false);
    } else if (status === "Expired") {
      setIsCanceled(true);
      setTimeLeft(0);
    } else if (status === "Pending" || status === "Completed") {
      setShowAddress(false);
      setIsOpen(false);
      setIsStatusModalOpen(true);
    }
  }, [balanceData]);

  useEffect(() => {
    if (!isOpen || !showAddress || isCanceled) return;
    const interval = setInterval(() => {
      if (!balanceFetching && !balanceError) {
        refetchBalance();
      }
    }, 10000);
    return () => clearInterval(interval);
  }, [showAddress, isOpen, isCanceled]);

  useEffect(() => {
    if (isOrderCompleted) {
      setAddress("");
      setShowAddress(false);
    }
  }, [isOrderCompleted]);

  useEffect(() => {
    const interval = 120;
    const index = Math.floor((600 - timeLeft) / interval);
    if (index < VirtualAddressMesages.length && timeLeft < 600 - interval) {
      setMessage(VirtualAddressMesages[index]);
    }
  }, [timeLeft]);

  const handleClose = () => {
    setAddress("");
    setShowAddress(false);
    setIsOpen(false);
    setIsOrderCompleted(true);
  };

  if (!isOpen) return null;

  const isLoading = !showAddress && !balanceError && !orderError;
  const isExpired = !timeLeft && isCanceled;
  const isError = orderError || balanceError;
  const isReady = showAddress && !!timeLeft && !balanceError;

  return (
    <Modal
      isNonUrlModal
      isNonClosable={isLoading}
      onCloseFunc={handleClose}
      className={`${isLoading || isExpired || isError ? "min-h-64 flex flex-col justify-center items-center" : ""} text-black`}
    >
      {isLoading && (
        // <Loading className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2" />
        <>
          <Loader2 size={80} strokeWidth={3} className="animate-spin" />
          <h3 className="h3 mt-5">Creating the order</h3>
        </>
      )}

      {isExpired && (
        <>
          <div className="items-center absolute bg-secondary text-black py-1 text-center rounded-full w-14">
            <div className="p-sm">
              <CountdownTimer timeLeft={timeLeft} setTimeLeft={setTimeLeft} />
            </div>
          </div>

          <p className="p text-center absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2">
            Your order has been closed due to insufficient balance.
          </p>
        </>
      )}

      {isError && (
        <p className="p text-center absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2">
          {orderError || "Something went wrong. Please try again later."}
        </p>
      )}

      {isReady && (
        <>
          <div className="items-center absolute bg-secondary text-black py-1 text-center rounded-full w-14">
            <div className="p-sm">
              <CountdownTimer timeLeft={timeLeft} setTimeLeft={setTimeLeft} />
            </div>
          </div>

          <div className="text-center">
            <h2 className="text-2xl font-semibold">Send funds</h2>
            <p className="p mt-4 max-w-[290px] mx-auto leading-4">
              To complete the swap, please deposit{" "}
              <span className="font-bold text-xl">{fromAmount}</span>{" "}
              {fromNetwork.token}s into your account{" "}
            </p>

            <p className="p-sm text-primary mt-4 mb-1">
              Scan the QR code with your wallet to send funds
            </p>

            <div className="w-[264px] h-[264px] mx-auto">
              {address ? (
                <QRCodeDisplay text={address} />
              ) : (
                <Skeleton className="w-[244px] h-[244px] bg-primary/80 rounded-md mx-auto my-3" />
              )}
            </div>

            {message && <p className="text-primary p-sm mb-4">{message}</p>}

            <div className="relative flex justify-center mt-1">
              <p className="p-sm text-primary px-1 relative z-10 bg-white">
                or enter the address manually
              </p>
              <div className="absolute w-full bg-primary/40 h-[.5px] top-1/2 -translate-y-1/2 mt-[1px]"></div>
            </div>

            <div className="flex gap-[5px] mt-[15px] items-center justify-center">
              <div
                className={`${buttonVariants({
                  variant: "empty",
                  size: "xl",
                })} flex gap-2 bg-gray py-3 !px-5 !justify-between flex-1 relative`}
              >
                <p>
                  Your {fromNetwork.token} address ({shortAddress(address)})
                </p>
              </div>
              <CopyBtn address={address} />
            </div>
          </div>

          <div className="flex gap-2 items-center mt-[10px]">
            <ShieldCheck
              strokeWidth={1.5}
              color="#000"
              size={55}
              className="h-7.5"
            />
            <p className="p-sm text-primary">
              Don’t worry — your funds stay safe and won’t be lost. They will
              stay in your account. You can find it in your{" "}
              <BlankLink
                href="/profile"
                className="text-blue-700"
                size={18}
                color="var(--color-blue-700)"
              >
                profile
              </BlankLink>
            </p>
          </div>
        </>
      )}
    </Modal>
  );
}
