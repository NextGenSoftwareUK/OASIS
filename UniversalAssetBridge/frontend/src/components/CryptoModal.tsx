"use client";

import { Dispatch, SetStateAction, useEffect, useMemo, useState } from "react";
import Image from "next/image";
import Modal from "@/components/Modal";
import CryptoItem from "@/components/CryptoItem";
import { SelectedCrypto, CryptoOption } from "@/types/crypto/crypto.type";
import { cryptoOptions, networkIcons } from "@/lib/cryptoOptions";
import { Input } from "@/components/ui/input";
import { Search } from "lucide-react";
import { Button } from "@/components/ui/button";

interface CryptoModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSelect: (crypto: SelectedCrypto) => void;
  selectedFrom: SelectedCrypto;
  selectedTo: SelectedCrypto;
  selectedNetwork: CryptoOption | null;
  selectNetwork: (network: CryptoOption) => void;
  setSelectedTo: Dispatch<SetStateAction<SelectedCrypto>>;
  setSelectedFrom: Dispatch<SetStateAction<SelectedCrypto>>;
}

export default function CryptoModal({
  isOpen,
  onClose,
  onSelect,
  selectedNetwork,
  selectNetwork,
}: CryptoModalProps) {
  const [search, setSearch] = useState("");

  // Convert cryptoOptions to network format
  const networks = useMemo(() => {
    return cryptoOptions.map(opt => ({
      id: opt.token,
      name: opt.network,
      tokens: [opt.token],
      description: opt.description
    }));
  }, []);

  useEffect(() => {
    if (networks.length && !selectedNetwork?.name) {
      selectNetwork(networks[0]);
    }
  }, [networks, selectedNetwork?.name, selectNetwork]);

  const selectedNetworkTokens = useMemo(() => {
    if (networks) {
      return (
        networks.find((n: CryptoOption) => n?.name === selectedNetwork?.name)
          ?.tokens || []
      );
    }
    return [];
  }, [networks, selectedNetwork]);

  const handleClick = (token: string) => {
    if (selectedNetwork) {
      onSelect({ network: selectedNetwork.name, token });
      onClose(); // Close modal after selection
    }
  };

  if (!isOpen) return null;

  return (
    <Modal isNonUrlModal onCloseFunc={onClose}>
      <h2 className="h2 mb-6 text-black">Choose a token</h2>

      <p className="p-sm text-primary mb-[10px]">Networks</p>
      <div className="flex flex-col justify-center h-full">
        <ul className="grid grid-cols-3 gap-3 mb-4">
          {networks.map((network: CryptoOption) => (
            <li key={network.id}>
              <CryptoItem
                image={networkIcons[network.name] || ""}
                crypto={network.name}
                className={`cursor-pointer transition-all ${
                  selectedNetwork?.name === network.name && "bg-secondary"
                }`}
                onClick={() => selectNetwork(network)}
              />
            </li>
          ))}
        </ul>

        <p className="p-sm text-primary mb-2 mt-5">
          Tokens in {selectedNetwork?.name}
        </p>
        <div className="flex gap-1 mb-2 h-[50px]">
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            icon={
              <Search color="var(--primary)" size={20} className="!w-5 !h-5" />
            }
            iconPosition="right"
            type="text"
            placeholder="Search for token"
          />
        </div>
        <div className="rounded-lg bg-muted p-5">
          <ul>
            {selectedNetworkTokens.filter((item: string) =>
              item.toLowerCase().includes(search.toLowerCase())
            ).length > 0 ? (
              selectedNetworkTokens
                .filter((item: string) =>
                  item.toLowerCase().includes(search.toLowerCase())
                )
                .map((token: string) => (
                  <li
                    key={token}
                    className="flex gap-3 items-center text-sm cursor-pointer text-primary"
                    onClick={() => handleClick(token)}
                  >
                    <Image
                      src={token === "ARB" ? `/${token}.png` : `/${token}.svg`}
                      alt={token}
                      width={18}
                      height={18}
                    />
                    <p className="p-sm">{token}</p>
                  </li>
                ))
            ) : (
              <p className="p-sm text-primary -my-[.5px]">
                No tokens available
              </p>
            )}
          </ul>
        </div>
      </div>
    </Modal>
  );
}
