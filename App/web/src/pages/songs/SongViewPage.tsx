import { useState, useEffect } from "react";
import { useParams } from "react-router";
import { Edit, ArrowLeft, Music, User, Hash, Activity, History, ChevronDown, Eye } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Label } from "@/shared/ui/label";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/shared/ui/card";
import { useToast } from "@/shared/ui/use-toast";
import { useNavigator } from "@/services/navigator";
import { getGetSongQueryKey, useGetSong } from "@/lib/generated-api/song-api/songs";
import type { Song } from "@/lib/generated-api/song-api/models/song";
import { Player } from "@/widgets/player";
import { apiClient } from "@/lib/axios-instance";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/shared/ui/collapsible";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/shared/ui/dialog";
import { useBandAccess } from "@/hooks/use-band-access";
import { useTranslation } from "react-i18next";
import { StemsPanel } from "@/features/stems/StemsPanel";
import { SongSectionsPanel } from "@/features/song-sections/SongSectionsPanel";

interface SongVersion {
    contentVersion: number;
    createdAt: string;
    createdBy: string;
    changedByEmail?: string | null;
    changedByRole?: string | null;
}

interface SongVersionDetails {
    contentVersion: number;
    createdAt: string;
    createdBy: string;
    title: string;
    authors: string[];
    key?: string | null;
    bpm?: number | null;
    tempoTrack: { bar: number; bpm: number }[];
    timeSignatureTrack: { bar: number; beats: number; beatUnit: number }[];
    countInBars: number;
    status: string | number;
}

export function SongViewPage() {
    const navigator = useNavigator();
    const { id } = useParams();
    const { toast } = useToast();
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const { canEditBand } = useBandAccess();

    const { data: songData, isLoading, error } = useGetSong(id || "");

    const [song, setSong] = useState<Song | null>(null);
    const [historyOpen, setHistoryOpen] = useState(false);
    const [previewVersion, setPreviewVersion] = useState<number | null>(null);
    const { data: versionsData, isLoading: versionsLoading } = useQuery({
        queryKey: ["song-versions", id],
        enabled: !!id,
        queryFn: async () => (await apiClient.get<{ value: SongVersion[] }>(`/song-api/songs/${id}/versions`)).data,
    });
    const restoreVersion = useMutation({
        mutationFn: async (contentVersion: number) =>
            apiClient.post(`/song-api/songs/${id}/versions/${contentVersion}/restore`),
        onSuccess: async () => {
            await Promise.all([
                queryClient.invalidateQueries({ queryKey: getGetSongQueryKey(id || "") }),
                queryClient.invalidateQueries({ queryKey: ["song-versions", id] }),
            ]);
            setPreviewVersion(null);
            toast({ title: t("songs.restored") });
        },
        onError: () => toast({ title: t("songs.restoreFailed"), variant: "destructive" }),
    });
    const { data: previewData, isLoading: previewLoading, isError: previewError } = useQuery({
        queryKey: ["song-version-preview", id, previewVersion],
        enabled: !!id && previewVersion !== null,
        queryFn: async () => (await apiClient.get<{ value: SongVersionDetails }>(
            `/song-api/songs/${id}/versions/${previewVersion}`,
        )).data,
    });

    useEffect(() => {
        if (songData?.value) {
            setSong(songData.value);
        }
    }, [songData]);

    useEffect(() => {
        if (error) {
            toast({
                title: t("common.error"),
                description: t("songs.loadSongFailed"),
                variant: "destructive",
            });
            navigator.go("/songs");
        }
    }, [error, toast, navigator]);

    if (isLoading) {
        return (
            <div className="container mx-auto py-8">
                <Card>
                    <CardContent className="py-12">
                        <div className="text-center">{t("songs.loadingSong")}</div>
                    </CardContent>
                </Card>
            </div>
        );
    }

    if (!song) {
        return (
            <div className="container mx-auto py-8">
                <Card>
                    <CardContent className="py-12">
                        <div className="text-center">{t("songActions.notFound")}</div>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <div className="container mx-auto py-8">
            <Card>
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-4">
                            <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => navigator.go("/songs")}
                            >
                                <ArrowLeft className="h-4 w-4" />
                            </Button>
                            <div>
                                <CardTitle className="flex items-center gap-2">
                                    <Music className="h-5 w-5" />
                                    {song.title}
                                </CardTitle>
                                <CardDescription>{t("songs.details")}</CardDescription>
                            </div>
                        </div>
                        {canEditBand(song.bandId) && <Button
                            variant="outline"
                            onClick={() =>
                                navigator.go(`/songs/${song.id}/edit`)
                            }
                        >
                            <Edit className="mr-2 h-4 w-4" />
                            {t("songs.editSong")}
                        </Button>}
                    </div>
                </CardHeader>
                <CardContent className="space-y-6">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <div className="space-y-2">
                            <Label>{t("songs.fields.title")}</Label>
                            <div className="text-lg font-medium">
                                {song.title}
                            </div>
                        </div>

                        {song.author && (
                            <div className="space-y-2">
                                <Label className="flex items-center gap-2">
                                    <User className="h-4 w-4" />
                                    {t("songActions.authorArtist")}
                                </Label>
                                <div>{song.author}</div>
                            </div>
                        )}

                        {song.key && (
                            <div className="space-y-2">
                                <Label className="flex items-center gap-2">
                                    <Hash className="h-4 w-4" />
                                    {t("songs.fields.key")}
                                </Label>
                                <div>{song.key}</div>
                            </div>
                        )}

                        {song.bpm && (
                            <div className="space-y-2">
                                <Label className="flex items-center gap-2">
                                    <Activity className="h-4 w-4" />
                                    BPM
                                </Label>
                                <div>{song.bpm}</div>
                            </div>
                        )}
                    </div>

                    <div className="text-sm text-muted-foreground">
                        {t("songActions.createdAt", { date: new Date(song.createdAt).toLocaleDateString() })}
                    </div>
                </CardContent>
            </Card>

            <Collapsible className="mb-6" open={historyOpen} onOpenChange={setHistoryOpen}>
                <Card className="mt-6">
                    <CollapsibleTrigger asChild>
                        <button type="button" className="flex w-full items-center justify-between p-6 text-left">
                            <div>
                                <CardTitle className="flex items-center gap-2">
                                    <History className="h-5 w-5" />
                                    {t("songs.history")}
                                </CardTitle>
                                <CardDescription className="mt-1.5">{t("songs.currentVersion", { version: song.contentVersion })}</CardDescription>
                            </div>
                            <ChevronDown className={`h-5 w-5 transition-transform ${historyOpen ? "rotate-180" : ""}`} />
                        </button>
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <CardContent>
                    {versionsLoading ? <div>{t("common.loading")}</div> : (
                        <div className="space-y-2">
                            {(versionsData?.value ?? []).map((version) => (
                                <div key={version.contentVersion} className="flex items-center justify-between rounded-md border p-3">
                                    <div>
                                        <div className="font-medium">{t("songs.version", { version: version.contentVersion })}</div>
                                        <div className="text-sm text-muted-foreground">
                                            {new Date(version.createdAt).toLocaleString()}
                                        </div>
                                        <div className="text-sm text-muted-foreground">
                                            {t("songs.changedBy", { actor: version.changedByEmail ?? version.createdBy })}
                                            {version.changedByRole ? ` (${version.changedByRole})` : ""}
                                        </div>
                                    </div>
                                    <div className="flex gap-2">
                                        {canEditBand(song.bandId) && <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => setPreviewVersion(version.contentVersion)}
                                        >
                                            <Eye className="mr-2 h-4 w-4" />
                                            {t("common.view")}
                                        </Button>}
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            disabled={version.contentVersion === song.contentVersion || restoreVersion.isPending}
                                            onClick={() => restoreVersion.mutate(version.contentVersion)}
                                        >
                                            {version.contentVersion === song.contentVersion ? t("common.current") : t("common.restore")}
                                        </Button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                        </CardContent>
                    </CollapsibleContent>
                </Card>
            </Collapsible>

            <Dialog
                open={previewVersion !== null}
                onOpenChange={(open) => { if (!open) setPreviewVersion(null); }}
            >
                <DialogContent className="max-h-[90vh] max-w-3xl overflow-y-auto">
                    <DialogHeader>
                        <DialogTitle>{t("songs.previewTitle", { version: previewVersion })}</DialogTitle>
                        <DialogDescription>
                            {t("songs.previewHint")}
                        </DialogDescription>
                    </DialogHeader>
                    {previewError ? <div className="text-destructive">{t("songs.previewFailed")}</div>
                    : previewLoading || !previewData?.value ? <div>{t("common.loading")}</div> : (() => {
                        const version = previewData.value;
                        return <div className="space-y-6">
                            <div className="grid gap-4 sm:grid-cols-2">
                                <div><Label>{t("songs.fields.title")}</Label><div className="font-medium">{version.title}</div></div>
                                <div><Label>{t("songs.fields.authors")}</Label><div>{version.authors.join(", ") || "—"}</div></div>
                                <div><Label>{t("songs.fields.key")}</Label><div>{version.key || "—"}</div></div>
                                <div><Label>BPM</Label><div>{version.bpm || "—"}</div></div>
                                <div>
                                    <Label>{t("songs.fields.tempoChanges")}</Label>
                                    <div>{version.tempoTrack.map(x => `${x.bpm} BPM ${t("songs.fields.fromBar", { bar: x.bar })}`).join(", ") || "—"}</div>
                                </div>
                                <div>
                                    <Label>{t("songs.fields.signature")}</Label>
                                    <div>{version.timeSignatureTrack.map(x => `${x.beats}/${x.beatUnit} ${t("songs.fields.fromBar", { bar: x.bar })}`).join(", ") || "—"}</div>
                                </div>
                                <div><Label>{t("songs.fields.countIn")}</Label><div>{version.countInBars}</div></div>
                            </div>
                            {canEditBand(song.bandId) && <div className="flex justify-end border-t pt-4">
                                <Button
                                    disabled={version.contentVersion === song.contentVersion || restoreVersion.isPending}
                                    onClick={() => restoreVersion.mutate(version.contentVersion)}
                                >
                                    {version.contentVersion === song.contentVersion
                                        ? t("songs.thisIsCurrent")
                                        : restoreVersion.isPending
                                          ? t("songs.restoring")
                                          : t("songs.restoreThis")}
                                </Button>
                            </div>}
                        </div>;
                    })()}
                </DialogContent>
            </Dialog>

            <SongSectionsPanel songId={song.id} canEdit={canEditBand(song.bandId)} />
            <StemsPanel songId={song.id} canEdit={canEditBand(song.bandId)} />

            {/* Player Component */}
            <Player songId={id || ""} songTitle={song.title} />
        </div>
    );
}
