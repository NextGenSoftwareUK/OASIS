import { buttonVariants } from "@/components/ui/button";

interface InfoRowProps {
  label: string;
  value: React.ReactNode;
}

export default function InfoRow({ label, value }: InfoRowProps) {
  return (
    <div
      className={`${buttonVariants({
        variant: "gray",
        size: "lg",
      })} px-5! w-full flex justify-between flex-wrap`}
    >
      <span className="text-gray-500">{label}:</span>
      {value}
    </div>
  );
}
