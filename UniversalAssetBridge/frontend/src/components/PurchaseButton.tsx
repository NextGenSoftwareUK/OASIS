"use client";

import Image from "next/image";
import Modal from "@/components/Modal";
import Loading from "@/components/Loading";
import CopyBtn from "@/components/CopyBtn";
import { useState } from "react";
import { Button, buttonVariants } from "@/components/ui/button";
import { useWalletStore } from "@/store/useWalletStore";
import { useBuyRwa } from "@/requests/rwa/buyRwa.request";
import { useSendSignedTransaction } from "@/requests/rwa/sendSignedTransaction.request";
import { shortAddress } from "@/utils/shortSomething";
// import { Transaction } from "@solana/web3.js"; // Temporarily disabled for trust focus

interface PurchaseButtonProps {
  tokenId: string;
  usageInMarketPage?: boolean;
}

export default function PurchaseButton({
  tokenId,
  usageInMarketPage,
}: PurchaseButtonProps) {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [status, setStatus] = useState<
    "idle" | "loading" | "success" | "error"
  >("idle");
  const [message, setMessage] = useState("");
  const [transactionId, setTransactionId] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  const { publicKey } = useWalletStore();
  const purchase = useBuyRwa();
  const submitTransaction = useSendSignedTransaction();

  const showMinimalModal =
    status === "loading" || status === "error" || status === "idle";

  const resetState = () => {
    setStatus("idle");
    setMessage("");
    setTransactionId("");
    setErrorMessage("");
  };

  const handlePurchase = async () => {
    resetState();
    setIsModalOpen(true);
    setStatus("loading");

    try {
      const provider = (window as any).solana;
      if (!provider || !provider.isPhantom) {
        throw new Error("Phantom wallet not found");
      }

      if (!publicKey) {
        throw new Error("Please connect your wallet firstly.");
      }

      await provider.connect();

      purchase.mutate(
        { rwaId: tokenId, buyerPubKey: publicKey },
        {
          onSuccess: async (res) => {
            setMessage(
              "Please check your Phantom wallet to confirm the transaction"
            );

            try {
              const txBase64 = res.data;
              // Temporarily disabled Solana transaction handling for trust focus
              // const transaction = Transaction.from(
              //   Buffer.from(txBase64, "base64")
              // );

              // Temporarily disabled Solana transaction signing for trust focus
              // const signedTransaction = await provider
              //   .signTransaction(transaction)
              //   .catch((err: any) => {
              //     throw new Error(
              //       err?.message === "User rejected the request."
              //         ? "Transaction signing was cancelled"
              //         : err?.message || "Failed to sign transaction"
              //     );
              //   });

              // Temporarily disabled Solana transaction submission for trust focus
              // const rawTx = signedTransaction.serialize();
              // const txBase64Signed = Buffer.from(rawTx).toString("base64");

              // submitTransaction.mutate(
              //   {
              //     transactionHash: txBase64,
              //     transactionSignature: txBase64Signed,
              //   },
              //   {
              //     onSuccess: (res) => {
              //       setTransactionId(res.data);
              //       setStatus("success");
              //     },
              //     onError: (err: any) => {
              //       throw new Error(
              //         err?.response?.data?.error?.message ||
              //           "Transaction submission failed"
              //       );
              //     },
              //   }
              // );
              
              // Temporary mock success for trust focus
              setTransactionId("mock-transaction-id");
              setStatus("success");
            } catch (err: any) {
              setErrorMessage(err.message || "Something went wrong");
              setStatus("error");
            }
          },
          onError: (error: any) => {
            setErrorMessage(
              error?.response?.data?.error?.message ||
                "Could not prepare transaction"
            );
            setStatus("error");
          },
        }
      );
    } catch (err: any) {
      setErrorMessage(
        err?.message === "User rejected the request."
          ? "Wallet connection was cancelled"
          : err?.message || "Something went wrong"
      );
      setStatus("error");
    }
  };

  return (
    <>
      <Button
        variant="green"
        size={usageInMarketPage ? "sm" : "lg"}
        onClick={handlePurchase}
      >
        Purchase
      </Button>

      {isModalOpen && (
        <Modal
          isNonClosable={status !== "error"}
          isNonUrlModal
          onCloseFunc={() => setIsModalOpen(false)}
          className={`${
            showMinimalModal ? "min-h-64 flex justify-center items-center" : ""
          } relative z-10000`}
        >
          <div className="flex flex-col items-center justify-center">
            {status === "loading" && (
              <>
                <Loading />
                {message && (
                  <p className="p text-black mt-6 text-center max-w-[80%]">
                    {message}
                  </p>
                )}
              </>
            )}

            {status === "success" && (
              <>
                <Image
                  src="/done.svg"
                  alt="Done"
                  width={100}
                  height={100}
                  className="mt-5 sm:w-20"
                />
                <h2 className="h2 my-5 text-black text-center">
                  You have successfully purchased your RWA
                </h2>
                <div className="flex gap-[5px] mb-[10px] w-full mt-5">
                  <div
                    className={`${buttonVariants({
                      variant: "empty",
                      size: "xl",
                    })} flex gap-2 bg-gray py-3 px-5 rounded-xl justify-between items-center flex-1 relative`}
                  >
                    <p className="sm:text-sm xxs:text-xs">
                      Your transaction ID:
                    </p>
                    <p>{shortAddress(transactionId)}</p>
                  </div>
                  <CopyBtn address={transactionId} />
                </div>
                <Button
                  variant="gray"
                  size="xl"
                  onClick={() => {
                    resetState();
                    setIsModalOpen(false);
                  }}
                  className="w-full mt-2"
                >
                  Done
                </Button>
              </>
            )}

            {status === "error" && (
              <p className="p text-black text-center max-w-sm whitespace-pre-line">
                {errorMessage ||
                  "Something went wrong. Please try again later."}
              </p>
            )}
          </div>
        </Modal>
      )}
    </>
  );
}
