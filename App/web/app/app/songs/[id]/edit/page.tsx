import { SongFormPage } from "@/views/songs/SongFormPage";
import { requireSession } from "@/lib/server/session";

export default async function Page({
    params,
}: {
    params: Promise<{ id: string }>;
}) {
    const { id } = await params;
    await requireSession(`/app/songs/${encodeURIComponent(id)}/edit`);
    return <SongFormPage />;
}
