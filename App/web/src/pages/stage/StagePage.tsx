import { useState, useEffect } from "react";
import { useNavigate } from "react-router";
import {
    Minimize2,
    ChevronUp,
    ChevronDown,
    Music,
    PauseCircle,
    Hash,
    Clock,
    ZoomIn,
    ZoomOut,
    Play,
    Pause,
} from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Badge } from "@/shared/ui/badge";
import { cn } from "@/lib/utils";

interface StageItem {
    id: string;
    type: "song" | "block" | "pause";
    title: string;
    key?: string;
    lyrics?: string[];
    chords?: string[];
    duration?: number;
    notes?: string;
}

interface StageData {
    playlistName: string;
    currentIndex: number;
    items: StageItem[];
    isPlaying: boolean;
}

export function StagePage() {
    const navigate = useNavigate();
    const [fontSize, setFontSize] = useState(3); // 1-5 scale
    const [showChords, setShowChords] = useState(true);
    const [autoScroll] = useState(false);
    const [scrollPosition] = useState(0);
    const [currentVerse, setCurrentVerse] = useState(0);

    const [stageData, setStageData] = useState<StageData>({
        playlistName: "Sunday Morning Service",
        currentIndex: 0,
        isPlaying: false,
        items: [
            {
                id: "1",
                type: "song",
                title: "Amazing Grace",
                key: "G",
                duration: 240,
                lyrics: [
                    "Amazing grace how sweet the sound",
                    "That saved a wretch like me",
                    "I once was lost but now I'm found",
                    "Was blind but now I see",
                    "",
                    "'Twas grace that taught my heart to fear",
                    "And grace my fears relieved",
                    "How precious did that grace appear",
                    "The hour I first believed",
                ],
                chords: [
                    "G                    G7",
                    "        C            G",
                    "                     Em",
                    "     D7              G",
                    "",
                    "G                    G7",
                    "        C            G",
                    "                     Em",
                    "     D7              G",
                ],
            },
            {
                id: "2",
                type: "block",
                title: "Prayer Time",
                duration: 120,
                notes: "Lead congregation in prayer",
            },
            {
                id: "3",
                type: "song",
                title: "How Great Thou Art",
                key: "A",
                duration: 300,
                lyrics: [
                    "O Lord my God, when I in awesome wonder",
                    "Consider all the worlds Thy hands have made",
                    "I see the stars, I hear the rolling thunder",
                    "Thy power throughout the universe displayed",
                    "",
                    "Then sings my soul, my Saviour God to Thee",
                    "How great Thou art! How great Thou art!",
                    "Then sings my soul, my Saviour God to Thee",
                    "How great Thou art! How great Thou art!",
                ],
                chords: [
                    "A                    D",
                    "                     A",
                    "                     E",
                    "                     A",
                    "",
                    "          A          D",
                    "                     A",
                    "          E          D",
                    "                     A",
                ],
            },
            {
                id: "4",
                type: "pause",
                title: "Short Break",
                duration: 60,
            },
        ],
    });

    const currentItem = stageData.items[stageData.currentIndex];

    useEffect(() => {
        if (autoScroll && stageData.isPlaying) {
            const interval = setInterval(() => {
                window.scrollBy(0, 1);
            }, 100 - scrollPosition);
            return () => clearInterval(interval);
        }
    }, [autoScroll, scrollPosition, stageData.isPlaying]);

    useEffect(() => {
        const handleKeyPress = (e: KeyboardEvent) => {
            switch (e.key) {
                case "ArrowLeft":
                    previousItem();
                    break;
                case "ArrowRight":
                    nextItem();
                    break;
                case "ArrowUp":
                    if (currentVerse > 0) setCurrentVerse((prev) => prev - 1);
                    break;
                case "ArrowDown":
                    if (
                        currentItem.lyrics &&
                        currentVerse <
                            Math.floor(currentItem.lyrics.length / 4) - 1
                    ) {
                        setCurrentVerse((prev) => prev + 1);
                    }
                    break;
                case " ":
                    e.preventDefault();
                    togglePlayPause();
                    break;
                case "Escape":
                    navigate(-1);
                    break;
            }
        };

        window.addEventListener("keydown", handleKeyPress);
        return () => window.removeEventListener("keydown", handleKeyPress);
    }, [currentItem, currentVerse]);

    const nextItem = () => {
        if (stageData.currentIndex < stageData.items.length - 1) {
            setStageData((prev) => ({
                ...prev,
                currentIndex: prev.currentIndex + 1,
            }));
            setCurrentVerse(0);
        }
    };

    const previousItem = () => {
        if (stageData.currentIndex > 0) {
            setStageData((prev) => ({
                ...prev,
                currentIndex: prev.currentIndex - 1,
            }));
            setCurrentVerse(0);
        }
    };

    const togglePlayPause = () => {
        setStageData((prev) => ({ ...prev, isPlaying: !prev.isPlaying }));
    };

    const increaseFontSize = () => {
        if (fontSize < 5) setFontSize((prev) => prev + 1);
    };

    const decreaseFontSize = () => {
        if (fontSize > 1) setFontSize((prev) => prev - 1);
    };

    const getFontSizeClass = () => {
        const sizes = [
            "text-lg",
            "text-xl",
            "text-2xl",
            "text-3xl",
            "text-4xl",
        ];
        return sizes[fontSize - 1];
    };

    const renderLyrics = () => {
        if (!currentItem.lyrics) return null;

        const startIdx = currentVerse * 4;
        const endIdx = startIdx + 4;
        const verseLyrics = currentItem.lyrics.slice(startIdx, endIdx);
        const verseChords =
            showChords && currentItem.chords
                ? currentItem.chords.slice(startIdx, endIdx)
                : [];

        return (
            <div className="space-y-2">
                {verseLyrics.map((line, idx) => (
                    <div key={idx}>
                        {showChords && verseChords[idx] && (
                            <div className="text-primary font-mono mb-1 whitespace-pre">
                                {verseChords[idx]}
                            </div>
                        )}
                        <div
                            className={cn(
                                "leading-relaxed",
                                line === "" && "h-4",
                            )}
                        >
                            {line}
                        </div>
                    </div>
                ))}
            </div>
        );
    };

    return (
        <div className="min-h-screen bg-background text-foreground p-8">
            {/* Header Controls */}
            <div className="fixed top-0 left-0 right-0 bg-background/95 backdrop-blur-sm border-b p-4 z-50">
                <div className="container mx-auto flex justify-between items-center">
                    <div className="flex items-center gap-4">
                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => navigate(-1)}
                        >
                            <Minimize2 className="h-4 w-4 mr-2" />
                            Exit Stage
                        </Button>
                        <Badge variant="outline" className="text-lg px-3 py-1">
                            {stageData.playlistName}
                        </Badge>
                    </div>

                    <div className="flex items-center gap-2">
                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={decreaseFontSize}
                        >
                            <ZoomOut className="h-4 w-4" />
                        </Button>
                        <span className="text-sm px-2">Size: {fontSize}</span>
                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={increaseFontSize}
                        >
                            <ZoomIn className="h-4 w-4" />
                        </Button>
                        <Button
                            variant={showChords ? "default" : "ghost"}
                            size="sm"
                            onClick={() => setShowChords(!showChords)}
                        >
                            <Hash className="h-4 w-4 mr-2" />
                            Chords
                        </Button>
                    </div>
                </div>
            </div>

            {/* Main Content */}
            <div className="mt-24 mb-32 max-w-5xl mx-auto">
                {/* Current Item Header */}
                <div className="mb-8 text-center">
                    <div className="flex items-center justify-center gap-4 mb-4">
                        {currentItem.type === "song" && (
                            <Music className="h-8 w-8" />
                        )}
                        <h1 className="text-5xl font-bold">
                            {currentItem.title}
                        </h1>
                        {currentItem.key && (
                            <Badge className="text-xl px-4 py-2">
                                Key: {currentItem.key}
                            </Badge>
                        )}
                    </div>
                    {currentItem.duration && (
                        <div className="flex items-center justify-center gap-2 text-muted-foreground">
                            <Clock className="h-4 w-4" />
                            <span>
                                {Math.floor(currentItem.duration / 60)}:
                                {(currentItem.duration % 60)
                                    .toString()
                                    .padStart(2, "0")}
                            </span>
                        </div>
                    )}
                </div>

                {/* Content Area */}
                <div className={cn("text-center", getFontSizeClass())}>
                    {currentItem.type === "song" && renderLyrics()}
                    {currentItem.type === "block" && (
                        <div className="py-12">
                            <Badge className="text-2xl px-6 py-3 mb-4">
                                Block
                            </Badge>
                            {currentItem.notes && (
                                <p className="text-muted-foreground mt-4">
                                    {currentItem.notes}
                                </p>
                            )}
                        </div>
                    )}
                    {currentItem.type === "pause" && (
                        <div className="py-12">
                            <PauseCircle className="h-24 w-24 mx-auto mb-4 text-muted-foreground" />
                            <p className="text-muted-foreground">
                                Taking a short break
                            </p>
                        </div>
                    )}
                </div>

                {/* Verse Navigation (for songs) */}
                {currentItem.type === "song" && currentItem.lyrics && (
                    <div className="flex justify-center gap-2 mt-8">
                        {Array.from({
                            length: Math.ceil(currentItem.lyrics.length / 4),
                        }).map((_, idx) => (
                            <Button
                                key={idx}
                                variant={
                                    currentVerse === idx ? "default" : "outline"
                                }
                                size="sm"
                                onClick={() => setCurrentVerse(idx)}
                            >
                                {idx + 1}
                            </Button>
                        ))}
                    </div>
                )}
            </div>

            {/* Footer Controls */}
            <div className="fixed bottom-0 left-0 right-0 bg-background/95 backdrop-blur-sm border-t p-4 z-50">
                <div className="container mx-auto">
                    <div className="flex justify-between items-center">
                        <div className="flex items-center gap-2">
                            <Button
                                variant="outline"
                                onClick={previousItem}
                                disabled={stageData.currentIndex === 0}
                            >
                                <ChevronUp className="h-4 w-4 mr-2" />
                                Previous
                            </Button>
                            <Button
                                variant="outline"
                                onClick={nextItem}
                                disabled={
                                    stageData.currentIndex ===
                                    stageData.items.length - 1
                                }
                            >
                                Next
                                <ChevronDown className="h-4 w-4 ml-2" />
                            </Button>
                        </div>

                        <div className="flex items-center gap-4">
                            <div className="text-sm text-muted-foreground">
                                Item {stageData.currentIndex + 1} of{" "}
                                {stageData.items.length}
                            </div>
                            <Button
                                size="lg"
                                onClick={togglePlayPause}
                                className="rounded-full"
                            >
                                {stageData.isPlaying ? (
                                    <Pause className="h-5 w-5" />
                                ) : (
                                    <Play className="h-5 w-5 ml-1" />
                                )}
                            </Button>
                        </div>

                        <div className="text-sm text-muted-foreground">
                            Use arrow keys to navigate • Space to play/pause •
                            ESC to exit
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
