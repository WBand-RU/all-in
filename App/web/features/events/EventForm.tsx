"use client";
import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Controller, useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowUp, ArrowDown, X } from "lucide-react";
import {
    createEvent,
    updateEvent,
    getListOfBands,
    getListOfSongs,
    getSong,
} from "@/lib/generated/web-api/client";
import type {
    EventResponse,
    SongResponse,
} from "@/lib/generated/web-api/models";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import { useBandAccess } from "@/hooks/use-band-access";
import { useToast } from "@/hooks/use-toast";
import { eventSchema, localDateTime, type EventFormValues } from "./event-form";

export function EventForm({ event: loadedEvent }: { event?: EventResponse }) {
    const [event] = useState(loadedEvent);
    const router = useRouter();
    const queryClient = useQueryClient();
    const { toast } = useToast();
    const { canEditBand } = useBandAccess();
    const bands = useQuery({
        queryKey: ["event-bands"],
        queryFn: () => getListOfBands(),
    });
    const form = useForm<EventFormValues>({
        resolver: zodResolver(eventSchema),
        defaultValues: {
            bandId: event?.bandId ?? "",
            title: event?.title ?? "",
            startsAt: event ? localDateTime(event.startsAt) : "",
            venue: event?.venue ?? "",
            description: event?.description ?? "",
            songIds: event?.songIds ?? [],
        },
    });
    const bandId = useWatch({ control: form.control, name: "bandId" });
    const selected = useWatch({ control: form.control, name: "songIds" });
    const [search, setSearch] = useState("");
    const songs = useQuery({
        queryKey: ["event-song-picker", bandId],
        enabled: !!bandId,
        queryFn: async () => {
            const result: SongResponse[] = [];
            for (let page = 1; ; page++) {
                const response = await getListOfSongs({
                    bandId,
                    page,
                    pageSize: 100,
                });
                const batch = response.value?.data ?? [];
                result.push(...batch);
                if (
                    !batch.length ||
                    result.length >= Number(response.value?.total ?? 0)
                )
                    return result;
            }
        },
    });
    const existing = useQuery({
        queryKey: ["event-existing-songs", event?.id],
        enabled: !!event?.songIds.length,
        queryFn: async () =>
            Promise.all(
                (event?.songIds ?? []).map(async (id) => {
                    try {
                        return (await getSong(id)).value;
                    } catch {
                        return null;
                    }
                })
            ),
    });
    const names = new Map(
        [...(existing.data ?? []), ...(songs.data ?? [])]
            .filter((song): song is SongResponse => !!song)
            .map((song) => [song.id, song.title])
    );
    const save = useMutation({
        mutationFn: async (values: EventFormValues) => {
            const data = {
                ...values,
                startsAt: new Date(values.startsAt).toISOString(),
                expectedVersion: event?.version ?? null,
            };
            return event ? updateEvent(event.id, data) : createEvent(data);
        },
        onSuccess: async (saved) => {
            await queryClient.invalidateQueries({ queryKey: ["events"] });
            await queryClient.invalidateQueries({
                queryKey: ["event", saved.id],
            });
            router.push(`/app/events/${saved.id}`);
        },
        onError: (error: unknown) => {
            const message =
                error && typeof error === "object" && "message" in error
                    ? String(error.message)
                    : "Возможно, событие уже изменено. Обновите страницу и попробуйте снова.";
            toast({
                title: "Не удалось сохранить событие",
                description: message,
                variant: "destructive",
            });
        },
    });
    const setSongs = (ids: string[]) =>
        form.setValue("songIds", ids, {
            shouldDirty: true,
            shouldValidate: true,
        });
    const move = (index: number, direction: number) => {
        const ids = [...selected];
        [ids[index], ids[index + direction]] = [
            ids[index + direction],
            ids[index],
        ];
        setSongs(ids);
    };
    return (
        <form
            onSubmit={form.handleSubmit((values) => save.mutate(values))}
            className="max-w-3xl space-y-6"
        >
            <h1 className="text-2xl font-bold">
                {event ? "Редактировать событие" : "Создать событие"}
            </h1>
            <fieldset disabled={save.isPending} className="space-y-5">
                <Controller
                    name="bandId"
                    control={form.control}
                    render={({ field, fieldState }) => (
                        <Field data-invalid={fieldState.invalid}>
                            <FieldLabel htmlFor="event-band">Группа</FieldLabel>
                            <select
                                {...field}
                                id="event-band"
                                disabled={!!event}
                                className="h-10 w-full rounded-md border bg-background px-3"
                                onChange={(e) => {
                                    field.onChange(e);
                                    setSongs([]);
                                }}
                            >
                                <option value="">Выберите группу</option>
                                {bands.data?.value
                                    ?.filter(
                                        (band) =>
                                            band.id && canEditBand(band.id)
                                    )
                                    .map((band) => (
                                        <option key={band.id} value={band.id}>
                                            {band.name}
                                        </option>
                                    ))}
                            </select>
                            <FieldError errors={[fieldState.error]} />
                        </Field>
                    )}
                />
                {(
                    [
                        ["title", "Название", "text", 200],
                        [
                            "startsAt",
                            "Дата и время",
                            "datetime-local",
                            undefined,
                        ],
                        ["venue", "Место", "text", 500],
                    ] as const
                ).map(([name, label, type, maxLength]) => (
                    <Controller
                        key={name}
                        name={name}
                        control={form.control}
                        render={({ field, fieldState }) => (
                            <Field data-invalid={fieldState.invalid}>
                                <FieldLabel htmlFor={`event-${name}`}>
                                    {label}
                                </FieldLabel>
                                <Input
                                    {...field}
                                    id={`event-${name}`}
                                    type={type}
                                    maxLength={maxLength}
                                    aria-invalid={fieldState.invalid}
                                />
                                <FieldError errors={[fieldState.error]} />
                            </Field>
                        )}
                    />
                ))}
                <Controller
                    name="description"
                    control={form.control}
                    render={({ field, fieldState }) => (
                        <Field>
                            <FieldLabel htmlFor="event-description">
                                Описание
                            </FieldLabel>
                            <Textarea
                                {...field}
                                id="event-description"
                                maxLength={5000}
                            />
                            <FieldError errors={[fieldState.error]} />
                        </Field>
                    )}
                />
                <section className="space-y-3">
                    <h2 className="text-lg font-semibold">
                        Песни и порядок выступления
                    </h2>
                    <ol className="space-y-2">
                        {selected.map((id, index) => (
                            <li
                                key={id}
                                className="flex items-center gap-2 rounded-md border p-2"
                            >
                                <span className="flex-1">
                                    {index + 1}.{" "}
                                    {names.get(id) ??
                                        "Песня недоступна — удалите её из списка"}
                                </span>
                                <Button
                                    type="button"
                                    size="icon"
                                    variant="ghost"
                                    disabled={index === 0}
                                    aria-label={`Выше: ${names.get(id) ?? index + 1}`}
                                    onClick={() => move(index, -1)}
                                >
                                    <ArrowUp />
                                </Button>
                                <Button
                                    type="button"
                                    size="icon"
                                    variant="ghost"
                                    disabled={index === selected.length - 1}
                                    aria-label={`Ниже: ${names.get(id) ?? index + 1}`}
                                    onClick={() => move(index, 1)}
                                >
                                    <ArrowDown />
                                </Button>
                                <Button
                                    type="button"
                                    size="icon"
                                    variant="ghost"
                                    aria-label={`Убрать: ${names.get(id) ?? index + 1}`}
                                    onClick={() =>
                                        setSongs(
                                            selected.filter(
                                                (songId) => songId !== id
                                            )
                                        )
                                    }
                                >
                                    <X />
                                </Button>
                            </li>
                        ))}
                    </ol>
                    {!selected.length && (
                        <p className="text-muted-foreground">
                            Песни ещё не добавлены.
                        </p>
                    )}
                    <Field>
                        <FieldLabel htmlFor="event-song-search">
                            Добавить песни группы
                        </FieldLabel>
                        <Input
                            id="event-song-search"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                            placeholder="Поиск по названию"
                            disabled={!bandId}
                        />
                    </Field>
                    {songs.isFetching && <p role="status">Загрузка песен…</p>}
                    {songs.isError && (
                        <p role="alert">
                            Не удалось загрузить песни.{" "}
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => songs.refetch()}
                            >
                                Повторить
                            </Button>
                        </p>
                    )}
                    <div className="max-h-64 space-y-2 overflow-y-auto">
                        {songs.data
                            ?.filter(
                                (song) =>
                                    !selected.includes(song.id) &&
                                    song.title
                                        .toLocaleLowerCase()
                                        .includes(search.toLocaleLowerCase())
                            )
                            .map((song) => (
                                <div
                                    key={song.id}
                                    className="flex items-center justify-between gap-3 rounded-md border p-2"
                                >
                                    <span>{song.title}</span>
                                    <Button
                                        type="button"
                                        variant="outline"
                                        disabled={selected.length >= 200}
                                        onClick={() =>
                                            setSongs([...selected, song.id])
                                        }
                                    >
                                        Добавить
                                    </Button>
                                </div>
                            ))}
                    </div>
                    {!!bandId && songs.isSuccess && !songs.data.length && (
                        <p className="text-muted-foreground">
                            У этой группы пока нет песен.
                        </p>
                    )}
                    <FieldError errors={[form.formState.errors.songIds]} />
                </section>
                <div className="flex gap-3">
                    <Button type="submit" disabled={save.isPending || !bandId}>
                        {save.isPending ? "Сохранение…" : "Сохранить событие"}
                    </Button>
                    <Button
                        variant="outline"
                        nativeButton={false}
                        role="link"
                        render={
                            <Link
                                href={
                                    event
                                        ? `/app/events/${event.id}`
                                        : "/app/events"
                                }
                            />
                        }
                    >
                        Отмена
                    </Button>
                </div>
            </fieldset>
        </form>
    );
}
