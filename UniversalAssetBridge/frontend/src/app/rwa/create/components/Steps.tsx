"use client";

import { MoveRight } from "lucide-react";

const steps = [
  {
    id: 1,
    width: "w-1/3",
    name: "Basic",
  },
  {
    id: 2,
    width: "w-2/3",
    name: "Addition",
  },
  {
    id: 3,
    width: "w-full",
    name: "Complete",
  },
];

export default function Steps() {
  const currentStep = 2;
  return (
    <ul className="space-y-2">
      {steps.map((step) => {
        return (
          <li
            key={step.id}
            className={`bg-primary text-white flex justify-end px-5 py-2 rounded-r-md relative overflow-hidden transition-all ${step.width}
            ${currentStep > step.id && "!bg-muted !text-black"}`}
          >
            {step.name}
            {currentStep === step.id && (
              <div className="absolute top-0 bottom-0 left-0 bg-muted z-10 w-2/5">
                <MoveRight
                  color="black"
                  size={25}
                  strokeWidth={1.5}
                  className="absolute top-1/2 right-5 -translate-y-1/2"
                />
              </div>
            )}
          </li>
        );
      })}
    </ul>
  );
}
