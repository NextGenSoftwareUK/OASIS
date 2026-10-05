import ProfileSidebar from "@/app/profile/components/ProfileSidebar";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Profile",
};

export default function ProfileLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <div className="w-full min-h-screen bg-background-website mt-20">
      <div className="max-w-[1300px] min-h-screen mx-auto w-full flex gap-12 md:px-5">
        <div className="w-[240px]">
          <ProfileSidebar />
        </div>

        <div className="max-w-[1010px] w-full">{children}</div>
      </div>
    </div>
  );
}
