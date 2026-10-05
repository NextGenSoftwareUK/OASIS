"use client";

import Link from "next/link";
import { useState } from "react";
import { useSearchParams } from "next/navigation";
import { useRouter } from "nextjs-toploader/app";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { useUserStore } from "@/store/useUserStore";

import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import PasswordField from "@/components/PasswordField";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { useSignUp } from "@/requests/auth/signUp.request";
import {
  signUpSchema,
  SignUpSchema,
  signUpSchemaDefaultValues,
} from "@/schemas/auth/signUp.schema";
import { useSignIn } from "@/requests/auth/signIn.request";
import { Loader2 } from "lucide-react";
import { saveUser } from "@/utils/saveUser";

export default function SignUpForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const callbackUrl = searchParams.get("callbackUrl") || "/";

  const [redirecting, setRedirecting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const { setUser } = useUserStore();

  const form = useForm<SignUpSchema>({
    resolver: zodResolver(signUpSchema),
    defaultValues: signUpSchemaDefaultValues,
    mode: "onBlur",
  });

  const submit = useSignUp();
  const submitSignIn = useSignIn();

  const onSubmit = (data: SignUpSchema) => {
    setErrorMessage("");
    setRedirecting(false);

    submit.mutate(data, {
      onSuccess: () => {
        setRedirecting(true);

        submitSignIn.mutate(
          {
            email: data.emailAddress,
            password: data.password,
          },
          {
            onSuccess: ({ res }) => {
              saveUser(res, setUser);
              router.push(callbackUrl);
            },
            onError: (error: any) => {
              setErrorMessage(
                error.response?.data?.error?.message || "An error occurred"
              );
            },
          }
        );
      },
      onError: (error: any) => {
        setErrorMessage(
          error.response?.data?.error?.message || "An error occurred"
        );
      },
    });
  };

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className="flex flex-col items-center gap-2 w-full"
      >
        <div className="flex flex-col gap-[14px] w-full">
          <FormField
            control={form.control}
            name="userName"
            render={({ field }) => (
              <FormItem className="w-full">
                <FormLabel>Username</FormLabel>
                <FormControl>
                  <Input
                    placeholder="john_doe"
                    type="text"
                    // isDark={true}
                    {...field}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="emailAddress"
            render={({ field }) => (
              <FormItem className="w-full">
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input
                    placeholder="example@example.com"
                    type="email"
                    // isDark={true}
                    {...field}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <PasswordField withRules form={form} />

          <FormField
            control={form.control}
            name="confirmPassword"
            render={({ field }) => (
              <FormItem className="w-full">
                <FormLabel>Confirm Password</FormLabel>
                <FormControl>
                  <Input
                    placeholder="Password"
                    type="password"
                    // isDark={true}
                    {...field}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {errorMessage && <p className="text-sm text-red-500">{errorMessage}</p>}

        <Button
          type="submit"
          variant="default"
          size="xl"
          className="w-full mt-[12px]"
          disabled={submitSignIn.isPending || submit.isPending || redirecting}
        >
          {submit.isPending ? (
            <>
              <Loader2 className="animate-spin" />
              Signing up...
            </>
          ) : (
            <>
              {redirecting ? (
                <>
                  <Loader2 className="animate-spin" />
                  Redirecting...
                </>
              ) : (
                "Sign up"
              )}
            </>
          )}
        </Button>

        <p className="p-sm text-secondary mt-2">
          Allready have an account?{" "}
          <Link href="/signin" className="text-blue-400">
            Sign in
          </Link>
        </p>
      </form>
    </Form>
  );
}
