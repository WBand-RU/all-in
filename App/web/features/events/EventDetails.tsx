"use client";
import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    getEvent,
    getSong,
    getSongContent,
    deleteEvent,
} from "@/lib/generated/web-api/client";
import { SongContent } from "@/features/song-content/SongContent";
import { StagePartitionView } from "./StagePartitionView";
import { useAuthContext } from "@/providers/auth/AuthorizationProviderContext";
import { EventForm } from "./EventForm";
import { useBandAccess } from "@/hooks/use-band-access";
import { useToast } from "@/hooks/use-toast";
import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
    DialogFooter,
} from "@/components/ui/dialog";

export function EventDetails({
    id,
    mode = "view",
}: {
    id: string;
    mode?: "view" | "edit" | "stage";
}) {
    const query = useQuery({
        queryKey: ["event", id],
        queryFn: ({ signal }) => getEvent(id, signal),
    });
    const { canEditBand, isPending: permissionsLoading } = useBandAccess();
    const [deleteOpen, setDeleteOpen] = useState(false);
    const [fontSize, setFontSize] = useState(20);
    const router = useRouter();
    const queryClient = useQueryClient();
    const { toast } = useToast();
    const remove = useMutation({
        mutationFn: () => deleteEvent(id),
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ["events"] });
            queryClient.removeQueries({ queryKey: ["event", id] });
            router.push("/app/events");
        },
        onError: () =>
            toast({
                title: "Не удалось удалить событие",
                variant: "destructive",
            }),
    });
    if (query.isPending) return <p role="status">Загрузка события…</p>;
    if (query.isError || !query.data)
        return (
            <div role="alert" className="space-y-3">
                <p>Событие недоступно или не удалось его загрузить.</p>
                <Button variant="outline" onClick={() => query.refetch()}>
                    Повторить
                </Button>
                <Button
                    variant="outline"
                    nativeButton={false}
                    role="link"
                    render={<Link href="/app/events" />}
                >
                    К событиям
                </Button>
            </div>
        );
    const item = query.data;
    if (mode === "edit") {
        if (permissionsLoading) return <p role="status">Проверка доступа…</p>;
        return canEditBand(item.bandId) ? (
            <EventForm key={id} event={item} />
        ) : (
            <p role="alert">
                Редактирование доступно владельцу и администраторам группы.
            </p>
        );
    }
    const stage = mode === "stage";
    return (
        <div
            className={
                stage
                    ? "fixed inset-0 z-50 overflow-y-auto bg-background p-4 md:p-8"
                    : "space-y-6"
            }
        >
            <div
                className={stage ? "mx-auto max-w-5xl space-y-8" : "space-y-6"}
            >
                <header className="space-y-4">
                    <div className="flex flex-wrap items-center justify-between gap-3">
                        <h1 className="text-2xl font-bold">{item.title}</h1>
                        <div className="flex flex-wrap gap-2">
                            {stage ? (
                                <>
                                    <Button
                                        variant="outline"
                                        aria-label="Уменьшить размер текста"
                                        disabled={fontSize <= 16}
                                        onClick={() =>
                                            setFontSize((size) => size - 2)
                                        }
                                    >
                                        A−
                                    </Button>
                                    <Button
                                        variant="outline"
                                        aria-label="Увеличить размер текста"
                                        disabled={fontSize >= 40}
                                        onClick={() =>
                                            setFontSize((size) => size + 2)
                                        }
                                    >
                                        A+
                                    </Button>
                                    <Button
                                        nativeButton={false}
                                        role="link"
                                        render={
                                            <Link href={`/app/events/${id}`} />
                                        }
                                    >
                                        Выйти со сцены
                                    </Button>
                                </>
                            ) : (
                                <>
                                    <Button
                                        nativeButton={false}
                                        role="link"
                                        render={
                                            <Link
                                                href={`/app/events/${id}/stage`}
                                            />
                                        }
                                    >
                                        На сцену
                                    </Button>
                                    {canEditBand(item.bandId) && (
                                        <>
                                            <Button
                                                variant="outline"
                                                nativeButton={false}
                                                role="link"
                                                render={
                                                    <Link
                                                        href={`/app/events/${id}/edit`}
                                                    />
                                                }
                                            >
                                                Редактировать
                                            </Button>
                                            <Button
                                                variant="destructive"
                                                onClick={() =>
                                                    setDeleteOpen(true)
                                                }
                                            >
                                                Удалить
                                            </Button>
                                        </>
                                    )}
                                    <Button
                                        variant="outline"
                                        nativeButton={false}
                                        role="link"
                                        render={<Link href="/app/events" />}
                                    >
                                        К событиям
                                    </Button>
                                </>
                            )}
                        </div>
                    </div>
                    <p className="text-muted-foreground">
                        <time dateTime={item.startsAt}>
                            {new Date(item.startsAt).toLocaleString("ru-RU", {
                                dateStyle: "long",
                                timeStyle: "short",
                            })}
                        </time>
                        {item.venue && ` · ${item.venue}`}
                    </p>
                    {stage && (
                        <p className="text-sm text-muted-foreground">
                            Выберите свою партию для каждой песни. Выбор
                            сохранится в этом браузере.
                        </p>
                    )}
                    {item.description && (
                        <p className="whitespace-pre-wrap">
                            {item.description}
                        </p>
                    )}
                </header>
                {!item.songIds.length ? (
                    <p className="text-muted-foreground">
                        Песни ещё не добавлены. Добавьте их при редактировании
                        события.
                    </p>
                ) : (
                    <>
                        <nav
                            aria-label="Песни события"
                            className="flex flex-wrap gap-2"
                        >
                            {item.songIds.map((songId, index) => (
                                <Button
                                    key={songId}
                                    variant="outline"
                                    nativeButton={false}
                                    role="link"
                                    render={
                                        <a href={`#event-song-${songId}`} />
                                    }
                                >
                                    {index + 1}
                                </Button>
                            ))}
                        </nav>
                        <div
                            className="space-y-8"
                            style={stage ? { fontSize } : undefined}
                        >
                            {item.songIds.map((songId, index) => (
                                <EventSong
                                    key={songId}
                                    id={songId}
                                    index={index}
                                    stage={stage}
                                />
                            ))}
                        </div>
                    </>
                )}
            </div>
            <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Удалить событие?</DialogTitle>
                        <DialogDescription>
                            Событие «{item.title}» будет удалено. Песни
                            останутся в репертуаре группы.
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                        <Button
                            variant="outline"
                            onClick={() => setDeleteOpen(false)}
                        >
                            Отмена
                        </Button>
                        <Button
                            variant="destructive"
                            disabled={remove.isPending}
                            onClick={() => remove.mutate()}
                        >
                            Удалить событие
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}

function EventSong({
    id,
    index,
    stage,
}: {
    id: string;
    index: number;
    stage: boolean;
}) {
    const { id: userId } = useAuthContext();
    const song = useQuery({
        queryKey: ["event-song", id],
        queryFn: ({ signal }) => getSong(id, signal),
    });
    const content = useQuery({
        queryKey: ["song-content", id],
        queryFn: ({ signal }) => getSongContent(id, signal),
        enabled: !!song.data?.value,
    });
    return (
        <Card
            id={`event-song-${id}`}
            className={
                stage ? "scroll-mt-6 text-[length:inherit]" : "scroll-mt-6"
            }
        >
            <CardHeader>
                <CardTitle className={stage ? "text-[1.25em]" : "text-xl"}>
                    {index + 1}.{" "}
                    {song.data?.value?.title ??
                        (song.isPending
                            ? "Загрузка песни…"
                            : "Песня недоступна")}
                </CardTitle>
                {song.data?.value && (
                    <p className="text-muted-foreground">
                        {song.data.value.key}
                        {song.data.value.bpm && ` · ${song.data.value.bpm} BPM`}
                    </p>
                )}
            </CardHeader>
            <CardContent className="space-y-4">
                {song.isError && (
                    <div role="alert">
                        <p>
                            Песня удалена, доступ изменился или произошла ошибка
                            загрузки.
                        </p>
                        <Button
                            variant="outline"
                            onClick={() => song.refetch()}
                        >
                            Повторить
                        </Button>
                    </div>
                )}
                {content.isFetching && <p role="status">Загрузка партий…</p>}
                {content.isError && (
                    <div role="alert">
                        Не удалось загрузить партии.{" "}
                        <Button
                            variant="outline"
                            onClick={() => content.refetch()}
                        >
                            Повторить
                        </Button>
                    </div>
                )}
                {content.data &&
                    (stage ? (
                        <StagePartitionView
                            songId={id}
                            userId={userId}
                            content={content.data}
                        />
                    ) : (
                        <SongContent content={content.data} />
                    ))}
                {!stage && song.data?.value && (
                    <Button
                        variant="outline"
                        nativeButton={false}
                        role="link"
                        render={<Link href={`/app/songs/${id}`} />}
                    >
                        Открыть песню
                    </Button>
                )}
            </CardContent>
        </Card>
    );
}
