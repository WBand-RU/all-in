import { DashboardPage } from "@/views/DashboardPage";
import { requireSession } from "@/lib/server/session";

export default async function Page() {
    await requireSession(`/app`);
    return <DashboardPage />;
}
