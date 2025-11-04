import { Skeleton } from "@/components/ui/skeleton";

export default function ProfileSkeleton() {
  return (
    <div className="flex flex-col space-y-3">
      <Skeleton className="w-2/3 h-10 rounded-xl" />
      <div className="space-y-2">
        <Skeleton className="w-full h-20" />
      </div>
    </div>
  );
}
