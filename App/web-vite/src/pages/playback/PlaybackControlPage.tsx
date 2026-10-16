import { useState, useEffect, useRef } from "react";
import { useParams } from "react-router";
import {
    Play,
    Pause,
    SkipBack,
    SkipForward,
    Volume2,
    VolumeX,
    Maximize2,
    Upload,
    Settings,
    Loader2,
    Plus,
    Edit,
    Trash2,
} from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Slider } from "@/shared/ui/slider";
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
    DialogFooter,
} from "@/shared/ui/dialog";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/shared/ui/alert-dialog";
import { Label } from "@/shared/ui/label";
import { Input } from "@/shared/ui/input";
import { Badge } from "@/shared/ui/badge";
import { useToast } from "@/shared/ui/use-toast";
import {
    useGetTracks,
    useUploadTrack,
} from "@/lib/generated-api/playback-api/tracks";
import {
    useGetMixerPresets,
    useCreateMixerPreset,
    useUpdateMixerPreset,
    useDeleteMixerPreset,
} from "@/lib/generated-api/playback-api/mixer-presets";
import { useGetSong } from "@/lib/generated-api/song-api/songs";
import type {
    MixerPreset,
    CreateMixerPresetRequest,
    UpdateMixerPresetRequest,
} from "@/lib/generated-api/playback-api/models";

export function PlaybackControlPage() {
    const { id: songId } = useParams();
    const { toast } = useToast();
    const audioRefs = useRef<{ [key: string]: HTMLAudioElement }>({});

    const [isPlaying, setIsPlaying] = useState(false);
    const [currentTime, setCurrentTime] = useState(0);
    const [duration] = useState(240); // Using fixed duration from playback
    const [uploadDialogOpen, setUploadDialogOpen] = useState(false);
    const [presetDialogOpen, setPresetDialogOpen] = useState(false);
    const [createPresetDialogOpen, setCreatePresetDialogOpen] = useState(false);
    const [editPresetDialogOpen, setEditPresetDialogOpen] = useState(false);
    const [deletePresetDialogOpen, setDeletePresetDialogOpen] = useState(false);
    const [isUploading, setIsUploading] = useState(false);
    const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
    const [trackSettings, setTrackSettings] = useState<{
        [trackId: string]: {
            volume: number;
            pan: number;
            muted: boolean;
            solo: boolean;
        };
    }>({});
    const [currentMixerPresetId, setCurrentMixerPresetId] = useState<
        string | undefined
    >();
    const [editingPreset, setEditingPreset] = useState<MixerPreset | null>(
        null,
    );
    const [presetName, setPresetName] = useState("");

    // API hooks
    const { data: tracksData, refetch: refetchTracks } = useGetTracks(
        songId || "",
    );
    const { data: presetsData, refetch: refetchPresets } = useGetMixerPresets(
        songId || "",
    );
    const { data: songData } = useGetSong(songId || "");
    const uploadTrackMutation = useUploadTrack();
    const createPresetMutation = useCreateMixerPreset();
    const updatePresetMutation = useUpdateMixerPreset();
    const deletePresetMutation = useDeleteMixerPreset();

    const tracks = tracksData?.value || [];
    const presets = presetsData?.value || [];
    const song = songData?.value;

    useEffect(() => {
        // Update time periodically when playing
        const interval = setInterval(() => {
            if (isPlaying && audioRefs.current.track1) {
                setCurrentTime(audioRefs.current.track1.currentTime);
            }
        }, 100);

        return () => clearInterval(interval);
    }, [isPlaying]);

    const handlePlayPause = () => {
        setIsPlaying(!isPlaying);
        // In real implementation, control audio playback
    };

    const handleSeek = (value: number[]) => {
        const time = value[0];
        setCurrentTime(time);
        // Sync all audio elements to this time
        Object.values(audioRefs.current).forEach((audio) => {
            audio.currentTime = time;
        });
    };

    const handleTrackVolumeChange = (trackId: string, volume: number) => {
        setTrackSettings((prev) => ({
            ...prev,
            [trackId]: {
                ...prev[trackId],
                volume,
            },
        }));
        toast({
            title: "Track Updated",
            description: `Volume changed to ${(volume * 100).toFixed(0)}%`,
        });
    };

    const handleTrackPanChange = (trackId: string, pan: number) => {
        setTrackSettings((prev) => ({
            ...prev,
            [trackId]: {
                ...prev[trackId],
                pan,
            },
        }));
        toast({
            title: "Track Updated",
            description: `Pan changed to ${pan >= 0 ? `R${(pan * 100).toFixed(0)}` : `L${Math.abs(pan * 100).toFixed(0)}`}`,
        });
    };

    const handleTrackMute = (trackId: string) => {
        setTrackSettings((prev) => ({
            ...prev,
            [trackId]: {
                ...prev[trackId],
                muted: !prev[trackId]?.muted,
            },
        }));
        toast({
            title: "Track Updated",
            description: "Track mute toggled",
        });
    };

    const handleTrackSolo = (trackId: string) => {
        setTrackSettings((prev) => ({
            ...prev,
            [trackId]: {
                ...prev[trackId],
                solo: !prev[trackId]?.solo,
            },
        }));
        toast({
            title: "Track Updated",
            description: "Track solo toggled",
        });
    };

    const handleFileUpload = async () => {
        if (!selectedFiles.length || !songId) return;

        setIsUploading(true);
        try {
            await uploadTrackMutation.mutateAsync({
                songId,
                data: { files: selectedFiles },
            });

            toast({
                title: "Upload Successful",
                description: `${selectedFiles.length} track(s) uploaded successfully`,
            });

            setSelectedFiles([]);
            setUploadDialogOpen(false);
            refetchTracks();
        } catch (error) {
            toast({
                title: "Upload Failed",
                description: "Failed to upload tracks",
                variant: "destructive",
            });
        } finally {
            setIsUploading(false);
        }
    };

    const handleCreatePreset = async () => {
        if (!presetName.trim() || !songId) return;

        const request: CreateMixerPresetRequest = {
            name: presetName,
            // For now, create empty preset - in real implementation, capture current track states
            output: { volume: 1, pan: 0, muted: false, solo: false },
            tracks: tracks.map(() => ({
                volume: 1,
                pan: 0,
                muted: false,
                solo: false,
            })),
        };

        try {
            await createPresetMutation.mutateAsync({
                songId,
                data: request,
            });

            toast({
                title: "Preset Created",
                description: `Preset "${presetName}" created successfully`,
            });

            setPresetName("");
            setCreatePresetDialogOpen(false);
            refetchPresets();
        } catch (error) {
            toast({
                title: "Creation Failed",
                description: "Failed to create preset",
                variant: "destructive",
            });
        }
    };

    const handleUpdatePreset = async () => {
        if (!editingPreset || !presetName.trim() || !songId) return;

        const request: UpdateMixerPresetRequest = {
            name: presetName,
        };

        try {
            await updatePresetMutation.mutateAsync({
                songId,
                id: editingPreset.id,
                data: request,
            });

            toast({
                title: "Preset Updated",
                description: `Preset "${presetName}" updated successfully`,
            });

            setPresetName("");
            setEditingPreset(null);
            setEditPresetDialogOpen(false);
            refetchPresets();
        } catch (error) {
            toast({
                title: "Update Failed",
                description: "Failed to update preset",
                variant: "destructive",
            });
        }
    };

    const handleDeletePreset = async () => {
        if (!editingPreset || !songId) return;

        try {
            await deletePresetMutation.mutateAsync({
                songId,
                id: editingPreset.id,
            });

            toast({
                title: "Preset Deleted",
                description: `Preset "${editingPreset.name}" deleted successfully`,
            });

            setEditingPreset(null);
            setDeletePresetDialogOpen(false);
            refetchPresets();
        } catch (error) {
            toast({
                title: "Deletion Failed",
                description: "Failed to delete preset",
                variant: "destructive",
            });
        }
    };

    const applyPreset = (preset: MixerPreset) => {
        setCurrentMixerPresetId(preset.id);
        toast({
            title: "Preset Applied",
            description: `Applied "${preset.name}" mixer preset`,
        });
        setPresetDialogOpen(false);
    };

    const openEditPreset = (preset: MixerPreset) => {
        setEditingPreset(preset);
        setPresetName(preset.name);
        setEditPresetDialogOpen(true);
    };

    const openDeletePreset = (preset: MixerPreset) => {
        setEditingPreset(preset);
        setDeletePresetDialogOpen(true);
    };

    const formatTime = (seconds: number) => {
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins}:${secs.toString().padStart(2, "0")}`;
    };

    const getInstrumentColor = (type: string) => {
        const colors: { [key: string]: string } = {
            Vocals: "bg-purple-500",
            Guitar: "bg-green-500",
            Piano: "bg-blue-500",
            Drums: "bg-orange-500",
            Bass: "bg-red-500",
        };
        return colors[type] || "bg-gray-500";
    };

    return (
        <div className="container mx-auto py-8">
            <div className="grid gap-6">
                {/* Main Playback Control */}
                <Card>
                    <CardHeader>
                        <div className="flex justify-between items-start">
                            <div>
                                <CardTitle>{song?.title}</CardTitle>
                                <CardDescription>
                                    Multitrack Playback Control
                                </CardDescription>
                            </div>
                            <div className="flex gap-2">
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() => setUploadDialogOpen(true)}
                                >
                                    <Upload className="h-4 w-4 mr-2" />
                                    Upload Tracks
                                </Button>
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() => setPresetDialogOpen(true)}
                                >
                                    <Settings className="h-4 w-4 mr-2" />
                                    Presets
                                </Button>
                                <Button variant="outline" size="sm">
                                    <Maximize2 className="h-4 w-4" />
                                </Button>
                            </div>
                        </div>
                    </CardHeader>
                    <CardContent className="space-y-6">
                        {/* Playback Controls */}
                        <div className="flex items-center justify-center gap-4">
                            <Button variant="outline" size="icon">
                                <SkipBack className="h-4 w-4" />
                            </Button>
                            <Button
                                size="lg"
                                onClick={handlePlayPause}
                                className="h-14 w-14 rounded-full"
                            >
                                {isPlaying ? (
                                    <Pause className="h-6 w-6" />
                                ) : (
                                    <Play className="h-6 w-6 ml-1" />
                                )}
                            </Button>
                            <Button variant="outline" size="icon">
                                <SkipForward className="h-4 w-4" />
                            </Button>
                        </div>

                        {/* Timeline */}
                        <div className="space-y-2">
                            <Slider
                                value={[currentTime]}
                                max={duration || 240}
                                step={0.1}
                                onValueChange={handleSeek}
                                className="w-full"
                            />
                            <div className="flex justify-between text-sm text-muted-foreground">
                                <span>{formatTime(currentTime)}</span>
                                <span>{formatTime(duration || 240)}</span>
                            </div>
                        </div>

                        {/* Tracks List */}
                        <div className="space-y-4">
                            <h3 className="text-lg font-semibold">
                                Tracks ({tracks.length})
                            </h3>
                            {tracks.length === 0 ? (
                                <div className="text-center py-8 text-muted-foreground">
                                    No tracks uploaded yet. Click "Upload
                                    Tracks" to get started.
                                </div>
                            ) : (
                                <div className="grid gap-4">
                                    {tracks.map((track) => {
                                        const settings = trackSettings[
                                            track.id
                                        ] || {
                                            volume: 0.8,
                                            pan: 0,
                                            muted: false,
                                            solo: false,
                                        };
                                        return (
                                            <div
                                                key={track.id}
                                                className={`p-4 border rounded-lg ${
                                                    settings.solo
                                                        ? "ring-2 ring-primary"
                                                        : ""
                                                }`}
                                            >
                                                <div className="flex items-center gap-4">
                                                    <div
                                                        className={`w-3 h-12 rounded-full ${getInstrumentColor(
                                                            track.instrument,
                                                        )}`}
                                                    />
                                                    <div className="flex-1 space-y-3">
                                                        <div className="flex items-center justify-between">
                                                            <div className="flex items-center gap-3">
                                                                <span className="font-medium">
                                                                    {
                                                                        track.instrument
                                                                    }
                                                                </span>
                                                                <Badge variant="secondary">
                                                                    {
                                                                        track.instrument
                                                                    }
                                                                </Badge>
                                                            </div>
                                                            <div className="flex gap-2">
                                                                <Button
                                                                    variant={
                                                                        settings.solo
                                                                            ? "default"
                                                                            : "outline"
                                                                    }
                                                                    size="sm"
                                                                    onClick={() =>
                                                                        handleTrackSolo(
                                                                            track.id,
                                                                        )
                                                                    }
                                                                >
                                                                    Solo
                                                                </Button>
                                                                <Button
                                                                    variant={
                                                                        settings.muted
                                                                            ? "destructive"
                                                                            : "outline"
                                                                    }
                                                                    size="icon"
                                                                    onClick={() =>
                                                                        handleTrackMute(
                                                                            track.id,
                                                                        )
                                                                    }
                                                                >
                                                                    {settings.muted ? (
                                                                        <VolumeX className="h-4 w-4" />
                                                                    ) : (
                                                                        <Volume2 className="h-4 w-4" />
                                                                    )}
                                                                </Button>
                                                            </div>
                                                        </div>

                                                        <div className="grid grid-cols-2 gap-4">
                                                            <div className="space-y-2">
                                                                <div className="flex justify-between">
                                                                    <Label className="text-sm">
                                                                        Volume
                                                                    </Label>
                                                                    <span className="text-sm text-muted-foreground">
                                                                        {Math.round(
                                                                            settings.volume *
                                                                                100,
                                                                        )}
                                                                        %
                                                                    </span>
                                                                </div>
                                                                <Slider
                                                                    value={[
                                                                        settings.volume,
                                                                    ]}
                                                                    max={1}
                                                                    step={0.01}
                                                                    onValueChange={(
                                                                        value,
                                                                    ) =>
                                                                        handleTrackVolumeChange(
                                                                            track.id,
                                                                            value[0],
                                                                        )
                                                                    }
                                                                    disabled={
                                                                        settings.muted
                                                                    }
                                                                />
                                                            </div>

                                                            <div className="space-y-2">
                                                                <div className="flex justify-between">
                                                                    <Label className="text-sm">
                                                                        Pan
                                                                    </Label>
                                                                    <span className="text-sm text-muted-foreground">
                                                                        {settings.pan ===
                                                                        0
                                                                            ? "C"
                                                                            : settings.pan <
                                                                                0
                                                                              ? `L${Math.abs(
                                                                                    Math.round(
                                                                                        settings.pan *
                                                                                            100,
                                                                                    ),
                                                                                )}`
                                                                              : `R${Math.round(settings.pan * 100)}`}
                                                                    </span>
                                                                </div>
                                                                <Slider
                                                                    value={[
                                                                        settings.pan,
                                                                    ]}
                                                                    min={-1}
                                                                    max={1}
                                                                    step={0.01}
                                                                    onValueChange={(
                                                                        value,
                                                                    ) =>
                                                                        handleTrackPanChange(
                                                                            track.id,
                                                                            value[0],
                                                                        )
                                                                    }
                                                                    disabled={
                                                                        settings.muted
                                                                    }
                                                                />
                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        );
                                    })}
                                </div>
                            )}
                        </div>
                    </CardContent>
                </Card>
            </div>

            {/* Upload Tracks Dialog */}
            <Dialog open={uploadDialogOpen} onOpenChange={setUploadDialogOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Upload Multitrack Files</DialogTitle>
                        <DialogDescription>
                            Upload individual track files for this song
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4">
                        <div className="space-y-2">
                            <Label>Select Audio Files</Label>
                            <Input
                                type="file"
                                multiple
                                accept="audio/*"
                                onChange={(e) => {
                                    const files = Array.from(
                                        e.target.files || [],
                                    );
                                    setSelectedFiles(files);
                                }}
                            />
                            {selectedFiles.length > 0 && (
                                <div className="text-sm text-muted-foreground">
                                    {selectedFiles.length} file(s) selected
                                </div>
                            )}
                        </div>
                        <DialogFooter>
                            <Button
                                variant="outline"
                                onClick={() => {
                                    setSelectedFiles([]);
                                    setUploadDialogOpen(false);
                                }}
                            >
                                Cancel
                            </Button>
                            <Button
                                disabled={
                                    isUploading || selectedFiles.length === 0
                                }
                                onClick={handleFileUpload}
                            >
                                {isUploading ? (
                                    <>
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                        Uploading...
                                    </>
                                ) : (
                                    <>
                                        <Upload className="mr-2 h-4 w-4" />
                                        Upload {selectedFiles.length} Track
                                        {selectedFiles.length !== 1 ? "s" : ""}
                                    </>
                                )}
                            </Button>
                        </DialogFooter>
                    </div>
                </DialogContent>
            </Dialog>

            {/* Mixer Presets Dialog */}
            <Dialog open={presetDialogOpen} onOpenChange={setPresetDialogOpen}>
                <DialogContent className="max-w-2xl">
                    <DialogHeader>
                        <div className="flex justify-between items-center">
                            <div>
                                <DialogTitle>Mixer Presets</DialogTitle>
                                <DialogDescription>
                                    Manage and apply mixer configurations
                                </DialogDescription>
                            </div>
                            <Button
                                onClick={() => {
                                    setPresetDialogOpen(false);
                                    setCreatePresetDialogOpen(true);
                                }}
                            >
                                <Plus className="mr-2 h-4 w-4" />
                                Create Preset
                            </Button>
                        </div>
                    </DialogHeader>
                    <div className="space-y-2 max-h-96 overflow-y-auto">
                        {presets.length === 0 ? (
                            <div className="text-center py-8 text-muted-foreground">
                                No presets created yet. Click "Create Preset" to
                                get started.
                            </div>
                        ) : (
                            presets.map((preset: MixerPreset) => (
                                <div
                                    key={preset.id}
                                    className={`flex items-center justify-between p-3 border rounded-lg ${
                                        currentMixerPresetId === preset.id
                                            ? "ring-2 ring-primary"
                                            : ""
                                    }`}
                                >
                                    <div className="flex items-center gap-3">
                                        <Settings className="h-4 w-4" />
                                        <span className="font-medium">
                                            {preset.name}
                                        </span>
                                        {currentMixerPresetId === preset.id && (
                                            <Badge variant="secondary">
                                                Active
                                            </Badge>
                                        )}
                                    </div>
                                    <div className="flex gap-2">
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => applyPreset(preset)}
                                        >
                                            Apply
                                        </Button>
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() =>
                                                openEditPreset(preset)
                                            }
                                        >
                                            <Edit className="h-4 w-4" />
                                        </Button>
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() =>
                                                openDeletePreset(preset)
                                            }
                                        >
                                            <Trash2 className="h-4 w-4" />
                                        </Button>
                                    </div>
                                </div>
                            ))
                        )}
                    </div>
                </DialogContent>
            </Dialog>

            {/* Create Preset Dialog */}
            <Dialog
                open={createPresetDialogOpen}
                onOpenChange={setCreatePresetDialogOpen}
            >
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Create Mixer Preset</DialogTitle>
                        <DialogDescription>
                            Create a new mixer configuration preset
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4">
                        <div className="space-y-2">
                            <Label>Preset Name</Label>
                            <Input
                                value={presetName}
                                onChange={(e) => setPresetName(e.target.value)}
                                placeholder="Enter preset name"
                            />
                        </div>
                    </div>
                    <DialogFooter>
                        <Button
                            variant="outline"
                            onClick={() => {
                                setPresetName("");
                                setCreatePresetDialogOpen(false);
                            }}
                        >
                            Cancel
                        </Button>
                        <Button
                            onClick={handleCreatePreset}
                            disabled={!presetName.trim()}
                        >
                            Create Preset
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* Edit Preset Dialog */}
            <Dialog
                open={editPresetDialogOpen}
                onOpenChange={setEditPresetDialogOpen}
            >
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Edit Mixer Preset</DialogTitle>
                        <DialogDescription>
                            Update the preset name
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4">
                        <div className="space-y-2">
                            <Label>Preset Name</Label>
                            <Input
                                value={presetName}
                                onChange={(e) => setPresetName(e.target.value)}
                                placeholder="Enter preset name"
                            />
                        </div>
                    </div>
                    <DialogFooter>
                        <Button
                            variant="outline"
                            onClick={() => {
                                setPresetName("");
                                setEditingPreset(null);
                                setEditPresetDialogOpen(false);
                            }}
                        >
                            Cancel
                        </Button>
                        <Button
                            onClick={handleUpdatePreset}
                            disabled={!presetName.trim()}
                        >
                            Update Preset
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* Delete Preset Dialog */}
            <AlertDialog
                open={deletePresetDialogOpen}
                onOpenChange={setDeletePresetDialogOpen}
            >
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Delete Mixer Preset</AlertDialogTitle>
                        <AlertDialogDescription>
                            Are you sure you want to delete "
                            {editingPreset?.name}"? This action cannot be
                            undone.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel
                            onClick={() => {
                                setEditingPreset(null);
                                setDeletePresetDialogOpen(false);
                            }}
                        >
                            Cancel
                        </AlertDialogCancel>
                        <AlertDialogAction onClick={handleDeletePreset}>
                            Delete
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </div>
    );
}
