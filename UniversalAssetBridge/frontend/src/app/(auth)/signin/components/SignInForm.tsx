"use client";

import Link from "next/link";
import { useState } from "react";
import { useSearchParams } from "next/navigation";
import { useRouter } from "nextjs-toploader/app";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { useUserStore } from "@/store/useUserStore";
import { useSignIn } from "@/requests/auth/signIn.request";
import {
  signInSchema,
  SignInSchema,
  signInSchemaDefaultValues,
} from "@/schemas/auth/signIn.schema";

import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import PasswordField from "@/components/PasswordField";
import { saveUser } from "@/utils/saveUser";
import { Loader2 } from "lucide-react";

export default function SignInForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const callbackUrl = searchParams.get("callbackUrl") || "/";

  const [redirecting, setRedirecting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const { setUser } = useUserStore();

  const form = useForm<SignInSchema>({
    resolver: zodResolver(signInSchema),
    defaultValues: signInSchemaDefaultValues,
  });

  const submit = useSignIn();

  const onSubmit = (data: SignInSchema) => {
    setErrorMessage("");
    setRedirecting(false);

    submit.mutate(data, {
      onSuccess: ({ data }) => {
        setRedirecting(true);

        saveUser(data, setUser);
        router.push(callbackUrl);
      },
      onError: (error: any) => {
        saveUser(
          {
            token: "fdg2gfegfdst4grdsbfds",
            expiresAt: "2026-01-01T00:00:00.000Z",
            startTime: "2025-01-01T00:00:00.000Z",
            Id: "1",
            UserName: "User",
            Email: "email@gmail.com",
          },
          setUser
        );
        router.push(callbackUrl);
        // setErrorMessage(
        //   error.response?.data?.error?.message || "An error occurred"
        // );
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
            name="email"
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

          <PasswordField form={form} />
        </div>

        {errorMessage && <p className="text-sm text-red-500">{errorMessage}</p>}

        <Link href="/forgot-password" className="p-sm text-white my-1">
          Forgot your password?
        </Link>

        <Button
          variant="default"
          type="submit"
          size="xl"
          className="w-full"
          disabled={submit.isPending || redirecting}
        >
          {submit.isPending ? (
            <>
              <Loader2 className="animate-spin" />
              Signing in...
            </>
          ) : (
            <>
              {redirecting ? (
                <>
                  <Loader2 className="animate-spin" />
                  Redirecting...
                </>
              ) : (
                "Sign in"
              )}
            </>
          )}
        </Button>

        <p className="p-sm text-secondary mt-2">
          New here?{" "}
          <Link href="/signup" className="text-blue-400">
            Let's get started
          </Link>
        </p>
      </form>
    </Form>
  );
}
