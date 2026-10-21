"use client";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { CalendarDays, Plus } from "lucide-react";
import { listEvents } from "@/lib/generated/web-api/client";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useBandAccess } from "@/hooks/use-band-access";

export function EventList() {
    const query = useQuery({
        queryKey: ["events"],
        queryFn: ({ signal }) => listEvents(signal),
    });
    const { access } = useBandAccess();
    return (
        <div className="space-y-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <h1 className="text-2xl font-bold">События</h1>
                {access.some((item) => item.canEditContent) && (
                    <Button
                        nativeButton={false}
                        role="link"
                        render={<Link href="/app/events/new" />}
                    >
                        <Plus />
                        Создать событие
                    </Button>
                )}
            </div>
            <p className="text-muted-foreground">
                Мероприятия и выступления ваших групп.
            </p>
            {query.isPending ? (
                <p role="status">Загрузка событий…</p>
            ) : query.isError ? (
                <div role="alert">
                    Не удалось загрузить события.{" "}
                    <Button variant="outline" onClick={() => query.refetch()}>
                        Повторить
                    </Button>
                </div>
            ) : !query.data?.length ? (
                <Card>
                    <CardContent className="py-6">
                        Событий пока нет. Создайте мероприятие и соберите список
                        песен.
                    </CardContent>
                </Card>
            ) : (
                <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                    {query.data.map((item) => (
                        <Card key={item.id}>
                            <CardHeader>
                                <CardTitle>
                                    <Link
                                        className="hover:underline"
                                        href={`/app/events/${item.id}`}
                                    >
                                        {item.title}
                                    </Link>
                                </CardTitle>
                            </CardHeader>
                            <CardContent className="space-y-3">
                                <p className="flex items-center gap-2">
                                    <CalendarDays className="size-4" />
                                    <time dateTime={item.startsAt}>
                                        {new Date(item.startsAt).toLocaleString(
                                            "ru-RU",
                                            {
                                                dateStyle: "medium",
                                                timeStyle: "short",
                                            }
                                        )}
                                    </time>
                                </p>
                                {item.venue && (
                                    <p className="text-muted-foreground">
                                        {item.venue}
                                    </p>
                                )}
                                <p>Песен: {item.songIds.length}</p>
                                <Button
                                    variant="outline"
                                    nativeButton={false}
                                    role="link"
                                    render={
                                        <Link
                                            href={`/app/events/${item.id}/stage`}
                                        />
                                    }
                                >
                                    На сцену
                                </Button>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            )}
        </div>
    );
}
