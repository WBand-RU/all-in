import "server-only";

import { cache } from "react";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

/** Verify the existing BFF session on every server request, without caching across users. */
export const hasSession = cache(async () => {
    const cookieStore = await cookies();
    if (
        !cookieStore.has("WBand.Session") &&
        !cookieStore.has("__Host-WBand.Session")
    )
        return false;
    const origin = process.env.BFF_ORIGIN ?? "http://localhost:4000";
    const response = await fetch(new URL("/auth/me", origin), {
        headers: { cookie: cookieStore.toString() },
        cache: "no-store",
        redirect: "manual",
        signal: AbortSignal.timeout(10_000),
    });
    if (response.status === 401) return false;
    if (!response.ok)
        throw new Error(
            `Сервис авторизации временно недоступен (HTTP ${response.status})`
        );
    // CSRF initialization runs in the browser so the BFF can set its antiforgery cookie.
    // Do not serialize OAuth tokens or an unusable server-side antiforgery token.
    return true;
});

export async function requireSession(returnUrl: string) {
    if (!(await hasSession()))
        redirect(`/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`);
}
