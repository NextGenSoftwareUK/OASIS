import "@/styles/loading.css";

interface LoadingProps {
  className?: string;
  classNameLoading?: string;
  style?: React.CSSProperties;
}

export default function Loading({
  className,
  classNameLoading,
  style,
}: LoadingProps) {
  return (
    <div className={className}>
      <div
        style={style}
        className={`loading-spinner ${classNameLoading}`}
      ></div>
    </div>
  );
}
