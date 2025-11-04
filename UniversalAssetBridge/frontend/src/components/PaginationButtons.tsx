import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import { getVisiblePages } from "@/utils/getVisiblePages.util";
import { ReadonlyURLSearchParams } from "next/navigation";

interface PaginationButtonsProps {
  className?: string;
  pages: number;
  currentPage: number;
  searchParams: ReadonlyURLSearchParams;
}

export function PaginationButtons({
  className,
  pages,
  currentPage,
  searchParams,
}: PaginationButtonsProps) {
  const visiblePages = getVisiblePages(currentPage, pages);

  const buildHref = (page: number) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("page", page.toString());
    return `?${params.toString()}`;
  };

  return (
    <Pagination className={className}>
      <PaginationContent>
        {currentPage > 1 && (
          <PaginationItem>
            <PaginationPrevious href={buildHref(currentPage - 1)} />
          </PaginationItem>
        )}

        {visiblePages.map((p, i) => (
          <PaginationItem key={i}>
            {p === "..." ? (
              <PaginationEllipsis />
            ) : (
              <PaginationLink href={buildHref(p)} isActive={p === currentPage}>
                {p}
              </PaginationLink>
            )}
          </PaginationItem>
        ))}

        {currentPage < pages && (
          <PaginationItem>
            <PaginationNext href={buildHref(currentPage + 1)} />
          </PaginationItem>
        )}
      </PaginationContent>
    </Pagination>
  );
}
