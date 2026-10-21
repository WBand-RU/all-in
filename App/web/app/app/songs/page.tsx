import { SongsListPage } from "@/views/songs/SongsListPage";
import { requireSession } from "@/lib/server/session";

export default async function Page() {
    await requireSession(`/app/songs`);
    return <SongsListPage />;
}
