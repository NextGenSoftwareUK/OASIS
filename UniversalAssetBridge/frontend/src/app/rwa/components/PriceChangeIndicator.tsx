import { ChevronUp, ChevronDown } from "lucide-react";

interface PriceChangeIndicatorProps {
  price: number;
  oldPrice?: number;
}

export function PriceChangeIndicator({
  price,
  oldPrice,
}: PriceChangeIndicatorProps) {
  if (!oldPrice) {
    return <p className="p opacity-60">---</p>;
  }

  const diff = ((price - oldPrice) / oldPrice) * 100;
  const isPositive = diff > 0;
  const isNeutral = diff === 0;
  const percentage = `${Math.abs(diff).toFixed(2)}%`;

  return (
    <span
      className={`flex items-center text-xs justify-end ${
        isPositive
          ? "text-green-500"
          : isNeutral
            ? "text-text-gray"
            : "text-red-600"
      }`}
    >
      {isPositive && <ChevronUp size={15} className="inline" />}
      {!isPositive && !isNeutral && (
        <ChevronDown size={15} className="inline" />
      )}
      {percentage}
    </span>
  );
}
