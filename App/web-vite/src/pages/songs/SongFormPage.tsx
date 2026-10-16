import { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router";
import { useNavigator } from "@/services/navigator";
import { Save, ArrowLeft } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/shared/ui/card";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/shared/ui/select";
import { useToast } from "@/shared/ui/use-toast";
import { useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
    useCreateSong,
    useUpdateSong,
    useGetSong,
    getGetSongQueryKey,
    getGetListOfSongsQueryKey,
} from "@/lib/generated-api/song-api/songs";
import { useGetListOfBands } from "@/lib/generated-api/band-api/bands";
import { useBandAccess } from "@/hooks/use-band-access";
import { SongSectionsPanel } from "@/features/song-sections/SongSectionsPanel";

const musicalKeys = [
    "Ab",
    "A",
    "A#",
    "Abm",
    "Am",
    "A#m",
    "Bb",
    "B",
    "Bbm",
    "Bm",
    "C",
    "C#",
    "Cm",
    "C#m",
    "Db",
    "D",
    "Dbm",
    "Dm",
    "D#",
    "D#m",
    "Eb",
    "E",
    "Ebm",
    "Em",
    "F",
    "Fm",
    "F#",
    "F#m",
    "Gb",
    "G",
    "Gbm",
    "Gm",
    "G#",
    "G#m",
];

interface SongForm {
    title: string;
    author: string;
    key: string;
    bpm: number | null;
}

export function SongFormPage() {
    const navigator = useNavigator();
    const navigate = useNavigate();
    const { id } = useParams();
    const { toast } = useToast();
    const { t } = useTranslation();
    const isEditing = !!id;
    const queryClient = useQueryClient();
    const createSong = useCreateSong({
        mutation: {
            onSuccess: async (response) => {
                await queryClient.invalidateQueries({ queryKey: getGetListOfSongsQueryKey() });
                toast({
                    title: t("common.success"),
                    description: t("songActions.created"),
                });
                // Navigate to the created song view
                if (response.value?.id) {
                    navigator.go(`/songs/${response.value.id}`);
                } else {
                    navigator.go("/songs");
                }
            },
            onError: () => {
                toast({
                    title: t("common.error"),
                    description: t("songActions.createFailed"),
                    variant: "destructive",
                });
            },
        },
    });

    const updateSong = useUpdateSong({
        mutation: {
            onSuccess: async () => {
                await Promise.all([
                    queryClient.invalidateQueries({ queryKey: getGetListOfSongsQueryKey() }),
                    queryClient.invalidateQueries({ queryKey: getGetSongQueryKey(id || "") }),
                    queryClient.invalidateQueries({ queryKey: ["song-versions", id] }),
                ]);
                toast({
                    title: t("common.success"),
                    description: t("songActions.updated"),
                });
                navigator.go("/songs");
            },
            onError: () => {
                toast({
                    title: t("common.error"),
                    description: t("songActions.updateFailed"),
                    variant: "destructive",
                });
            },
        },
    });
    const [form, setForm] = useState<SongForm>({
        title: "",
        author: "",
        key: "",
        bpm: null,
    });
    const [currentBandId, setCurrentBandId] = useState("");
    const { data: bandsData } = useGetListOfBands();
    const bands = bandsData?.value ?? [];
    const { access, canEditBand, isLoading: accessLoading } = useBandAccess();
    const editableBands = bands.filter(band =>
        access.some(item => item.bandId === band.id && item.canEditContent),
    );

    useEffect(() => {
        if (!currentBandId && editableBands.length > 0) setCurrentBandId(editableBands[0].id);
    }, [editableBands, currentBandId]);

    const {
        data: songData,
        isLoading: isLoadingSong,
        error: songError,
    } = useGetSong(id || "", {
        query: {
            enabled: !!id,
        },
    });

    useEffect(() => {
        if (songData?.value) {
            const song = songData.value;
            if (accessLoading) return;
            if (!access.some(item => item.bandId === song.bandId && item.canEditContent)) {
                toast({
                    title: t("songs.insufficient"),
                    description: t("songs.memberReadOnly"),
                    variant: "destructive",
                });
                navigate(`/app/songs/${song.id}`);
                return;
            }
            setForm({
                title: song.title,
                author: song.authors?.join(", ") || song.author || "",
                key: song.key || "",
                bpm: song.bpm
                    ? typeof song.bpm === "string"
                        ? parseInt(song.bpm)
                        : song.bpm
                    : null,
            });
            setCurrentBandId(song.bandId);
        }
    }, [songData, access, accessLoading, navigate, t, toast]);

    useEffect(() => {
        if (songError) {
            toast({
                title: t("common.error"),
                description: t("songs.loadSongFailed"),
                variant: "destructive",
            });
            navigate("/app/songs");
        }
    }, [songError, toast, navigate, t]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!form.title.trim()) {
            toast({
                title: t("songActions.validationError"),
                description: t("songActions.titleRequired"),
                variant: "destructive",
            });
            return;
        }

        if (isEditing && id) {
            if (!songData?.value || !canEditBand(songData.value.bandId)) {
                toast({ title: t("songs.insufficient"), variant: "destructive" });
                return;
            }
            updateSong.mutate({
                songId: id,
                data: {
                    title: form.title,
                    author: null,
                    key: form.key || null,
                    bpm: form.bpm,
                    authors: form.author.split(",").map(author => author.trim()).filter(Boolean),
                    status: songData?.value?.status ?? 0,
                    expectedContentVersion: songData?.value?.contentVersion ?? 0,
                },
            });
        } else {
            if (!currentBandId) {
                toast({ title: t("songs.noBand"), description: t("songs.createBandFirst"), variant: "destructive" });
                return;
            }
            createSong.mutate({
                data: {
                    bandId: currentBandId,
                    title: form.title,
                    author: null,
                    key: form.key || null,
                    bpm: form.bpm,
                    authors: form.author.split(",").map(author => author.trim()).filter(Boolean),
                },
            });
        }
    };

    return (
        <div className="container mx-auto py-8">
            <Card>
                <CardHeader>
                    <div className="flex items-center gap-4">
                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => navigator.go("/songs")}
                        >
                            <ArrowLeft className="h-4 w-4" />
                        </Button>
                        <div>
                            <CardTitle>
                                {isEditing ? t("songs.editSong") : t("songs.createSong")}
                            </CardTitle>
                            <CardDescription>
                                {isEditing
                                    ? t("songActions.updateHint")
                                    : t("songActions.createHint")}
                            </CardDescription>
                        </div>
                    </div>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-6">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            {!isEditing && <div className="space-y-2 md:col-span-2">
                                <Label>{t("common.band")} *</Label>
                                <Select value={currentBandId} onValueChange={setCurrentBandId}>
                                    <SelectTrigger><SelectValue placeholder={t("common.selectBand")} /></SelectTrigger>
                                    <SelectContent>{editableBands.map((band) => <SelectItem key={band.id} value={band.id}>{band.name}</SelectItem>)}</SelectContent>
                                </Select>
                            </div>}
                            <div className="space-y-2">
                                <Label htmlFor="title">{t("songs.fields.title")} *</Label>
                                <Input
                                    id="title"
                                    value={form.title}
                                    onChange={(e) =>
                                        setForm({
                                            ...form,
                                            title: e.target.value,
                                        })
                                    }
                                    placeholder="Amazing Grace"
                                    required
                                />
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="author">{t("songs.fields.authors")}</Label>
                                <Input
                                    id="author"
                                    value={form.author}
                                    onChange={(e) =>
                                        setForm({
                                            ...form,
                                            author: e.target.value,
                                        })
                                    }
                                    placeholder="John Newton"
                                />
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="key">{t("songs.fields.key")}</Label>
                                <Input
                                    id="key"
                                    value={form.key}
                                    onChange={(event) => setForm({ ...form, key: event.target.value })}
                                    list="musical-keys"
                                    placeholder="C, F#m, Bb"
                                />
                                <datalist id="musical-keys">
                                    {musicalKeys.map((key) => <option key={key} value={key} />)}
                                </datalist>
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="bpm">BPM</Label>
                                <Input
                                    id="bpm"
                                    type="number"
                                    value={form.bpm || ""}
                                    onChange={(e) =>
                                        setForm({
                                            ...form,
                                            bpm: e.target.value
                                                ? parseInt(e.target.value)
                                                : null,
                                        })
                                    }
                                    placeholder="120"
                                    min="20"
                                    max="400"
                                />
                            </div>
                        </div>

                        <div className="flex justify-end gap-4">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => navigator.go("/songs")}
                            >
                                {t("common.cancel")}
                            </Button>
                            <Button
                                type="submit"
                                disabled={
                                    createSong.isPending ||
                                    updateSong.isPending ||
                                    isLoadingSong
                                }
                            >
                                <Save className="mr-2 h-4 w-4" />
                                {t(isEditing ? "songActions.update" : "songActions.create")}
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>
            {isEditing && id && songData?.value && <SongSectionsPanel songId={id} canEdit={canEditBand(songData.value.bandId)} />}
        </div>
    );
}
