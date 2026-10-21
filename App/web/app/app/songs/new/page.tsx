import { SongFormPage } from "@/views/songs/SongFormPage";
import { requireSession } from "@/lib/server/session";

export default async function Page() {
    await requireSession(`/app/songs/new`);
    return <SongFormPage />;
}
