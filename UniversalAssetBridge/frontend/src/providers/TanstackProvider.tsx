"use client";

import Modal from "@/components/Modal";
import { removeUser } from "@/utils/removeUser.util";
import { useUserStore } from "@/store/useUserStore";
import {
  MutationCache,
  QueryCache,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import { useRouter } from "nextjs-toploader/app";
import { useState } from "react";

export default function TanstackProvider({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const setUser = useUserStore((state) => state.setUser);
  const router = useRouter();
  const [isServerError, setIsServerError] = useState(false);
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        refetchOnWindowFocus: false,
        refetchOnReconnect: false,
        refetchOnMount: true,
        gcTime: 0,
      },
    },
    queryCache: new QueryCache({
      onError: (err: any) => {
        setIsServerError(false);
        if (err?.response?.status === 401) {
          removeUser(setUser, router);
        } else if (err?.response?.status >= 500) {
          router.push("/");
          setIsServerError(true);
        }
      },
    }),
    mutationCache: new MutationCache({
      onError: (err: any) => {
        if (err?.response?.status === 401) {
          removeUser(setUser, router);
        }
      },
    }),
  });
  return (
    <QueryClientProvider client={queryClient}>
      {children}
      {isServerError && (
        <Modal
          onCloseFunc={() => setIsServerError(false)}
          isNonUrlModal
          className="min-h-64 flex justify-center items-center"
        >
          <p className="p text-center">
            Something went wrong on the server. <br /> Please try again later
          </p>
        </Modal>
      )}
    </QueryClientProvider>
  );
}
