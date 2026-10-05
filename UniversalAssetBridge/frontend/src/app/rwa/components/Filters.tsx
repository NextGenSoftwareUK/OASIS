"use client";

import FiltersForm from "./FiltersForm";
import { Dispatch, SetStateAction, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { Funnel, X } from "lucide-react";
import { RwaFiltersParams } from "@/types/rwa/rwa.type";

interface FiltersProps {
  setReqParams: Dispatch<SetStateAction<RwaFiltersParams>>;
}

export default function Filters({ setReqParams }: FiltersProps) {
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  return (
    <>
      <div className="w-[300px]">
        <FiltersForm setReqParams={setReqParams} />
      </div>
    </>
  );
}
