import { useState, useEffect } from "react";
import { useParams } from "react-router";
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
import { useTranslation } from "react-i18next";
import {
    useCreateSong,
    useUpdateSong,
    useGetSong,
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
    timeSignature: string;
    countInBars: number;
}

export function SongFormPage() {
    const navigator = useNavigator();
    const { id } = useParams();
    const { toast } = useToast();
    const { t } = useTranslation();
    const isEditing = !!id;
    const createSong = useCreateSong({
        mutation: {
            onSuccess: (response) => {
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
            onSuccess: () => {
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
        timeSignature: "4/4",
        countInBars: 2,
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
                navigator.go(`/songs/${song.id}`);
                return;
            }
            setForm({
                title: song.title,
                author: song.author || "",
                key: song.key || "",
                bpm: song.bpm
                    ? typeof song.bpm === "string"
                        ? parseInt(song.bpm)
                        : song.bpm
                    : null,
                timeSignature: song.timeSignatureTrack?.[0]
                    ? `${song.timeSignatureTrack[0].beats}/${song.timeSignatureTrack[0].beatUnit}`
                    : "4/4",
                countInBars: song.countInBars ?? 2,
            });
            setCurrentBandId(song.bandId);
        }
    }, [songData, access, accessLoading]);

    useEffect(() => {
        if (songError) {
            toast({
                title: t("common.error"),
                description: t("songs.loadSongFailed"),
                variant: "destructive",
            });
            navigator.go("/songs");
        }
    }, [songError, toast, navigator]);

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
            const [beats, beatUnit] = form.timeSignature.split("/").map(Number);
            updateSong.mutate({
                songId: id,
                data: {
                    title: form.title,
                    author: form.author || null,
                    key: form.key || null,
                    bpm: form.bpm,
                    authors: form.author ? [form.author] : [],
                    tempoTrack: form.bpm ? [{ bar: 1, bpm: form.bpm }] : [],
                    timeSignatureTrack: [{ bar: 1, beats, beatUnit }],
                    countInBars: form.countInBars,
                    status: songData?.value?.status ?? 0,
                    expectedContentVersion: songData?.value?.contentVersion ?? 0,
                },
            });
        } else {
            if (!currentBandId) {
                toast({ title: t("songs.noBand"), description: t("songs.createBandFirst"), variant: "destructive" });
                return;
            }
            const [beats, beatUnit] = form.timeSignature.split("/").map(Number);
            createSong.mutate({
                data: {
                    bandId: currentBandId,
                    title: form.title,
                    author: form.author || null,
                    key: form.key || null,
                    bpm: form.bpm,
                    authors: form.author ? [form.author] : [],
                    tempoTrack: form.bpm ? [{ bar: 1, bpm: form.bpm }] : [],
                    timeSignatureTrack: [{ bar: 1, beats, beatUnit }],
                    countInBars: form.countInBars,
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
                                <Label htmlFor="author">{t("songActions.authorArtist")}</Label>
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
                                    max="300"
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="timeSignature">{t("songs.fields.signature")}</Label>
                                <Input id="timeSignature" value={form.timeSignature} onChange={(e) => setForm({ ...form, timeSignature: e.target.value })} placeholder="4/4" />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="countInBars">{t("songs.fields.countIn")}</Label>
                                <Input id="countInBars" type="number" min="0" max="8" value={form.countInBars} onChange={(e) => setForm({ ...form, countInBars: Number(e.target.value) })} />
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
            {isEditing && id && <SongSectionsPanel songId={id} canEdit />}
        </div>
    );
}
