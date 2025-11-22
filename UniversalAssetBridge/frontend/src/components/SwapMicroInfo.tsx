"use client";

import SwapRoute from "@/components/SwapRoute";
import { Skeleton } from "@/components/ui/skeleton";
import { ArrowRightLeft, ChevronDown, Loader2 } from "lucide-react";
import { useEffect, useState } from "react";

interface SwapMicroInfoProps {
  fromToken: string;
  toToken: string;
  rootRate: number;
  fee: number;

  fromAmount: number;
  toAmount: number;
}

export default function SwapMicroInfo({
  fromToken,
  toToken,
  rootRate,
  fee,
  fromAmount,
  toAmount,
}: SwapMicroInfoProps) {
  const [isInfoOpen, setIsInfoOpen] = useState(false);
  const [isRouteOpen, setIsRouteOpen] = useState(false);

  const [rate, setRate] = useState(rootRate);
  const [tokensConsistency, setTokensConsistency] = useState([
    fromToken,
    toToken,
  ]);

  useEffect(() => {
    setTokensConsistency([fromToken, toToken]);
    setRate(rootRate);
  }, [rootRate, setTokensConsistency]);

  const switchRate = () => {
    setTokensConsistency([tokensConsistency[1], tokensConsistency[0]]);
    setRate(1 / rate);
  };

  const onRouteClose = () => setIsRouteOpen(false);

  return (
    <>
      <div className="flex justify-between mt-2">
        <div className="flex">
          <div className="p-sm flex items-center gap-1">
            <span className="opacity-60">Rate: </span>
            {!rate ? (
              <Skeleton className="h-5 w-20 ml-1 rounded-[8px] bg-primary" />
            ) : (
              <>
                {" "}
                1 {tokensConsistency[0]} = {rate} {tokensConsistency[1]}
                <ArrowRightLeft
                  onClick={switchRate}
                  className="ml-1 cursor-pointer transition-all hover:opacity-70"
                  size={16}
                />
              </>
            )}
          </div>
        </div>
        <div className="flex items-center gap-[10px]">
          <p className="p-sm ">
            <span className="opacity-60">Fee:</span> {fee}% ($12)
          </p>
          <ChevronDown
            onClick={() => setIsInfoOpen(!isInfoOpen)}
            className={`opacity-80 cursor-pointer transition-all hover:opacity-50 ${isInfoOpen && "rotate-180"}`}
            size={18}
          />
        </div>
      </div>
      <div
        className={`border-t border-primary/50 mt-2 pt-2 ${isInfoOpen ? "flex" : "hidden"}`}
      >
        <div className="flex justify-between w-full">
          <p className="p-sm opacity-60">Route:</p>
          <p
            onClick={() => setIsRouteOpen(!isRouteOpen)}
            className="p-sm border-b cursor-pointer"
          >
            Quantum Street
          </p>
        </div>
      </div>
      {isRouteOpen && (
        <SwapRoute
          fromToken={tokensConsistency[0]}
          toToken={tokensConsistency[1]}
          fromAmount={fromAmount}
          toAmount={toAmount}
          onClose={onRouteClose}
        />
      )}
    </>
  );
}
