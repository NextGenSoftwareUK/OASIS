import "@/styles/loading.css";

interface LoadingAltProps {
  className?: string;
}

export default function LoadingAlt({ className }: LoadingAltProps) {
  return (
    <div
      style={{ zIndex: 100000000 }}
      className={`absolute top-0 left-0 right-0 bottom-0 bg-black/25 ${className}`}
    >
      <div className="loading-spinner-alt z-50"></div>
    </div>
  );
}
