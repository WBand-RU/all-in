import { EventDetails } from "@/features/events/EventDetails";
import { requireSession } from "@/lib/server/session";

export default async function Page({
    params,
}: {
    params: Promise<{ id: string }>;
}) {
    const { id } = await params;
    await requireSession(`/app/events/${encodeURIComponent(id)}/stage`);
    return <EventDetails id={id} mode="stage" />;
}
