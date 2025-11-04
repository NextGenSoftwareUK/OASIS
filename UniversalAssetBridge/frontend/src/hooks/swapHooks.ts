import { useState } from "react";
import { defaultSelectedFrom, defaultSelectedTo } from "@/lib/cryptoOptions";
import { CryptoOption, SelectedCrypto } from "@/types/crypto/crypto.type";
import { SwapTarget } from "@/types/crypto/swap.type";

export function useSwap() {
  const [selectedNetwork, setSelectedNetwork] = useState<CryptoOption | null>(
    null
  );
  const [selectedFrom, setSelectedFrom] =
    useState<SelectedCrypto>(defaultSelectedFrom);
  const [selectedTo, setSelectedTo] =
    useState<SelectedCrypto>(defaultSelectedTo);

  const [isCryptoModalOpen, setIsCryptoModalOpen] = useState(false);
  const [isStatusModalOpen, setIsStatusModalOpen] = useState(false);
  const [statuses, setStatuses] = useState<string[]>([]);
  const [currentTarget, setCurrentTarget] = useState<SwapTarget | null>(null);

  const openCryptoModal = (target: SwapTarget) => {
    setCurrentTarget(target);
    setIsCryptoModalOpen(true);
  };

  const closeCryptoModal = () => {
    setIsCryptoModalOpen(false);
    setCurrentTarget(null);
  };

  const selectNetwork = (network: CryptoOption) => {
    setSelectedNetwork(network);
  };

  const selectCrypto = (crypto: SelectedCrypto) => {
    if (currentTarget === "from") {
      setSelectedFrom(crypto);
    } else if (currentTarget === "to") {
      setSelectedTo(crypto);
    }
    closeCryptoModal();
  };

  const simulateStatuses = () => {
    const steps = [
      "Connecting to network...",
      "Validating transaction...",
      "Processing swap...",
      "Swap completed!",
    ];
    setStatuses([]);
    steps.forEach((step, index) => {
      setTimeout(() => {
        setStatuses((prev) => [...prev, step]);
      }, index * 1500);
    });
  };

  const closeStatusModal = () => {
    setIsStatusModalOpen(false);
    setStatuses([]);
  };

  return {
    selectedNetwork,
    selectNetwork,
    selectedFrom,
    setSelectedFrom,
    selectedTo,
    setSelectedTo,
    isCryptoModalOpen,
    openCryptoModal,
    closeCryptoModal,
    selectCrypto,
    isStatusModalOpen,
    setIsStatusModalOpen,
    statuses,
    simulateStatuses,
    closeStatusModal,
  };
}
