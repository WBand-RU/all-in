import { Geist_Mono, Inter } from "next/font/google";
import { cn } from "@/lib/utils";
import type { Metadata } from "next";
import { cookies } from "next/headers";
import { AppProviders } from "@/providers/app-providers";
import "./globals.css";

const inter = Inter({ subsets: ["latin"], variable: "--font-sans" });
const fontMono = Geist_Mono({ subsets: ["latin"], variable: "--font-mono" });

export const metadata: Metadata = {
    title: { default: "WBand", template: "%s · WBand" },
    description: "Группы, участники и репертуар музыкантов",
};

export default async function RootLayout({
    children,
}: Readonly<{ children: React.ReactNode }>) {
    const language =
        (await cookies()).get("wband-language")?.value === "en" ? "en" : "ru";
    return (
        <html
            lang={language}
            suppressHydrationWarning
            className={cn(
                "antialiased",
                fontMono.variable,
                "font-sans",
                inter.variable
            )}
        >
            <body>
                <AppProviders language={language}>{children}</AppProviders>
            </body>
        </html>
    );
}
