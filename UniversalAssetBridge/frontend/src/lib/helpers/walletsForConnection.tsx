export const walletsForConnection = [
  {
    id: 1,
    walletName: "Phantom",
    img: "/phantom.svg",
    detectingFn: () => {
      if ("phantom" in window) {
        const provider = (window as any).phantom?.solana;
        return provider?.isPhantom ? "Detected" : "Undetected";
      } else {
        return "Undetected";
      }
    }
  }
];