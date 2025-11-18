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
} from "@/shared/ui/dialog";
import { Label } from "@/shared/ui/label";
import { Input } from "@/shared/ui/input";
import { Badge } from "@/shared/ui/badge";
import { useToast } from "@/shared/ui/use-toast";

interface Track {
    id: string;
    name: string;
    instrumentType: string;
    fileUrl: string;
    volume: number;
    pan: number;
    muted: boolean;
    solo: boolean;
}

interface Playback {
    id: string;
    songId: string;
    songTitle: string;
    tracks: Track[];
    currentMixerPresetId?: string;
    duration: number;
}

interface MixerPreset {
    id: string;
    name: string;
    trackSettings: { [trackId: string]: TrackSettings };
}

interface TrackSettings {
    volume: number;
    pan: number;
    muted: boolean;
    solo: boolean;
}

export function PlaybackControlPage() {
    const { id } = useParams();
    const { toast } = useToast();
    const audioRefs = useRef<{ [key: string]: HTMLAudioElement }>({});

    const [isPlaying, setIsPlaying] = useState(false);
    const [currentTime, setCurrentTime] = useState(0);
    const [duration] = useState(240); // Using fixed duration from playback
    const [uploadDialogOpen, setUploadDialogOpen] = useState(false);
    const [mixerPresetDialogOpen, setMixerPresetDialogOpen] = useState(false);
    const [isLoading, setIsLoading] = useState(false);

    const [playback, setPlayback] = useState<Playback>({
        id: id || "1",
        songId: "song1",
        songTitle: "Amazing Grace",
        duration: 240,
        tracks: [
            {
                id: "track1",
                name: "Lead Vocal",
                instrumentType: "Vocals",
                fileUrl: "/audio/lead-vocal.mp3",
                volume: 0.8,
                pan: 0,
                muted: false,
                solo: false,
            },
            {
                id: "track2",
                name: "Backing Vocals",
                instrumentType: "Vocals",
                fileUrl: "/audio/backing-vocals.mp3",
                volume: 0.6,
                pan: 0.3,
                muted: false,
                solo: false,
            },
            {
                id: "track3",
                name: "Acoustic Guitar",
                instrumentType: "Guitar",
                fileUrl: "/audio/acoustic-guitar.mp3",
                volume: 0.7,
                pan: -0.2,
                muted: false,
                solo: false,
            },
            {
                id: "track4",
                name: "Piano",
                instrumentType: "Piano",
                fileUrl: "/audio/piano.mp3",
                volume: 0.5,
                pan: 0.1,
                muted: false,
                solo: false,
            },
            {
                id: "track5",
                name: "Drums",
                instrumentType: "Drums",
                fileUrl: "/audio/drums.mp3",
                volume: 0.6,
                pan: 0,
                muted: false,
                solo: false,
            },
        ],
    });

    const [presets] = useState<MixerPreset[]>([
        {
            id: "preset1",
            name: "Vocals Focus",
            trackSettings: {
                track1: { volume: 1, pan: 0, muted: false, solo: false },
                track2: { volume: 0.8, pan: 0.3, muted: false, solo: false },
                track3: { volume: 0.4, pan: -0.2, muted: false, solo: false },
                track4: { volume: 0.3, pan: 0.1, muted: false, solo: false },
                track5: { volume: 0.3, pan: 0, muted: false, solo: false },
            },
        },
        {
            id: "preset2",
            name: "Instrumental",
            trackSettings: {
                track1: { volume: 0.3, pan: 0, muted: false, solo: false },
                track2: { volume: 0.2, pan: 0.3, muted: false, solo: false },
                track3: { volume: 0.9, pan: -0.2, muted: false, solo: false },
                track4: { volume: 0.8, pan: 0.1, muted: false, solo: false },
                track5: { volume: 0.7, pan: 0, muted: false, solo: false },
            },
        },
        {
            id: "preset3",
            name: "Balanced Mix",
            trackSettings: {
                track1: { volume: 0.7, pan: 0, muted: false, solo: false },
                track2: { volume: 0.6, pan: 0.3, muted: false, solo: false },
                track3: { volume: 0.7, pan: -0.2, muted: false, solo: false },
                track4: { volume: 0.5, pan: 0.1, muted: false, solo: false },
                track5: { volume: 0.6, pan: 0, muted: false, solo: false },
            },
        },
    ]);

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
        setPlayback((prev) => ({
            ...prev,
            tracks: prev.tracks.map((track) =>
                track.id === trackId ? { ...track, volume } : track,
            ),
        }));
    };

    const handleTrackPanChange = (trackId: string, pan: number) => {
        setPlayback((prev) => ({
            ...prev,
            tracks: prev.tracks.map((track) =>
                track.id === trackId ? { ...track, pan } : track,
            ),
        }));
    };

    const handleTrackMute = (trackId: string) => {
        setPlayback((prev) => ({
            ...prev,
            tracks: prev.tracks.map((track) =>
                track.id === trackId
                    ? { ...track, muted: !track.muted }
                    : track,
            ),
        }));
    };

    const handleTrackSolo = (trackId: string) => {
        setPlayback((prev) => ({
            ...prev,
            tracks: prev.tracks.map((track) =>
                track.id === trackId ? { ...track, solo: !track.solo } : track,
            ),
        }));
    };

    const applyPreset = (preset: MixerPreset) => {
        setPlayback((prev) => ({
            ...prev,
            currentMixerPresetId: preset.id,
            tracks: prev.tracks.map((track) => {
                const settings = preset.trackSettings[track.id];
                return settings ? { ...track, ...settings } : track;
            }),
        }));
        toast({
            title: "Preset Applied",
            description: `Applied "${preset.name}" mixer preset`,
        });
        setMixerPresetDialogOpen(false);
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
                                <CardTitle>{playback.songTitle}</CardTitle>
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
                                    onClick={() =>
                                        setMixerPresetDialogOpen(true)
                                    }
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

                        {/* Tracks Mixer */}
                        <div className="space-y-4">
                            <h3 className="text-lg font-semibold">
                                Track Mixer
                            </h3>
                            <div className="grid gap-4">
                                {playback.tracks.map((track) => (
                                    <div
                                        key={track.id}
                                        className={`p-4 border rounded-lg ${
                                            track.solo
                                                ? "ring-2 ring-primary"
                                                : ""
                                        }`}
                                    >
                                        <div className="flex items-center gap-4">
                                            <div
                                                className={`w-3 h-12 rounded-full ${getInstrumentColor(
                                                    track.instrumentType,
                                                )}`}
                                            />
                                            <div className="flex-1 space-y-3">
                                                <div className="flex items-center justify-between">
                                                    <div className="flex items-center gap-3">
                                                        <span className="font-medium">
                                                            {track.name}
                                                        </span>
                                                        <Badge variant="secondary">
                                                            {
                                                                track.instrumentType
                                                            }
                                                        </Badge>
                                                    </div>
                                                    <div className="flex gap-2">
                                                        <Button
                                                            variant={
                                                                track.solo
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
                                                                track.muted
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
                                                            {track.muted ? (
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
                                                                    track.volume *
                                                                        100,
                                                                )}
                                                                %
                                                            </span>
                                                        </div>
                                                        <Slider
                                                            value={[
                                                                track.volume,
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
                                                                track.muted
                                                            }
                                                        />
                                                    </div>

                                                    <div className="space-y-2">
                                                        <div className="flex justify-between">
                                                            <Label className="text-sm">
                                                                Pan
                                                            </Label>
                                                            <span className="text-sm text-muted-foreground">
                                                                {track.pan === 0
                                                                    ? "C"
                                                                    : track.pan <
                                                                        0
                                                                      ? `L${Math.abs(Math.round(track.pan * 100))}`
                                                                      : `R${Math.round(track.pan * 100)}`}
                                                            </span>
                                                        </div>
                                                        <Slider
                                                            value={[track.pan]}
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
                                                                track.muted
                                                            }
                                                        />
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
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
                            <Input type="file" multiple accept="audio/*" />
                        </div>
                        <Button
                            className="w-full"
                            disabled={isLoading}
                            onClick={() => {
                                setIsLoading(true);
                                setTimeout(() => {
                                    setIsLoading(false);
                                    setUploadDialogOpen(false);
                                    toast({
                                        title: "Upload Complete",
                                        description:
                                            "Tracks uploaded successfully",
                                    });
                                }, 2000);
                            }}
                        >
                            {isLoading ? (
                                <>
                                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                    Uploading...
                                </>
                            ) : (
                                <>
                                    <Upload className="mr-2 h-4 w-4" />
                                    Upload Tracks
                                </>
                            )}
                        </Button>
                    </div>
                </DialogContent>
            </Dialog>

            {/* Mixer Presets Dialog */}
            <Dialog
                open={mixerPresetDialogOpen}
                onOpenChange={setMixerPresetDialogOpen}
            >
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Mixer Presets</DialogTitle>
                        <DialogDescription>
                            Apply a predefined mixer configuration
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-2">
                        {presets.map((preset) => (
                            <Button
                                key={preset.id}
                                variant="outline"
                                className="w-full justify-start"
                                onClick={() => applyPreset(preset)}
                            >
                                <Settings className="mr-2 h-4 w-4" />
                                {preset.name}
                            </Button>
                        ))}
                    </div>
                </DialogContent>
            </Dialog>
        </div>
    );
}
