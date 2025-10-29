import { ReactNode } from "react";
import { cn } from "@/lib/utils";

export type TableColumn<T> = {
  key: string;
  header: string;
  render: (item: T) => ReactNode;
  align?: "left" | "center" | "right";
  sortable?: boolean;
};

export type TableProps<T> = {
  columns: TableColumn<T>[];
  data: T[];
  keyExtractor: (item: T, index: number) => string | number;
  onRowClick?: (item: T) => void;
  isLoading?: boolean;
  emptyMessage?: string;
  className?: string;
};

export function Table<T>({ 
  columns, 
  data, 
  keyExtractor, 
  onRowClick,
  isLoading = false,
  emptyMessage = "No data available",
  className 
}: TableProps<T>) {
  const alignmentClasses = {
    left: "text-left",
    center: "text-center",
    right: "text-right",
  };

  if (isLoading) {
    return (
      <div className={cn("rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(6,11,26,0.7)] p-12", className)}>
        <div className="flex flex-col items-center justify-center gap-3">
          <div className="h-8 w-8 animate-spin rounded-full border-2 border-[var(--accent)] border-t-transparent" />
          <p className="text-sm text-[var(--muted)]">Loading...</p>
        </div>
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div className={cn("rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(6,11,26,0.7)] p-12", className)}>
        <p className="text-center text-sm text-[var(--muted)]">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className={cn("rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(6,11,26,0.7)] overflow-hidden", className)}>
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="border-b border-[var(--color-card-border)]/30 bg-[rgba(5,5,16,0.5)]">
            <tr>
              {columns.map((column) => (
                <th
                  key={column.key}
                  className={cn(
                    "px-4 py-3 text-xs uppercase tracking-wide text-[var(--muted)] font-semibold",
                    alignmentClasses[column.align || "left"]
                  )}
                >
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.map((item, index) => (
              <tr
                key={keyExtractor(item, index)}
                onClick={() => onRowClick?.(item)}
                className={cn(
                  "border-b border-[var(--color-card-border)]/20 transition",
                  onRowClick && "cursor-pointer hover:bg-[rgba(34,211,238,0.05)]",
                  index === data.length - 1 && "border-b-0"
                )}
              >
                {columns.map((column) => (
                  <td
                    key={column.key}
                    className={cn(
                      "px-4 py-3 text-sm",
                      alignmentClasses[column.align || "left"]
                    )}
                  >
                    {column.render(item)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

