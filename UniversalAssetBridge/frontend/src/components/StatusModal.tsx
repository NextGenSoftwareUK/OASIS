"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import Modal from "@/components/Modal";
import Loading from "@/components/Loading";
import CopyBtn from "@/components/CopyBtn";
import { shortAddress } from "@/utils/shortSomething";
import { statusMessages } from "@/lib/helpers/statusMessages";
import { useGetVirtualAccountBalance } from "@/requests/user/getVirtualAccountBalance.request";
import { Button, buttonVariants } from "@/components/ui/button";
import Link from "next/link";
import { SOLANA_ENVIRONMENT } from "@/lib/constants";
import { ArrowUpRight, Loader2, ShieldCheck } from "lucide-react";
import BlankLink from "@/components/BlankLink";

interface StatusModalProps {
  orderId: string;
  isOpen: boolean;
  onClose: () => void;
  isOrderCompleted: boolean;
}

export default function StatusModal({
  orderId,
  isOpen,
  onClose,
  isOrderCompleted,
}: StatusModalProps) {
  const [messageIndex, setMessageIndex] = useState(0);
  const [txId, setTxId] = useState("");

  const { data, refetch } = useGetVirtualAccountBalance(orderId!, !txId);
  const isCompleted = data?.data.status === "Completed";

  useEffect(() => {
    if (isCompleted) setTxId(data.data.transactionId);
    console.log("isCompeted", isCompleted);
    console.log("txId", data?.data.transactionId);
  }, [isCompleted, data, data?.data.transactionId]);

  useEffect(() => {
    if (isOrderCompleted) setTxId("");
  }, [isOrderCompleted]);

  useEffect(() => {
    if (!orderId || isCompleted || txId) return;
    const interval = setInterval(refetch, 10000);
    return () => clearInterval(interval);
  }, [orderId, isCompleted, txId]);

  useEffect(() => {
    if (!isOpen || isCompleted) return;
    const interval = setInterval(() => {
      setMessageIndex((i) => (i + 1) % statusMessages.length);
    }, 5000);
    return () => clearInterval(interval);
  }, [isOpen, isCompleted]);

  if (!isOpen) return null;

  const renderLoading = () => (
    <div className="flex flex-col items-center justify-center py-10">
      <Loader2 strokeWidth={3} size={80} className="animate-spin" />
      <h2 className="h2 text-center mt-5">{statusMessages[messageIndex]}</h2>
    </div>
  );

  const renderSuccess = () => (
    <>
      <div className="p-5 bg-muted rounded-md mt-5">
        <Image
          src="/done.svg"
          alt="Done"
          width={45}
          height={45}
          className="sm:w-20"
        />
      </div>
      <h2 className="text-[22px] mt-6 block text-center font-bold">
        Completed
      </h2>
      <p className="p mt-1 block text-center text-primary">
        Your transaction was successful
      </p>

      <div className="flex gap-[5px] mb-[10px] w-full mt-5">
        <div
          className={`${buttonVariants({
            variant: "empty",
            size: "xl",
          })} flex gap-2 bg-gray py-3 !px-5 !justify-between flex-1 relative`}
        >
          <p className="">Your transaction ID ({shortAddress(txId)})</p>
        </div>
        <CopyBtn address={txId} />
      </div>

      <Link
        href={`https://explorer.solana.com/address/${txId}${
          SOLANA_ENVIRONMENT === "devnet" ? "?cluster=devnet" : ""
        }`}
        target="_blank"
        className={`${buttonVariants({ variant: "muted", size: "xl" })} mb-[10px] w-full relative`}
      >
        Check in Solana Explorer
        <ArrowUpRight
          className="absolute right-3 top-1/2 -translate-y-1/2 !w-10 !h-10"
          strokeWidth={1}
          size={30}
          color="var(--primary)"
        />
      </Link>

      <Button
        variant="default"
        size="xl"
        onClick={() => {
          onClose();
          setTxId("");
        }}
        className="w-full"
      >
        Done
      </Button>

      <div className="flex gap-2 items-center mt-[10px]">
        <ShieldCheck
          strokeWidth={1.5}
          color="#000"
          size={55}
          className="h-7.5"
        />
        <p className="p-sm text-primary">
          If there are any left funds in your account. You can find them by
          checking your account balance in your{" "}
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
  );

  return (
    <Modal isNonClosable className="grid text-black">
      <div className="flex flex-col items-center justify-center">
        {!txId ? renderLoading() : renderSuccess()}
      </div>
    </Modal>
  );
}
