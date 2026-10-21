import { EventList } from "@/features/events/EventList";
import { requireSession } from "@/lib/server/session";

export default async function Page() {
    await requireSession("/app/events");
    return <EventList />;
}
