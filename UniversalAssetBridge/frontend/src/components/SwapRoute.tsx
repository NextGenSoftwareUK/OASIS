import Modal from "@/components/Modal";
import { shortDescription } from "@/utils/shortSomething";

interface SwapRouteProps {
  fromToken: string;
  toToken: string;
  fromAmount: number;
  toAmount: number;
  onClose: () => void;
}

export default function SwapRoute({
  fromToken,
  toToken,
  fromAmount,
  toAmount,
  onClose,
}: SwapRouteProps) {
  const topSection = [
    {
      label: "User system wallet",
      token: fromToken,
      amount: fromAmount,
    },
    {
      label: "User wallet",
      token: toToken,
      amount: toAmount,
    },
  ];

  const bottomSection = [
    { token: fromToken, label: "Quantum Street" },
    { token: toToken, label: "Quantum Street" },
  ];

  return (
    <Modal isNonUrlModal onCloseFunc={onClose}>
      <h2 className="h2 mb-6 text-black">Route</h2>

      <div className="bg-muted w-full py-10 px-5 rounded-md relative">
        {/* Top */}
        <div className="flex justify-between relative z-10">
          {topSection.map(({ label, token, amount }) => (
            <div key={label} className="flex flex-col items-center gap-0.5">
              <p className="p-sm text-primary text-center">{label}</p>
              <div className="flex gap-2 items-center bg-secondary rounded-sm text-black px-2.5 py-1.5">
                <img src={`/${token}.svg`} alt={token} className="h-4" />
                <p className="p-sm">
                  {shortDescription(amount, 7)} {token}
                </p>
              </div>
            </div>
          ))}
        </div>

        {/* Vertical Lines */}
        <div className="absolute top-[80px] left-1/6 w-[2px] h-[80px] bg-primary/40" />
        <div className="absolute top-[80px] right-1/6 w-[2px] h-[80px] bg-primary/40" />

        {/* Horizontal Lines */}
        <div className="absolute bottom-[79px] left-[77px] h-[2px] w-[200px] bg-primary/40" />
        <div className="absolute bottom-[79px] right-[77px] h-[2px] w-[200px] bg-primary/40" />

        {/* Bottom */}
        <div className="flex justify-evenly mt-12 relative z-10">
          {bottomSection.map(({ token, label }) => (
            <div key={token} className="flex flex-col items-center gap-0.5">
              <div className="flex gap-2 items-center bg-secondary rounded-sm text-black px-2.5 py-1.5">
                <img src={`/${token}.svg`} alt={token} className="h-4" />
                <p className="p-sm">{token}</p>
              </div>
              <p className="p-sm text-primary text-center">{label}</p>
            </div>
          ))}
        </div>
      </div>
    </Modal>
  );
}
