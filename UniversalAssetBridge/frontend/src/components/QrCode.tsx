"use client";

import { useEffect, useState } from "react";
import QRCode from "qrcode";

export default function QRCodeDisplay({ text }: { text: string }) {
  const [url, setUrl] = useState("");

  useEffect(() => {
    QRCode.toDataURL(text).then(setUrl).catch(console.error);
  }, [text]);

  if (!url) return null;

  return (
    <div className="flex flex-col items-center">
      <img src={url} alt="QR Code" width={264} height={264} />
    </div>
  );
}
