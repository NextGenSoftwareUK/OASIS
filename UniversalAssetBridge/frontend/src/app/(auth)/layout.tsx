import Link from "next/link";

export default function ProfileLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <div className="w-full min-h-screen bg-background-website">
      <div className="max-w-[1300px] min-h-screen mx-auto w-full md:px-5 flex flex-col justify-between">
        <header className="pt-6">
          <Link href="/" className="font-semibold">
            Quantum Street Bridge
          </Link>
        </header>
        {children}
        <div className=""></div>
      </div>
    </div>
  );
}
