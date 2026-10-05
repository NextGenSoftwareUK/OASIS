import { passwordRules } from "@/lib/helpers/passwordRules";
import { CircleCheckBig } from "lucide-react";

interface PasswordRuleProps {
  passwordField: string;
  className?: string;
}

export default function PasswordRules({
  passwordField,
  className,
}: PasswordRuleProps) {
  return (
    <div className={className}>
      <p className="p-sm">Password must contain:</p>
      <ul className="text-secondary p-sm mt-2">
        {passwordRules.map((rule, i) => {
          const passed = rule.check(passwordField);
          return (
            <li
              key={i}
              className={`flex items-center gap-1 ${
                passed ? "text-green-600" : "text-secondary"
              }`}
            >
              <CircleCheckBig
                size={16}
                color="green"
                className={`${passed ? "block" : "hidden"}`}
              />
              {rule.text}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
