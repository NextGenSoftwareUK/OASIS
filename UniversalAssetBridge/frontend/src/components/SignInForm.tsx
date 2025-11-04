"use client";

import Link from "next/link";
import Cookies from "js-cookie";
import { useState } from "react";
import { useSearchParams } from "next/navigation";
import { useRouter } from "nextjs-toploader/app";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { useUserStore } from "@/store/useUserStore";
import { parseJwt } from "@/utils/parseJwt.util";
import { useSignIn } from "@/requests/auth/signIn.request";
import {
  signInSchema,
  SignInSchema,
  signInSchemaDefaultValues,
  signInSchemaFields,
} from "@/schemas/auth/signIn.schema";

import { Form, FormControl, FormField, FormItem, FormMessage } from "./ui/form";
import { Input } from "./ui/input";
import { Button } from "./ui/button";
import LoadingAlt from "./LoadingAlt";

export default function SignInForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const callbackUrl = searchParams.get("callbackUrl") || "/";

  const [errorMessage, setErrorMessage] = useState("");
  const { setUser } = useUserStore();

  const form = useForm<SignInSchema>({
    resolver: zodResolver(signInSchema),
    defaultValues: signInSchemaDefaultValues,
  });

  const submit = useSignIn();

  const onSubmit = (data: SignInSchema) => {
    setErrorMessage("");

    submit.mutate(data, {
      onSuccess: ({ data }) => {
        const { token, expiresAt, startTime } = data;
        const { Id, UserName, Email } = parseJwt(token);

        setUser({ token, expiresAt, startTime, Id, UserName, Email });

        Cookies.set("oasisToken", token, { expires: new Date(expiresAt) });
        router.push(callbackUrl);
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
        {signInSchemaFields.map((item) => (
          <FormField
            key={item.name}
            control={form.control}
            name={item.name as keyof SignInSchema}
            render={({ field }) => (
              <FormItem className="w-full">
                <FormControl>
                  <Input
                    placeholder={item.placeholder}
                    type={item.type}
                    {...field}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        ))}

        {errorMessage && <p className="text-sm text-red-500">{errorMessage}</p>}

        <Button variant="default" type="submit" size="xl" className="w-full">
          {submit.isPending ? "Signing in..." : "Sign In"}
        </Button>

        <Link href="/signup" className="text-sm text-blue-600">
          Create account
        </Link>

        {submit.isPending && <LoadingAlt />}
      </form>
    </Form>
  );
}
