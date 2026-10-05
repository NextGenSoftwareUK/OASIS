import Image from "next/image";
import Link from "next/link";

import "@/styles/modal.css";
import { X } from "lucide-react";

interface ModalProps {
  children: React.ReactNode;
  isNonUrlModal?: boolean;
  onCloseFunc?: () => void;
  isNonClosable?: boolean;
  className?: string;
}

export default function Modal({
  children,
  isNonUrlModal,
  onCloseFunc,
  isNonClosable,
  className,
}: ModalProps) {
  return (
    <div className="modal-backdrop sm:px-5 relative">
      <div className={`modal-content ${className}`}>
        {!isNonUrlModal && !isNonClosable && (
          <Link
            href="/"
            className="modal-close bg-muted rounded-full p-[5px] sm:top-3! sm:right-3!"
          >
            <X size={18} color="#000" />
          </Link>
        )}
        {isNonUrlModal && !isNonClosable && (
          <button
            type="button"
            className="modal-close bg-muted rounded-full p-[5px] sm:top-3! sm:right-3!"
            onClick={onCloseFunc}
          >
            <X size={18} color="#000" />
          </button>
        )}
        {children}
      </div>
    </div>
  );
}
