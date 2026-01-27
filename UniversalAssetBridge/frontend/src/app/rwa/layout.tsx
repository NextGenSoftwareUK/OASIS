import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Rwa Market",
};

export default function RwaMarketLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <div className="w-full min-h-screen bg-background-website">
      <div className="max-w-[1300px] min-h-screen mx-auto w-full xl:px-5 pb-5">
        {children}
      </div>
    </div>
  );
}
