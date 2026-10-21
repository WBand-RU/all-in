"use client";

import { useEffect, useState, type ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createInstance } from "i18next";
import { I18nextProvider, initReactI18next } from "react-i18next";
import { ThemeProvider } from "@/components/theme-provider";
import { Toaster } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { resources } from "@/lib/translations";

export function AppProviders({
    children,
    language,
}: {
    children: ReactNode;
    language: "ru" | "en";
}) {
    const [queryClient] = useState(
        () =>
            new QueryClient({
                defaultOptions: {
                    queries: { staleTime: 30_000, retry: 1 },
                    mutations: { retry: false },
                },
            })
    );
    const [i18n] = useState(() => {
        const instance = createInstance();
        void instance.use(initReactI18next).init({
            resources,
            lng: language,
            fallbackLng: "ru",
            supportedLngs: ["ru", "en"],
            initAsync: false,
            interpolation: { escapeValue: false },
        });
        return instance;
    });
    useEffect(() => {
        const updateLanguage = (value: string) => {
            document.documentElement.lang = value;
        };
        i18n.on("languageChanged", updateLanguage);
        return () => {
            i18n.off("languageChanged", updateLanguage);
        };
    }, [i18n]);
    return (
        <ThemeProvider>
            <I18nextProvider i18n={i18n}>
                <QueryClientProvider client={queryClient}>
                    <TooltipProvider>{children}</TooltipProvider>
                    <Toaster />
                </QueryClientProvider>
            </I18nextProvider>
        </ThemeProvider>
    );
}
