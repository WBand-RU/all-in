import { useState, useEffect } from "react";
import { useParams } from "react-router";
import { Edit, ArrowLeft, Music, User, Hash, Activity } from "lucide-react";
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
import { useGetSong } from "@/lib/generated-api/song-api/songs";
import type { Song } from "@/lib/generated-api/song-api/models/song";

export function SongViewPage() {
    const navigator = useNavigator();
    const { id } = useParams();
    const { toast } = useToast();

    const { data: songData, isLoading, error } = useGetSong(id || "");

    const [song, setSong] = useState<Song | null>(null);

    useEffect(() => {
        if (songData?.value) {
            setSong(songData.value);
        }
    }, [songData]);

    useEffect(() => {
        if (error) {
            toast({
                title: "Error",
                description: "Failed to load song",
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
                        <div className="text-center">Loading song...</div>
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
                        <div className="text-center">Song not found</div>
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
                                <CardDescription>Song details</CardDescription>
                            </div>
                        </div>
                        <Button
                            variant="outline"
                            onClick={() =>
                                navigator.go(`/songs/${song.id}/edit`)
                            }
                        >
                            <Edit className="mr-2 h-4 w-4" />
                            Edit Song
                        </Button>
                    </div>
                </CardHeader>
                <CardContent className="space-y-6">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <div className="space-y-2">
                            <Label>Title</Label>
                            <div className="text-lg font-medium">
                                {song.title}
                            </div>
                        </div>

                        {song.author && (
                            <div className="space-y-2">
                                <Label className="flex items-center gap-2">
                                    <User className="h-4 w-4" />
                                    Author/Artist
                                </Label>
                                <div>{song.author}</div>
                            </div>
                        )}

                        {song.key && (
                            <div className="space-y-2">
                                <Label className="flex items-center gap-2">
                                    <Hash className="h-4 w-4" />
                                    Key
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

                    {song.lyrics && (
                        <div className="space-y-2">
                            <Label>Lyrics</Label>
                            <div className="bg-muted p-4 rounded-md whitespace-pre-wrap">
                                {song.lyrics}
                            </div>
                        </div>
                    )}

                    {song.chords && (
                        <div className="space-y-2">
                            <Label>Chords</Label>
                            <div className="bg-muted p-4 rounded-md whitespace-pre-wrap font-mono">
                                {song.chords}
                            </div>
                        </div>
                    )}

                    <div className="text-sm text-muted-foreground">
                        Created {new Date(song.createdAt).toLocaleDateString()}
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
