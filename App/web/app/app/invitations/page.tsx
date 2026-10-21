import { MyInvitationsPage } from "@/views/MyInvitationsPage";
import { requireSession } from "@/lib/server/session";

export default async function Page() {
    await requireSession(`/app/invitations`);
    return <MyInvitationsPage />;
}
