"use cleint";

import PasswordRules from "@/components/PasswordRules";
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { passwordRules } from "@/lib/helpers/passwordRules";
import { CircleCheckBig, Eye, EyeOff } from "lucide-react";
import { useState } from "react";
import { UseFormReturn } from "react-hook-form";

interface PasswordFieldProps {
  form: UseFormReturn<any>;
  withRules?: boolean;
}

export default function PasswordField({
  form,
  withRules = false,
}: PasswordFieldProps) {
  const [showPassword, setShowPassword] = useState(false);

  const password = form.watch("password");

  return (
    <>
      <FormField
        control={form.control}
        name="password"
        render={({ field }) => (
          <FormItem className="w-full">
            <FormLabel>Password</FormLabel>
            <FormControl>
              <Input
                iconPosition="right"
                icon={
                  <>
                    {showPassword ? (
                      <Eye
                        size={20}
                        color="var(--primary)"
                        className="cursor-pointer"
                        onClick={() => setShowPassword(!showPassword)}
                      />
                    ) : (
                      <EyeOff
                        size={20}
                        color="var(--primary)"
                        className="cursor-pointer"
                        onClick={() => setShowPassword(!showPassword)}
                      />
                    )}
                  </>
                }
                placeholder="Password"
                type={showPassword ? "text" : "password"}
                // isDark={true}
                {...field}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      {withRules && <PasswordRules passwordField={password} />}
    </>
  );
}
