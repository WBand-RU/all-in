import { BandViewPage } from "@/views/bands/BandViewPage";
import { requireSession } from "@/lib/server/session";

export default async function Page({
    params,
}: {
    params: Promise<{ id: string }>;
}) {
    const { id } = await params;
    await requireSession(`/app/bands/${encodeURIComponent(id)}`);
    return <BandViewPage />;
}
