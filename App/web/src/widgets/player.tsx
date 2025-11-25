import { useState, useEffect, useRef } from "react";
import {
    Play,
    Pause,
    SkipBack,
    SkipForward,
    Volume2,
    VolumeX,
    Upload,
    Settings,
    Loader2,
    Plus,
    Edit,
    Trash2,
    Download,
    Music,
} from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Label } from "@/shared/ui/label";
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
import type { MixerPreset } from "@/lib/generated-api/playback-api/models";

interface PlayerProps {
    songId: string;
    songTitle: string;
    className?: string;
}

export function Player({ songId, songTitle, className }: PlayerProps) {
    const { toast } = useToast();

    // Playback state
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

    // Audio refs for synchronized playback
    const audioRefs = useRef<{ [key: string]: HTMLAudioElement }>({});

    // API hooks
    const { data: tracksData, refetch: refetchTracks } = useGetTracks(songId);
    const { data: presetsData } = useGetMixerPresets(songId);
    const uploadTrack = useUploadTrack({
        mutation: {
            onSuccess: () => {
                toast({
                    title: "Success",
                    description: "Playback session uploaded successfully",
                });
                setUploadDialogOpen(false);
                setSelectedFiles([]);
                refetchTracks();
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
    const createMixerPreset = useCreateMixerPreset({
        mutation: {
            onSuccess: () => {
                toast({
                    title: "Success",
                    description: "Mixer preset created successfully",
                });
                setCreatePresetDialogOpen(false);
                setPresetName("");
            },
            onError: () => {
                toast({
                    title: "Error",
                    description: "Failed to create mixer preset",
                    variant: "destructive",
                });
            },
        },
    });
    const updateMixerPreset = useUpdateMixerPreset({
        mutation: {
            onSuccess: () => {
                toast({
                    title: "Success",
                    description: "Mixer preset updated successfully",
                });
                setEditPresetDialogOpen(false);
                setPresetName("");
                setEditingPreset(null);
            },
            onError: () => {
                toast({
                    title: "Error",
                    description: "Failed to update mixer preset",
                    variant: "destructive",
                });
            },
        },
    });
    const deleteMixerPreset = useDeleteMixerPreset({
        mutation: {
            onSuccess: () => {
                toast({
                    title: "Success",
                    description: "Mixer preset deleted successfully",
                });
                setDeletePresetDialogOpen(false);
                setEditingPreset(null);
            },
            onError: () => {
                toast({
                    title: "Error",
                    description: "Failed to delete mixer preset",
                    variant: "destructive",
                });
            },
        },
    });

    const tracks = tracksData?.value || [];
    const presets = presetsData?.value || [];

    // Initialize audio elements when tracks change
    useEffect(() => {
        // Cleanup removed tracks
        Object.keys(audioRefs.current).forEach((trackId) => {
            if (!tracks.find((t) => t.id === trackId)) {
                const audio = audioRefs.current[trackId];
                if (audio) {
                    audio.pause();
                    audio.src = "";
                }
                delete audioRefs.current[trackId];
            }
        });
    }, [tracks]);

    // Update audio volumes when settings change
    useEffect(() => {
        tracks.forEach((track) => {
            const audio = audioRefs.current[track.id];
            const settings = trackSettings[track.id];

            if (audio && settings) {
                // Apply solo logic
                const hasActiveSolo = tracks.some(
                    (t) => trackSettings[t.id]?.solo,
                );
                const shouldBeAudible =
                    !settings.muted && (!hasActiveSolo || settings.solo);

                audio.volume = shouldBeAudible ? settings.volume : 0;
            }
        });
    }, [trackSettings, tracks]);

    // Cleanup audio elements on unmount
    useEffect(() => {
        return () => {
            Object.values(audioRefs.current).forEach((audio) => {
                if (audio) {
                    audio.pause();
                    audio.src = "";
                }
            });
            audioRefs.current = {};
        };
    }, []);

    // Helper functions
    const formatTime = (seconds: number) => {
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins}:${secs.toString().padStart(2, "0")}`;
    };

    const getInstrumentColor = (instrument: string) => {
        const colors: { [key: string]: string } = {
            Piano: "bg-blue-500",
            Drums: "bg-orange-500",
            Bass: "bg-red-500",
        };
        return colors[instrument] || "bg-gray-500";
    };

    // Playback handlers
    const handlePlayPause = () => {
        if (isPlaying) {
            // Pause all tracks
            Object.values(audioRefs.current).forEach((audio) => {
                if (audio) audio.pause();
            });
            setIsPlaying(false);
        } else {
            // Play all tracks synchronously
            const playPromises = Object.values(audioRefs.current).map(
                (audio) => {
                    if (audio) {
                        audio.currentTime = currentTime;
                        return audio.play();
                    }
                    return Promise.resolve();
                },
            );

            Promise.all(playPromises)
                .then(() => {
                    setIsPlaying(true);
                })
                .catch((error) => {
                    console.error("Error playing tracks:", error);
                    toast({
                        title: "Playback Error",
                        description: "Failed to start playback",
                        variant: "destructive",
                    });
                });
        }
    };

    const handleSeek = (value: number[]) => {
        const newTime = value[0];
        setCurrentTime(newTime);

        // Seek all tracks to the same position if they're playing
        Object.values(audioRefs.current).forEach((audio) => {
            if (audio) {
                audio.currentTime = newTime;
            }
        });
    };

    // Track control handlers
    const handleTrackVolumeChange = (trackId: string, volume: number) => {
        setTrackSettings((prev) => ({
            ...prev,
            [trackId]: { ...prev[trackId], volume },
        }));

        // Apply volume to audio element
        const audio = audioRefs.current[trackId];
        if (audio) {
            const settings = trackSettings[trackId] || {
                volume: 0.8,
                muted: false,
            };
            audio.volume = settings.muted ? 0 : volume;
        }
    };

    const handleTrackPanChange = (trackId: string, pan: number) => {
        setTrackSettings((prev) => ({
            ...prev,
            [trackId]: { ...prev[trackId], pan },
        }));

        // Note: HTML5 Audio doesn't support pan directly, would need Web Audio API for that
    };

    const handleTrackMute = (trackId: string) => {
        setTrackSettings((prev) => {
            const currentSettings = prev[trackId] || {
                volume: 0.8,
                muted: false,
            };
            const newMuted = !currentSettings.muted;
            const newSettings = { ...currentSettings, muted: newMuted };

            // Apply mute to audio element
            const audio = audioRefs.current[trackId];
            if (audio) {
                audio.volume = newMuted ? 0 : newSettings.volume || 0.8;
            }

            return {
                ...prev,
                [trackId]: newSettings,
            };
        });
    };

    const handleTrackSolo = (trackId: string) => {
        setTrackSettings((prev) => {
            const currentSettings = prev[trackId] || { solo: false };
            const newSolo = !currentSettings.solo;

            // Update solo state for this track
            const newSettings = { ...currentSettings, solo: newSolo };

            // If soloing, mute all other tracks; if unsoloing, unmute all
            const updatedSettings = { ...prev, [trackId]: newSettings };

            // Apply solo logic to all tracks
            tracks.forEach((track) => {
                const trackSettings = updatedSettings[track.id] || {
                    volume: 0.8,
                    muted: false,
                    solo: false,
                };
                const audio = audioRefs.current[track.id];

                if (audio) {
                    // A track should be audible if:
                    // 1. It's not muted AND (no solo is active OR this track is soloed)
                    const hasActiveSolo = tracks.some(
                        (t) => updatedSettings[t.id]?.solo,
                    );
                    const shouldBeAudible =
                        !trackSettings.muted &&
                        (!hasActiveSolo || trackSettings.solo);

                    audio.volume = shouldBeAudible
                        ? trackSettings.volume || 0.8
                        : 0;
                }
            });

            return updatedSettings;
        });
    };

    const handleTrackDownload = async (trackId: string, trackName: string) => {
        try {
            // Call the download endpoint
            const response = await fetch(
                `/playback-api/songs/${songId}/tracks/${trackId}/download`,
                {
                    method: "GET",
                    headers: {
                        Authorization: `Bearer ${localStorage.getItem("access_token")}`, // Assuming token is stored here
                    },
                },
            );

            if (!response.ok) {
                throw new Error("Download failed");
            }

            // Get the pre-signed URL from response
            const data = await response.json();
            const downloadUrl = data.value;

            // Create a temporary link and trigger download
            const link = document.createElement("a");
            link.href = downloadUrl;
            link.download = `${trackName}.wav`; // Assuming WAV format, adjust as needed
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            toast({
                title: "Download Started",
                description: `Downloading ${trackName}...`,
            });
        } catch (error) {
            console.error("Download error:", error);
            toast({
                title: "Download Failed",
                description: "Failed to download the track",
                variant: "destructive",
            });
        }
    };

    // File upload handler
    const handleFileUpload = () => {
        setIsUploading(true);
        uploadTrack.mutate({
            songId: songId,
            data: {
                files: selectedFiles,
            },
        });
        setIsUploading(false);
    };

    // Preset handlers
    const applyPreset = (preset: MixerPreset) => {
        if (preset.tracks) {
            // Convert preset tracks to trackSettings format
            const newSettings: { [trackId: string]: any } = {};
            preset.tracks.forEach((trackPreset, index) => {
                const trackId = tracks[index]?.id;
                if (trackId) {
                    newSettings[trackId] = {
                        volume: trackPreset.volume || 0.8,
                        pan: trackPreset.pan || 0,
                        muted: trackPreset.muted || false,
                        solo: trackPreset.solo || false,
                    };
                }
            });
            setTrackSettings(newSettings);
        }
        setCurrentMixerPresetId(preset.id);
        toast({
            title: "Preset Applied",
            description: `Applied preset "${preset.name}"`,
        });
    };

    const openEditPreset = (preset: MixerPreset) => {
        setEditingPreset(preset);
        setPresetName(preset.name);
        setPresetDialogOpen(false);
        setEditPresetDialogOpen(true);
    };

    const openDeletePreset = (preset: MixerPreset) => {
        setEditingPreset(preset);
        setPresetDialogOpen(false);
        setDeletePresetDialogOpen(true);
    };

    const handleCreatePreset = () => {
        // Convert trackSettings to tracks array format
        const tracksArray = tracks.map((track) => ({
            volume: trackSettings[track.id]?.volume || 0.8,
            pan: trackSettings[track.id]?.pan || 0,
            muted: trackSettings[track.id]?.muted || false,
            solo: trackSettings[track.id]?.solo || false,
        }));

        createMixerPreset.mutate({
            songId: songId,
            data: {
                name: presetName,
                tracks: tracksArray,
            },
        });
    };

    const handleUpdatePreset = () => {
        if (editingPreset) {
            // Convert trackSettings to tracks array format
            const tracksArray = tracks.map((track) => ({
                volume: trackSettings[track.id]?.volume || 0.8,
                pan: trackSettings[track.id]?.pan || 0,
                muted: trackSettings[track.id]?.muted || false,
                solo: trackSettings[track.id]?.solo || false,
            }));

            updateMixerPreset.mutate({
                songId: songId,
                id: editingPreset.id,
                data: {
                    name: presetName,
                    tracks: tracksArray,
                },
            });
        }
    };

    const handleDeletePreset = () => {
        if (editingPreset) {
            deleteMixerPreset.mutate({
                songId: songId,
                id: editingPreset.id,
            });
        }
    };

    return (
        <div className={className}>
            <Card>
                <CardHeader>
                    <div className="flex justify-between items-start">
                        <div>
                            <CardTitle className="flex items-center gap-2">
                                <Music className="h-5 w-5" />
                                Playback & Mixer
                            </CardTitle>
                            <CardDescription>
                                Multitrack playback control for {songTitle}
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
                                No tracks uploaded yet. Click "Upload Tracks" to
                                get started.
                            </div>
                        ) : (
                            <div className="grid gap-4">
                                {tracks.map((track: any) => {
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
                                                                variant="outline"
                                                                size="icon"
                                                                onClick={() =>
                                                                    handleTrackDownload(
                                                                        track.id,
                                                                        track.instrument,
                                                                    )
                                                                }
                                                            >
                                                                <Download className="h-4 w-4" />
                                                            </Button>
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

            {/* Upload Tracks Dialog */}
            <Dialog open={uploadDialogOpen} onOpenChange={setUploadDialogOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Upload Multitrack Files</DialogTitle>
                        <DialogDescription>
                            Upload individual track files for {songTitle}.
                            Instruments will be automatically detected from
                            filenames.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4">
                        <div className="space-y-2">
                            <Label>Select Audio Files</Label>
                            <input
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
                            <input
                                value={presetName}
                                onChange={(e) => setPresetName(e.target.value)}
                                placeholder="Enter preset name"
                                className="w-full px-3 py-2 border border-input rounded-md"
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
                            <input
                                value={presetName}
                                onChange={(e) => setPresetName(e.target.value)}
                                placeholder="Enter preset name"
                                className="w-full px-3 py-2 border border-input rounded-md"
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

            {/* Hidden Audio Elements for Synchronized Playback */}
            {tracks.map((track) => (
                <audio
                    key={track.id}
                    ref={(el) => {
                        if (el && !audioRefs.current[track.id]) {
                            audioRefs.current[track.id] = el;
                            // Set the source when the element is created
                            el.src = `/api/files/${track.fileKey}`;
                            el.preload = "metadata";

                            // Set up event listeners
                            el.addEventListener("timeupdate", () => {
                                if (audioRefs.current[tracks[0]?.id] === el) {
                                    setCurrentTime(el.currentTime);
                                }
                            });

                            el.addEventListener("ended", () => {
                                setIsPlaying(false);
                                setCurrentTime(0);
                            });

                            el.addEventListener("error", (e) => {
                                console.error("Audio error:", e);
                                toast({
                                    title: "Audio Error",
                                    description: `Failed to load track: ${track.instrument}`,
                                    variant: "destructive",
                                });
                            });

                            // Apply initial settings
                            const settings = trackSettings[track.id] || {
                                volume: 0.8,
                                muted: false,
                            };
                            el.volume = settings.muted ? 0 : settings.volume;
                        }
                    }}
                    style={{ display: "none" }}
                />
            ))}
        </div>
    );
}
