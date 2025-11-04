import { Suspense } from "react";
import SignUpForm from "@/app/(auth)/signup/components/SignUpForm";

export default function page() {
  return (
    <div className="max-w-[380px] w-full self-center">
      <h1 className="h1 mb-2">Get started</h1>
      <p className="p-sm text-secondary mb-7">
        Lorem ipsum dolor sit amet consectetur. Element sit curabitur habitant
        at quam.
      </p>
      <div className="mb-12">
        <Suspense fallback={<div>Loading...</div>}>
          <SignUpForm />
        </Suspense>
      </div>
    </div>
  );
}
