import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    ArrowDown,
    ArrowLeft,
    ArrowUp,
    Plus,
    Save,
    Trash2,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import { apiClient } from "@/lib/axios-instance";
import { useGetListOfBands } from "@/lib/generated-api/band-api/bands";
import { useBandAccess } from "@/hooks/use-band-access";
import { Button } from "@/shared/ui/button";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/shared/ui/card";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/shared/ui/select";
import { Textarea } from "@/shared/ui/textarea";
import { useToast } from "@/shared/ui/use-toast";
import type { PlaylistDto, PlaylistItemDto } from "./PlaylistsListPage";

type EditableItem = Omit<
    PlaylistItemDto,
    "songTitle" | "songContentVersion" | "order" | "structureOverride"
> & { structureText: string };
type AvailableSong = { id: string; title: string };

export function PlaylistFormPage() {
    const navigate = useNavigate();
    const { id } = useParams();
    const { t } = useTranslation();
    const { toast } = useToast();
    const queryClient = useQueryClient();
    const { access } = useBandAccess();
    const isEditing = !!id;
    const { data: bandsData } = useGetListOfBands();
    const editableBands = (bandsData?.value ?? []).filter((band) =>
        access.some((item) => item.bandId === band.id && item.canEditContent),
    );
    const [bandId, setBandId] = useState("");
    const [title, setTitle] = useState("");
    const [description, setDescription] = useState("");
    const [eventDateTime, setEventDateTime] = useState("");
    const [venue, setVenue] = useState("");
    const [contentVersion, setContentVersion] = useState(1);
    const [items, setItems] = useState<EditableItem[]>([]);
    const initializedPlaylistId = useRef<string | null>(null);
    const { data: songsData = [] } = useQuery({
        queryKey: ["playlist-available-songs", bandId],
        enabled: !!bandId,
        queryFn: async () => {
            const response = await apiClient.get<{
                value: { data: AvailableSong[] };
            }>("/song-api/songs", {
                params: { bandId, page: 1, pageSize: 100 },
            });
            return response.data.value.data ?? [];
        },
    });
    const { data: loaded } = useQuery({
        queryKey: ["playlist-editor", id],
        enabled: isEditing,
        queryFn: async () =>
            (
                await apiClient.get<{ value: PlaylistDto }>(
                    `/playlist-api/playlists/${id}`,
                )
            ).data.value,
        staleTime: 0,
        refetchOnMount: "always",
    });

    useEffect(() => {
        if (!isEditing && !bandId && editableBands.length > 0)
            setBandId(editableBands[0].id);
    }, [isEditing, bandId, editableBands]);
    useEffect(() => {
        if (!loaded || initializedPlaylistId.current === loaded.id) return;

        initializedPlaylistId.current = loaded.id;
        setBandId(loaded.bandId);
        setTitle(loaded.title);
        setDescription(loaded.description ?? "");
        setVenue(loaded.venue ?? "");
        setEventDateTime(
            loaded.eventDateTime ? loaded.eventDateTime.slice(0, 16) : "",
        );
        setContentVersion(loaded.contentVersion);
        setItems(
            [...(loaded.items ?? [])]
                .sort((left, right) => left.order - right.order)
                .map((item) => ({
                    id: item.id,
                    songId: item.songId,
                    keyOverride: item.keyOverride ?? null,
                    bpmOverride: item.bpmOverride ?? null,
                    transition: item.transition,
                    pauseSeconds: item.pauseSeconds ?? 0,
                    crossfadeSeconds: item.crossfadeSeconds ?? 0,
                    notes: item.notes ?? null,
                    stemMixOverrides: item.stemMixOverrides ?? [],
                    structureText: (item.structureOverride ?? [])
                        .map(
                            (section) =>
                                `${section.name}:${section.startBar}-${section.endBar}`,
                        )
                        .join("\n"),
                })),
        );
    }, [loaded]);

    const songs = [...songsData];
    for (const item of loaded?.items ?? []) {
        if (!songs.some((song) => song.id === item.songId)) {
            songs.push({ id: item.songId, title: item.songTitle });
        }
    }

    const save = useMutation({
        mutationFn: async () => {
            const payload = {
                title,
                description: description || null,
                eventDateTime: eventDateTime
                    ? new Date(eventDateTime).toISOString()
                    : null,
                venue: venue || null,
                items: items.map((item) => ({
                    id: item.id,
                    songId: item.songId,
                    keyOverride: item.keyOverride || null,
                    bpmOverride: item.bpmOverride || null,
                    structureOverride: item.structureText
                        .split("\n")
                        .map((line) => /^(.+):(\d+)-(\d+)$/.exec(line.trim()))
                        .filter(Boolean)
                        .map((match) => ({
                            name: match![1],
                            startBar: Number(match![2]),
                            endBar: Number(match![3]),
                        })),
                    stemMixOverrides: [],
                    transition: item.transition,
                    pauseSeconds: item.pauseSeconds,
                    crossfadeSeconds: item.crossfadeSeconds,
                    notes: item.notes || null,
                })),
            };
            return isEditing
                ? apiClient.put(`/playlist-api/playlists/${id}`, {
                      ...payload,
                      expectedContentVersion: contentVersion,
                  })
                : apiClient.post("/playlist-api/playlists", {
                      ...payload,
                      bandId,
                  });
        },
        onSuccess: async () => {
            toast({
                title: t(isEditing ? "playlists.updated" : "playlists.created"),
            });
            await queryClient.invalidateQueries({ queryKey: ["playlist", id] });
            await queryClient.invalidateQueries({
                queryKey: ["playlist-editor", id],
            });
            await queryClient.invalidateQueries({ queryKey: ["playlists"] });
            navigate(
                isEditing && id ? `/app/playlists/${id}` : "/app/playlists",
            );
        },
        onError: () =>
            toast({ title: t("playlists.saveFailed"), variant: "destructive" }),
    });

    const addSong = () => {
        const used = new Set(items.map((item) => item.songId));
        const song = songs.find((candidate) => !used.has(candidate.id));
        if (!song) return;
        setItems([
            ...items,
            {
                id: crypto.randomUUID(),
                songId: song.id,
                keyOverride: null,
                bpmOverride: null,
                transition: "Pause",
                pauseSeconds: 0,
                crossfadeSeconds: 0,
                notes: null,
                stemMixOverrides: [],
                structureText: "",
            },
        ]);
    };
    const move = (index: number, offset: number) => {
        const target = index + offset;
        if (target < 0 || target >= items.length) return;
        const copy = [...items];
        [copy[index], copy[target]] = [copy[target], copy[index]];
        setItems(copy);
    };
    const update = (index: number, value: Partial<EditableItem>) =>
        setItems(
            items.map((item, itemIndex) =>
                itemIndex === index ? { ...item, ...value } : item,
            ),
        );

    const returnPath =
        isEditing && id ? `/app/playlists/${id}` : "/app/playlists";

    return (
        <div className="container mx-auto py-8">
            <Card>
                <CardHeader>
                    <div className="flex items-center gap-4">
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => navigate(returnPath)}
                        >
                            <ArrowLeft className="h-4 w-4" />
                        </Button>
                        <div>
                            <CardTitle>
                                {t(
                                    isEditing
                                        ? "playlists.edit"
                                        : "playlists.new",
                                )}
                            </CardTitle>
                            <CardDescription>
                                {t("playlists.formHint")}
                            </CardDescription>
                        </div>
                    </div>
                </CardHeader>
                <CardContent>
                    <form
                        className="space-y-6"
                        onSubmit={(event) => {
                            event.preventDefault();
                            if (!title.trim() || !bandId) return;
                            save.mutate();
                        }}
                    >
                        <div className="grid gap-4 md:grid-cols-2">
                            <div className="space-y-2">
                                <Label>{t("common.band")} *</Label>
                                <Select
                                    disabled={isEditing}
                                    value={bandId}
                                    onValueChange={(value) => {
                                        setBandId(value);
                                        setItems([]);
                                    }}
                                >
                                    <SelectTrigger>
                                        <SelectValue
                                            placeholder={t("common.selectBand")}
                                        />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {editableBands.map((band) => (
                                            <SelectItem
                                                key={band.id}
                                                value={band.id}
                                            >
                                                {band.name}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                            <div className="space-y-2">
                                <Label>{t("playlists.name")} *</Label>
                                <Input
                                    value={title}
                                    onChange={(event) =>
                                        setTitle(event.target.value)
                                    }
                                />
                            </div>
                            <div className="space-y-2">
                                <Label>{t("playlists.dateTime")}</Label>
                                <Input
                                    type="datetime-local"
                                    value={eventDateTime}
                                    onChange={(event) =>
                                        setEventDateTime(event.target.value)
                                    }
                                />
                            </div>
                            <div className="space-y-2">
                                <Label>{t("playlists.venue")}</Label>
                                <Input
                                    value={venue}
                                    onChange={(event) =>
                                        setVenue(event.target.value)
                                    }
                                />
                            </div>
                        </div>
                        <div className="space-y-2">
                            <Label>{t("playlists.notes")}</Label>
                            <Textarea
                                value={description}
                                onChange={(event) =>
                                    setDescription(event.target.value)
                                }
                            />
                        </div>
                        <div className="flex items-center justify-between">
                            <h3 className="font-semibold">
                                {t("playlists.items")}
                            </h3>
                            <Button
                                type="button"
                                disabled={
                                    !bandId || items.length >= songs.length
                                }
                                onClick={addSong}
                            >
                                <Plus className="mr-2 h-4 w-4" />
                                {t("playlists.addSong")}
                            </Button>
                        </div>
                        {items.length === 0 && (
                            <div className="rounded-md border border-dashed p-8 text-center text-muted-foreground">
                                {t("playlists.noItems")}
                            </div>
                        )}
                        <div className="space-y-4">
                            {items.map((item, index) => (
                                <Card key={item.id}>
                                    <CardContent className="space-y-4 pt-6">
                                        <div className="flex items-center gap-2">
                                            <strong className="mr-auto">
                                                {index + 1}.{" "}
                                                {songs.find(
                                                    (song) =>
                                                        song.id === item.songId,
                                                )?.title ??
                                                    loaded?.items.find(
                                                        (loadedItem) =>
                                                            loadedItem.songId ===
                                                            item.songId,
                                                    )?.songTitle}
                                            </strong>
                                            <Button
                                                type="button"
                                                variant="ghost"
                                                size="icon"
                                                onClick={() => move(index, -1)}
                                            >
                                                <ArrowUp className="h-4 w-4" />
                                            </Button>
                                            <Button
                                                type="button"
                                                variant="ghost"
                                                size="icon"
                                                onClick={() => move(index, 1)}
                                            >
                                                <ArrowDown className="h-4 w-4" />
                                            </Button>
                                            <Button
                                                type="button"
                                                variant="ghost"
                                                size="icon"
                                                onClick={() =>
                                                    setItems(
                                                        items.filter(
                                                            (_, itemIndex) =>
                                                                itemIndex !==
                                                                index,
                                                        ),
                                                    )
                                                }
                                            >
                                                <Trash2 className="h-4 w-4" />
                                            </Button>
                                        </div>
                                        <Select
                                            value={item.songId}
                                            onValueChange={(songId) =>
                                                update(index, { songId })
                                            }
                                        >
                                            <SelectTrigger>
                                                <SelectValue />
                                            </SelectTrigger>
                                            <SelectContent>
                                                {songs.map((song) => (
                                                    <SelectItem
                                                        key={song.id}
                                                        value={song.id}
                                                    >
                                                        {song.title}
                                                    </SelectItem>
                                                ))}
                                            </SelectContent>
                                        </Select>
                                        <div className="grid gap-4 md:grid-cols-3">
                                            <div className="space-y-2">
                                                <Label>
                                                    {t("playlists.keyOverride")}
                                                </Label>
                                                <Input
                                                    value={
                                                        item.keyOverride ?? ""
                                                    }
                                                    onChange={(event) =>
                                                        update(index, {
                                                            keyOverride:
                                                                event.target
                                                                    .value,
                                                        })
                                                    }
                                                />
                                            </div>
                                            <div className="space-y-2">
                                                <Label>
                                                    {t("playlists.bpmOverride")}
                                                </Label>
                                                <Input
                                                    type="number"
                                                    min="20"
                                                    max="400"
                                                    value={
                                                        item.bpmOverride ?? ""
                                                    }
                                                    onChange={(event) =>
                                                        update(index, {
                                                            bpmOverride: event
                                                                .target.value
                                                                ? Number(
                                                                      event
                                                                          .target
                                                                          .value,
                                                                  )
                                                                : null,
                                                        })
                                                    }
                                                />
                                            </div>
                                            <div className="space-y-2">
                                                <Label>
                                                    {t("playlists.transition")}
                                                </Label>
                                                <Select
                                                    value={item.transition}
                                                    onValueChange={(
                                                        transition: EditableItem["transition"],
                                                    ) =>
                                                        update(index, {
                                                            transition,
                                                        })
                                                    }
                                                >
                                                    <SelectTrigger>
                                                        <SelectValue />
                                                    </SelectTrigger>
                                                    <SelectContent>
                                                        <SelectItem value="AutoStart">
                                                            {t(
                                                                "playlists.autoStart",
                                                            )}
                                                        </SelectItem>
                                                        <SelectItem value="Pause">
                                                            {t(
                                                                "playlists.pause",
                                                            )}
                                                        </SelectItem>
                                                        <SelectItem value="Crossfade">
                                                            {t(
                                                                "playlists.crossfade",
                                                            )}
                                                        </SelectItem>
                                                    </SelectContent>
                                                </Select>
                                            </div>
                                        </div>
                                        {item.transition === "Pause" && (
                                            <div className="space-y-2">
                                                <Label>
                                                    {t(
                                                        "playlists.pauseSeconds",
                                                    )}
                                                </Label>
                                                <Input
                                                    type="number"
                                                    min="0"
                                                    max="3600"
                                                    value={item.pauseSeconds}
                                                    onChange={(event) =>
                                                        update(index, {
                                                            pauseSeconds:
                                                                Number(
                                                                    event.target
                                                                        .value,
                                                                ),
                                                        })
                                                    }
                                                />
                                            </div>
                                        )}
                                        {item.transition === "Crossfade" && (
                                            <div className="space-y-2">
                                                <Label>
                                                    {t(
                                                        "playlists.crossfadeSeconds",
                                                    )}
                                                </Label>
                                                <Input
                                                    type="number"
                                                    min="0"
                                                    max="60"
                                                    value={
                                                        item.crossfadeSeconds
                                                    }
                                                    onChange={(event) =>
                                                        update(index, {
                                                            crossfadeSeconds:
                                                                Number(
                                                                    event.target
                                                                        .value,
                                                                ),
                                                        })
                                                    }
                                                />
                                            </div>
                                        )}
                                        <div className="space-y-2">
                                            <Label>
                                                {t(
                                                    "playlists.structureOverride",
                                                )}
                                            </Label>
                                            <Textarea
                                                value={item.structureText}
                                                onChange={(event) =>
                                                    update(index, {
                                                        structureText:
                                                            event.target.value,
                                                    })
                                                }
                                                placeholder={t(
                                                    "songs.sectionsPlaceholder",
                                                )}
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label>
                                                {t("playlists.itemNotes")}
                                            </Label>
                                            <Input
                                                value={item.notes ?? ""}
                                                onChange={(event) =>
                                                    update(index, {
                                                        notes: event.target
                                                            .value,
                                                    })
                                                }
                                            />
                                        </div>
                                    </CardContent>
                                </Card>
                            ))}
                        </div>
                        <div className="flex justify-end gap-3">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => navigate(returnPath)}
                            >
                                {t("common.cancel")}
                            </Button>
                            <Button
                                type="submit"
                                disabled={
                                    save.isPending || !title.trim() || !bandId
                                }
                            >
                                <Save className="mr-2 h-4 w-4" />
                                {t("common.save")}
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}
