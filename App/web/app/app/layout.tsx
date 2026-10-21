import { hasSession } from "@/lib/server/session";
import type { ReactNode } from "react";
import { AppLayout } from "@/AppLayout";
import { AuthorizedProvider } from "@/providers/AuthorizedProvider";

export default async function WorkspaceLayout({
    children,
}: {
    children: ReactNode;
}) {
    // Let the page issue its redirect with the exact destination before mounting client auth.
    if (!(await hasSession())) return children;
    return (
        <AuthorizedProvider>
            <AppLayout>{children}</AppLayout>
        </AuthorizedProvider>
    );
}
