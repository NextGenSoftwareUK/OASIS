"use client";

import Link from "next/link";
import { useState } from "react";
import { useRouter } from "nextjs-toploader/app";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import {
  signUpSchema,
  SignUpSchema,
  signUpSchemaDefaultValues,
  signUpSchemaFields,
} from "@/schemas/auth/signUp.schema";
import { useSignUp } from "@/requests/auth/signUp.request";

import { Form, FormControl, FormField, FormItem, FormMessage } from "./ui/form";
import { Input } from "./ui/input";
import { Button } from "./ui/button";
import LoadingAlt from "./LoadingAlt";

export default function SignUpForm() {
  const router = useRouter();
  const [successMessage, setSuccessMessage] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const form = useForm<SignUpSchema>({
    resolver: zodResolver(signUpSchema),
    defaultValues: signUpSchemaDefaultValues,
  });

  const submit = useSignUp();

  const onSubmit = (data: SignUpSchema) => {
    setErrorMessage("");
    setSuccessMessage(false);

    submit.mutate(data, {
      onSuccess: ({ data }) => {
        if (data?.userId) {
          setSuccessMessage(true);
          setTimeout(() => {
            router.push("/signin");
          }, 3000);
        }
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
        {signUpSchemaFields.map((item) => (
          <FormField
            key={item.name}
            control={form.control}
            name={item.name as keyof SignUpSchema}
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

        {errorMessage && (
          <p className="text-sm text-red-500 text-center">{errorMessage}</p>
        )}

        {successMessage && (
          <p className="text-sm text-green-600 text-center">
            Your account has been successfully created.
            <br /> Redirecting to sign in...
          </p>
        )}

        <Button
          type="submit"
          variant="default"
          size="xl"
          className="w-full"
          disabled={successMessage}
        >
          {submit.isPending
            ? "Signing up..."
            : successMessage
              ? "Redirecting..."
              : "Sign up"}
        </Button>

        <Link href="/signin" className="text-sm text-blue-600">
          Already have an account?
        </Link>

        {submit.isPending && <LoadingAlt />}
      </form>
    </Form>
  );
}
