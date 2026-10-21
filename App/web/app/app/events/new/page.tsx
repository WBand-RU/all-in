import { EventForm } from "@/features/events/EventForm";
import { requireSession } from "@/lib/server/session";

export default async function Page() {
    await requireSession("/app/events/new");
    return <EventForm />;
}
