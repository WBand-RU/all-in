"use client";

import { useCallback, useMemo } from "react";
import { usePathname, useRouter } from "next/navigation";

/** Keep application links under /app while resolving relative navigation. */
export function resolveAppPath(path: string, pathname: string) {
    if (path.startsWith("/"))
        return path === "/app" || path.startsWith("/app/")
            ? path
            : `/app${path}`;
    const resolved = new URL(
        path,
        `https://wband.invalid${pathname.replace(/\/$/, "")}/`
    );
    return resolved.pathname === "/"
        ? "/app"
        : resolved.pathname + resolved.search + resolved.hash;
}

export function useNavigator() {
    const router = useRouter();
    const pathname = usePathname();
    const go = useCallback(
        (path: string) => router.push(resolveAppPath(path, pathname)),
        [router, pathname]
    );
    return useMemo(() => ({ go }), [go]);
}
