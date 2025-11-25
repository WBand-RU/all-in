import { useState, useEffect } from "react";
import { useParams } from "react-router";
import { useNavigator } from "@/services/navigator";
import { Save, ArrowLeft } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";
import { Textarea } from "@/shared/ui/textarea";
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
import {
    useCreateSong,
    useUpdateSong,
    useGetSong,
} from "@/lib/generated-api/song-api/songs";

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
    lyrics: string;
    chords: string;
    key: string;
    bpm: number | null;
}

export function SongFormPage() {
    const navigator = useNavigator();
    const { id } = useParams();
    const { toast } = useToast();
    const isEditing = !!id;
    const createSong = useCreateSong({
        mutation: {
            onSuccess: (response) => {
                toast({
                    title: "Success",
                    description: "Song created successfully",
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
                    title: "Error",
                    description: "Failed to create song",
                    variant: "destructive",
                });
            },
        },
    });

    const updateSong = useUpdateSong({
        mutation: {
            onSuccess: () => {
                toast({
                    title: "Success",
                    description: "Song updated successfully",
                });
                navigator.go("/songs");
            },
            onError: () => {
                toast({
                    title: "Error",
                    description: "Failed to update song",
                    variant: "destructive",
                });
            },
        },
    });
    const [form, setForm] = useState<SongForm>({
        title: "",
        author: "",
        lyrics: "",
        chords: "",
        key: "",
        bpm: null,
    });
    const [currentBandId] = useState("default"); // TODO: Get from context

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
            setForm({
                title: song.title,
                author: song.author || "",
                lyrics: song.lyrics || "",
                chords: song.chords || "",
                key: song.key || "",
                bpm: song.bpm
                    ? typeof song.bpm === "string"
                        ? parseInt(song.bpm)
                        : song.bpm
                    : null,
            });
        }
    }, [songData]);

    useEffect(() => {
        if (songError) {
            toast({
                title: "Error",
                description: "Failed to load song",
                variant: "destructive",
            });
            navigator.go("/songs");
        }
    }, [songError, toast, navigator]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!form.title.trim()) {
            toast({
                title: "Validation Error",
                description: "Song title is required",
                variant: "destructive",
            });
            return;
        }

        if (isEditing && id) {
            updateSong.mutate({
                songId: id,
                data: {
                    title: form.title,
                    author: form.author || null,
                    lyrics: form.lyrics || null,
                    chords: form.chords || null,
                    key: form.key || null,
                    bpm: form.bpm,
                },
            });
        } else {
            createSong.mutate({
                data: {
                    bandId: currentBandId,
                    title: form.title,
                    author: form.author || null,
                    lyrics: form.lyrics || null,
                    chords: form.chords || null,
                    key: form.key || null,
                    bpm: form.bpm,
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
                                {isEditing ? "Edit Song" : "New Song"}
                            </CardTitle>
                            <CardDescription>
                                {isEditing
                                    ? "Update song details"
                                    : "Add a new worship song to your collection"}
                            </CardDescription>
                        </div>
                    </div>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-6">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div className="space-y-2">
                                <Label htmlFor="title">Title *</Label>
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
                                <Label htmlFor="author">Author/Artist</Label>
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
                                <Label htmlFor="key">Key</Label>
                                <Select
                                    value={form.key}
                                    onValueChange={(value) =>
                                        setForm({ ...form, key: value })
                                    }
                                >
                                    <SelectTrigger id="key">
                                        <SelectValue placeholder="Select key" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {musicalKeys.map((key) => (
                                            <SelectItem key={key} value={key}>
                                                {key}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
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
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="lyrics">Lyrics</Label>
                            <Textarea
                                id="lyrics"
                                value={form.lyrics}
                                onChange={(e) =>
                                    setForm({ ...form, lyrics: e.target.value })
                                }
                                placeholder="Enter song lyrics..."
                                rows={10}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="chords">Chords</Label>
                            <Textarea
                                id="chords"
                                value={form.chords}
                                onChange={(e) =>
                                    setForm({ ...form, chords: e.target.value })
                                }
                                placeholder="Enter chord progression..."
                                rows={6}
                            />
                        </div>

                        <div className="flex justify-end gap-4">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => navigator.go("/songs")}
                            >
                                Cancel
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
                                {isEditing ? "Update" : "Create"} Song
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}
