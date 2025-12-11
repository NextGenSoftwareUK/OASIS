import { Dispatch, SetStateAction } from "react";

export type SetCopied = Dispatch<SetStateAction<boolean>>;
export type SetCopiedMap = Dispatch<SetStateAction<Record<string, boolean>>>;
