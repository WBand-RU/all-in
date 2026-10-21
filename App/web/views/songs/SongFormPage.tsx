"use client";

import { hasId } from "@/lib/api-data";
import { useState } from "react";
import { useParams } from "next/navigation";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import { ArrowLeft, Save } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/components/ui/card";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { useGetListOfBands } from "@/lib/generated/web-api/client";
import {
    createSong,
    updateSong,
    useGetSong,
    getGetSongQueryKey,
    getGetListOfSongsQueryKey,
} from "@/lib/generated/web-api/client";
import type { SongResponse } from "@/lib/generated/web-api/models";
import { songSchema } from "@/lib/form-schemas";
import { useBandAccess } from "@/hooks/use-band-access";
import { useNavigator } from "@/services/navigator";
import { SongPartitionsPanel } from "@/features/song-partitions/SongPartitionsPanel";

type Values = z.infer<typeof songSchema>;
type BandOption = { id: string; name: string };

export function SongFormPage() {
    const { id } = useParams<{ id?: string }>();
    const { t } = useTranslation();
    const song = useGetSong(id ?? "", { query: { enabled: !!id } });
    const bands = useGetListOfBands();
    const access = useBandAccess();
    if (access.isLoading || bands.isLoading || (id && song.isLoading))
        return <p role="status">{t("common.loading")}</p>;
    if (
        access.isError ||
        bands.isError ||
        (id && (song.isError || !song.data?.value))
    )
        return <p role="alert">{t("songs.loadSongFailed")}</p>;
    const editableBands = (bands.data?.value ?? [])
        .filter(hasId)
        .filter((band) => access.canEditBand(band.id));
    if (
        (song.data?.value && !access.canEditBand(song.data.value.bandId)) ||
        (!id && !editableBands.length)
    ) {
        return (
            <p role="alert">
                {t("songs.insufficient")}. {t("songs.memberReadOnly")}
            </p>
        );
    }
    return (
        <SongEditor
            key={id ?? "new"}
            song={id ? (song.data?.value ?? undefined) : undefined}
            bands={editableBands}
        />
    );
}

function SongEditor({
    song: loadedSong,
    bands,
}: {
    song?: SongResponse;
    bands: BandOption[];
}) {
    const [song] = useState(loadedSong);
    const { t } = useTranslation();
    const navigator = useNavigator();
    const queryClient = useQueryClient();
    const form = useForm<Values>({
        resolver: zodResolver(songSchema),
        defaultValues: {
            bandId: song?.bandId ?? bands[0]?.id ?? "",
            title: song?.title ?? "",
            author: song?.authors.join(", ") ?? "",
            key: song?.key ?? "",
            bpm: song?.bpm == null ? null : Number(song.bpm),
        },
    });
    // Keep the version of the snapshot used to initialize the editor. Refetches must not
    // silently advance it while the user is editing, otherwise concurrent writes are lost.

    async function submit(values: Values) {
        try {
            const metadata = {
                title: values.title,
                authors: values.author
                    .split(",")
                    .map((author) => author.trim())
                    .filter(Boolean),
                author: null,
                key: values.key || null,
                bpm: values.bpm,
            };
            const result = song
                ? await updateSong(song.id, {
                      ...metadata,
                      status: song.status,
                      expectedContentVersion: song.contentVersion,
                  })
                : await createSong({ ...metadata, bandId: values.bandId });
            if (result.code !== "Success") {
                toast.error(t("errors.generic"));
                return;
            }
            await Promise.all([
                queryClient.invalidateQueries({
                    queryKey: getGetListOfSongsQueryKey(),
                }),
                ...(song
                    ? [
                          queryClient.invalidateQueries({
                              queryKey: getGetSongQueryKey(song.id),
                          }),
                          queryClient.invalidateQueries({
                              queryKey: ["song-versions", song.id],
                          }),
                      ]
                    : []),
            ]);
            toast.success(
                t(song ? "songActions.updated" : "songActions.created")
            );
            navigator.go(`/songs/${song?.id ?? result.value?.id ?? ""}`);
        } catch (error) {
            const conflict =
                (error as { code?: string })?.code === "song_version_conflict";
            form.setError("root", {
                message: conflict
                    ? "Песня изменена другим участником. Обновите страницу перед сохранением."
                    : t(
                          song
                              ? "songActions.updateFailed"
                              : "songActions.createFailed"
                      ),
            });
        }
    }
    const errors = form.formState.errors;
    const bandItems = bands.map((band) => ({
        value: band.id,
        label: band.name,
    }));
    return (
        <div className="mx-auto w-full max-w-4xl space-y-6 py-4">
            <Card>
                <CardHeader>
                    <div className="flex items-center gap-3">
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => navigator.go("/songs")}
                            aria-label={t("nav.songs")}
                        >
                            <ArrowLeft />
                        </Button>
                        <div>
                            <CardTitle>
                                {t(
                                    song ? "songs.editSong" : "songs.createSong"
                                )}
                            </CardTitle>
                            <CardDescription>
                                {t(
                                    song
                                        ? "songActions.updateHint"
                                        : "songActions.createHint"
                                )}
                            </CardDescription>
                        </div>
                    </div>
                </CardHeader>
                <CardContent>
                    <form
                        onSubmit={form.handleSubmit(submit)}
                        className="space-y-6"
                    >
                        <fieldset
                            disabled={form.formState.isSubmitting}
                            className="grid gap-6 md:grid-cols-2"
                        >
                            {!song && (
                                <Field
                                    className="md:col-span-2"
                                    data-invalid={!!errors.bandId}
                                >
                                    <FieldLabel htmlFor="song-band">
                                        {t("common.band")}
                                    </FieldLabel>
                                    <Controller
                                        name="bandId"
                                        control={form.control}
                                        render={({ field }) => (
                                            <Select
                                                items={bandItems}
                                                name={field.name}
                                                value={field.value}
                                                onValueChange={field.onChange}
                                            >
                                                <SelectTrigger
                                                    id="song-band"
                                                    onBlur={field.onBlur}
                                                    ref={field.ref}
                                                    aria-invalid={
                                                        !!errors.bandId
                                                    }
                                                >
                                                    <SelectValue
                                                        placeholder={t(
                                                            "common.selectBand"
                                                        )}
                                                    />
                                                </SelectTrigger>
                                                <SelectContent>
                                                    {bandItems.map((band) => (
                                                        <SelectItem
                                                            key={band.value}
                                                            value={band.value}
                                                        >
                                                            {band.label}
                                                        </SelectItem>
                                                    ))}
                                                </SelectContent>
                                            </Select>
                                        )}
                                    />
                                    <FieldError errors={[errors.bandId]} />
                                </Field>
                            )}
                            <Field data-invalid={!!errors.title}>
                                <FieldLabel htmlFor="song-title">
                                    {t("songs.fields.title")}
                                </FieldLabel>
                                <Controller
                                    name="title"
                                    control={form.control}
                                    render={({ field }) => (
                                        <Input
                                            id="song-title"
                                            required
                                            maxLength={200}
                                            aria-invalid={!!errors.title}
                                            aria-describedby="song-title-error"
                                            {...field}
                                        />
                                    )}
                                />
                                <FieldError
                                    id="song-title-error"
                                    errors={[errors.title]}
                                />
                            </Field>
                            <Field data-invalid={!!errors.author}>
                                <FieldLabel htmlFor="song-authors">
                                    {t("songs.fields.authors")}
                                </FieldLabel>
                                <Controller
                                    name="author"
                                    control={form.control}
                                    render={({ field }) => (
                                        <Input
                                            id="song-authors"
                                            aria-invalid={!!errors.author}
                                            aria-describedby="song-authors-error"
                                            {...field}
                                        />
                                    )}
                                />
                                <FieldError
                                    id="song-authors-error"
                                    errors={[errors.author]}
                                />
                            </Field>
                            <Field data-invalid={!!errors.key}>
                                <FieldLabel htmlFor="song-key">
                                    {t("songs.fields.key")}
                                </FieldLabel>
                                <Controller
                                    name="key"
                                    control={form.control}
                                    render={({ field }) => (
                                        <Input
                                            id="song-key"
                                            placeholder="C, F#m, Bb"
                                            aria-invalid={!!errors.key}
                                            aria-describedby="song-key-error"
                                            {...field}
                                        />
                                    )}
                                />
                                <FieldError
                                    id="song-key-error"
                                    errors={[errors.key]}
                                />
                            </Field>
                            <Field data-invalid={!!errors.bpm}>
                                <FieldLabel htmlFor="song-bpm">BPM</FieldLabel>
                                <Controller
                                    name="bpm"
                                    control={form.control}
                                    render={({ field }) => (
                                        <Input
                                            id="song-bpm"
                                            type="number"
                                            min={20}
                                            max={400}
                                            step={1}
                                            aria-invalid={!!errors.bpm}
                                            aria-describedby="song-bpm-error"
                                            {...field}
                                            value={field.value ?? ""}
                                            onChange={(event) =>
                                                field.onChange(
                                                    event.target.value === ""
                                                        ? null
                                                        : Number(
                                                              event.target.value
                                                          )
                                                )
                                            }
                                        />
                                    )}
                                />
                                <FieldError
                                    id="song-bpm-error"
                                    errors={[errors.bpm]}
                                />
                            </Field>
                        </fieldset>
                        {errors.root && (
                            <p
                                role="alert"
                                className="text-sm text-destructive"
                            >
                                {errors.root.message}
                            </p>
                        )}
                        <div className="flex justify-end gap-2">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => navigator.go("/songs")}
                            >
                                {t("common.cancel")}
                            </Button>
                            <Button
                                type="submit"
                                disabled={form.formState.isSubmitting}
                            >
                                <Save />
                                {t(
                                    form.formState.isSubmitting
                                        ? "common.updating"
                                        : song
                                          ? "songActions.update"
                                          : "songActions.create"
                                )}
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>
            {song && <SongPartitionsPanel songId={song.id} canEdit />}
        </div>
    );
}
