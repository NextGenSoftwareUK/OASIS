import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatAddress(address: string, length: number = 8): string {
  if (!address) return "";
  if (address.length <= length * 2) return address;
  return `${address.slice(0, length)}...${address.slice(-length)}`;
}

export function formatBalance(balance: number, decimals: number = 6): string {
  return new Intl.NumberFormat("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: decimals,
  }).format(balance);
}

export function formatCurrency(amount: number, currency: string = "USD"): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency,
  }).format(amount);
}

export function getProviderColor(provider: string): string {
  const colors: Record<string, string> = {
    ethereum: "bg-wallet-ethereum",
    solana: "bg-wallet-solana",
    polygon: "bg-wallet-polygon",
    arbitrum: "bg-wallet-arbitrum",
    bitcoin: "bg-wallet-bitcoin",
    starknet: "bg-[#8C7BFF]",
    zcash: "bg-[#00ff88]",
    aztec: "bg-[#60a5fa]",
    miden: "bg-[#8b5cf6]",
  };
  return colors[provider.toLowerCase()] || "bg-gray-500";
}

export function getProviderIcon(provider: string): string {
  const icons: Record<string, string> = {
    ethereum: "🔷",
    solana: "☀️",
    polygon: "💜",
    arbitrum: "🔵",
    bitcoin: "₿",
    starknet: "⚡",
    zcash: "🛡️",
    aztec: "🔐",
    miden: "✨",
  };
  return icons[provider.toLowerCase()] || "🔗";
}

export function validateAddress(address: string): boolean {
  // Basic validation - can be enhanced for specific chains
  return address.length > 0 && address.length <= 100;
}

export function validateAmount(amount: string): boolean {
  const num = parseFloat(amount);
  return !isNaN(num) && num > 0;
}

export function generateWalletId(): string {
  return Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15);
}

export function copyToClipboard(text: string): Promise<void> {
  return navigator.clipboard.writeText(text);
}

export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout: NodeJS.Timeout;
  return (...args: Parameters<T>) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  };
} 