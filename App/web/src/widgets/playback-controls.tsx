import { useState } from "react";
import {
    Play,
    Upload,
    Settings,
    Loader2,
    Music,
    Recycle,
    Trash2,
} from "lucide-react";
import { Button } from "@/shared/ui/button";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/shared/ui/card";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from "@/shared/ui/dialog";
import { Label } from "@/shared/ui/label";
import { Input } from "@/shared/ui/input";
import { Badge } from "@/shared/ui/badge";
import { useToast } from "@/shared/ui/use-toast";
import {
    useGetTracks,
    useUploadTrack,
} from "@/lib/generated-api/playback-api/tracks";

interface PlaybackControlsProps {
    songId: string;
    songTitle: string;
    className?: string;
}

export function PlaybackControls({
    songId,
    songTitle,
    className,
}: PlaybackControlsProps) {
    const { toast } = useToast();

    const [uploadDialogOpen, setUploadDialogOpen] = useState(false);

    // Fetch playbacks for this song
    const { data: tracksData, isLoading: tracksLoading } = useGetTracks(songId);

    // Upload playback mutation
    const uploadTrack = useUploadTrack({
        mutation: {
            onSuccess: () => {
                toast({
                    title: "Success",
                    description: "Playback session uploaded successfully",
                });
                setUploadDialogOpen(false);
                setUploadForm({
                    files: [],
                });
                // Refetch playbacks
                // The query will automatically refetch due to React Query
            },
            onError: () => {
                toast({
                    title: "Error",
                    description: "Failed to upload playback session",
                    variant: "destructive",
                });
            },
        },
    });

    // Form state for upload dialog
    const [uploadForm, setUploadForm] = useState({
        files: [] as File[],
    });

    const tracks = tracksData?.value || [];

    const handleUpload = () => {
        if (uploadForm.files.length === 0) {
            toast({
                title: "Validation Error",
                description: "Please select at least one audio file",
                variant: "destructive",
            });
            return;
        }

        uploadTrack.mutate({
            songId: songId,
            data: {
                files: uploadForm.files,
            },
        });
    };

    const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
        const files = Array.from(e.target.files || []);
        setUploadForm((prev) => ({ ...prev, files }));
    };

    return (
        <div className={className}>
            <Card>
                <CardHeader>
                    <div className="flex justify-between items-center">
                        <div className="flex flex-col gap-1">
                            <CardTitle className="flex items-center gap-2">
                                <Music className="h-5 w-5" />
                                Playback
                            </CardTitle>
                            <CardDescription>
                                Multitrack playback for {songTitle}
                            </CardDescription>
                        </div>
                        <Button onClick={() => setUploadDialogOpen(true)}>
                            <Upload className="h-4 w-4 mr-2" />
                            Upload Audio Files
                        </Button>
                    </div>
                </CardHeader>
                <CardContent className="space-y-6">
                    {/* Loading State */}
                    {tracksLoading ? (
                        <div className="text-center py-8">
                            <Loader2 className="h-8 w-8 animate-spin mx-auto mb-4" />
                            Loading playbacks...
                        </div>
                    ) : tracks.length === 0 ? (
                        <div className="text-center py-12 border-2 border-dashed border-muted-foreground/25 rounded-lg">
                            <Music className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                            <h3 className="text-lg font-semibold mb-2">
                                No playbacks yet
                            </h3>
                            <p className="text-muted-foreground mb-4">
                                Upload multitrack files to create your first
                                playback
                            </p>
                            <Button onClick={() => setUploadDialogOpen(true)}>
                                <Upload className="h-4 w-4 mr-2" />
                                Upload Audio Files
                            </Button>
                        </div>
                    ) : (
                        <div className="grid gap-4">
                            <div>
                                <Button variant="outline" size="sm">
                                    <Play className="h-4 w-4 mr-2" />
                                    Play All
                                </Button>
                            </div>
                            {tracks.map((track: any) => (
                                <div
                                    key={track.id}
                                    className="border rounded-lg p-4 hover:bg-muted/50 cursor-pointer transition-colors"
                                >
                                    <div className="flex items-center justify-between">
                                        <div>
                                            <h4 className="font-medium">
                                                {track.name || "Track"}
                                            </h4>
                                            <div className="flex items-center gap-4 mt-2">
                                                <span className="text-sm text-muted-foreground">
                                                    {new Date(
                                                        track.createdAt,
                                                    ).toLocaleDateString()}
                                                </span>
                                            </div>
                                        </div>
                                        <div className="flex gap-2">
                                            <Button variant="outline" size="sm">
                                                <Play className="h-4 w-4 mr-2" />
                                                Play
                                            </Button>
                                            <Button
                                                variant="destructive"
                                                size="sm"
                                            >
                                                <Trash2 className="h-4 w-4" />
                                                Delete
                                            </Button>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>

            {/* Upload Tracks Dialog */}
            <Dialog open={uploadDialogOpen} onOpenChange={setUploadDialogOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Upload Audio Files</DialogTitle>
                        <DialogDescription>
                            Add multitrack audio files to {songTitle}.
                            Instruments will be automatically detected from
                            filenames.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4">
                        <div className="space-y-2">
                            <Label>Select Audio Files *</Label>
                            <Input
                                type="file"
                                multiple
                                accept="audio/*"
                                onChange={handleFileSelect}
                            />
                            {uploadForm.files.length > 0 && (
                                <p className="text-sm text-muted-foreground">
                                    {uploadForm.files.length} file
                                    {uploadForm.files.length !== 1
                                        ? "s"
                                        : ""}{" "}
                                    selected
                                </p>
                            )}
                            <p className="text-sm text-muted-foreground">
                                Upload drum, guitar, bass, vocals, and other
                                instrument tracks
                            </p>
                        </div>
                        <Button
                            className="w-full"
                            disabled={uploadTrack.isPending}
                            onClick={handleUpload}
                        >
                            {uploadTrack.isPending ? (
                                <>
                                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                    Uploading Files...
                                </>
                            ) : (
                                <>
                                    <Upload className="mr-2 h-4 w-4" />
                                    Upload Audio Files
                                </>
                            )}
                        </Button>
                    </div>
                </DialogContent>
            </Dialog>
        </div>
    );
}
