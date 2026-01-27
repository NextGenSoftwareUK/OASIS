interface PageTitleProps {
  title: string;
  parentClassName?: string;
  className?: string;
}

export default function PageTitle({
  title,
  className,
  parentClassName,
}: PageTitleProps) {
  return (
    <div className={`mb-8 xs:mb-5 ${parentClassName}`}>
      <h2
        className={`text-white font-bold text-3xl md:text-3xl xs:text-2xl ${className}`}
      >
        {title}
      </h2>
    </div>
  );
}
