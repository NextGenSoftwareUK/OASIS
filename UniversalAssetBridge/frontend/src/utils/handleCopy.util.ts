import { SetCopied, SetCopiedMap } from "@/types/util/handleCopy.type";

export const handleCopy = async (
  value: string,
  options?: {
    setIsCopied?: SetCopied;
    setCopiedMap?: SetCopiedMap;
    key?: string;
  }
) => {
  try {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(value);
    } else {
      const textArea = document.createElement("textarea");
      textArea.value = value;
      textArea.style.position = "absolute";
      textArea.style.opacity = "0";
      document.body.appendChild(textArea);
      textArea.select();
      document.execCommand("copy");
      document.body.removeChild(textArea);
    }

    if (options?.setIsCopied) {
      options.setIsCopied(true);
      setTimeout(() => options.setIsCopied?.(false), 2000);
    }

    if (options?.setCopiedMap && options.key) {
      options.setCopiedMap((prev) => ({ ...prev, [options.key!]: true }));
      setTimeout(() => {
        options.setCopiedMap?.((prev) => ({ ...prev, [options.key!]: false }));
      }, 2000);
    }
  } catch (err) {
    console.error("Error copying text:", err);
  }
};
