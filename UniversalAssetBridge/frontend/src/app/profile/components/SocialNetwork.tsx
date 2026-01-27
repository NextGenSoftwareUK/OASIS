import { Button } from "@/components/ui/button";
import { ArrowUpRight } from "lucide-react";
import Image from "next/image";

interface SocialProps {
  iconPath: string;
  socialNetwork: string;
  profile: string;
}

export default function SocialNetwork({
  iconPath,
  socialNetwork,
  profile,
}: SocialProps) {
  return (
    <div className="bg-muted p-5 rounded-md text-black min-w-[224px]">
      <div className="flex gap-[15px] items-center">
        <Image
          src={iconPath}
          alt={socialNetwork}
          width={30}
          height={30}
          priority
        />
        <p className="p">{socialNetwork}</p>
      </div>

      {profile && (
        <>
          <p className="p mt-2.5">{profile}</p>
          <p className="p-sm text-black/40 -mt-1">Connected</p>
        </>
      )}

      {profile ? (
        <Button className="mt-12.5" variant="outline" size="lg">
          Change
          <ArrowUpRight className="inline" size={30} />
        </Button>
      ) : (
        <Button className="mt-12.5" variant="default" size="lg">
          Link
        </Button>
      )}
    </div>
  );
}
