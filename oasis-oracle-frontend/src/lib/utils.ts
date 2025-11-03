import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

/**
 * Merge Tailwind CSS classes with clsx
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Format large numbers with abbreviations (K, M, B)
 */
export function formatNumber(num: number, decimals: number = 2): string {
  if (num >= 1_000_000_000) {
    return (num / 1_000_000_000).toFixed(decimals) + "B";
  }
  if (num >= 1_000_000) {
    return (num / 1_000_000).toFixed(decimals) + "M";
  }
  if (num >= 1_000) {
    return (num / 1_000).toFixed(decimals) + "K";
  }
  return num.toFixed(decimals);
}

/**
 * Format currency with proper decimals
 */
export function formatCurrency(amount: number, currency: string = "USD"): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: amount < 1 ? 6 : 2,
  }).format(amount);
}

/**
 * Format percentage
 */
export function formatPercentage(value: number, decimals: number = 2): string {
  return `${value >= 0 ? "+" : ""}${value.toFixed(decimals)}%`;
}

/**
 * Truncate address (blockchain address)
 */
export function truncateAddress(address: string, startChars: number = 6, endChars: number = 4): string {
  if (address.length <= startChars + endChars) {
    return address;
  }
  return `${address.slice(0, startChars)}...${address.slice(-endChars)}`;
}

/**
 * Format time ago
 */
export function timeAgo(date: Date | number): string {
  const now = Date.now();
  const timestamp = typeof date === "number" ? date : date.getTime();
  const seconds = Math.floor((now - timestamp) / 1000);

  if (seconds < 60) return `${seconds}s ago`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
  return `${Math.floor(seconds / 86400)}d ago`;
}

/**
 * Get status color based on value
 */
export function getStatusColor(status: "success" | "warning" | "danger" | "default"): string {
  const colors = {
    success: "var(--color-positive)",
    warning: "var(--color-warning)",
    danger: "var(--negative)",
    default: "var(--color-muted)",
  };
  return colors[status];
}





