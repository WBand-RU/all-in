import { BandListPage } from "@/views/bands/BandListPage";
import { requireSession } from "@/lib/server/session";

export default async function Page() {
    await requireSession(`/app/bands`);
    return <BandListPage />;
}
