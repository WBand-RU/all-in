import { useEffect, useRef, useState } from "react";
import { Download, LoaderCircle, Play, Square } from "lucide-react";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import { getStemDownloadUrl, type MixArtifact } from "./api";
import { MixPlayback } from "./mix-playback";
import { listeningMixes } from "./mix-catalog";

export function MixPlayer({ groups }: { groups: [string, MixArtifact[]][] }) {
    const audioRef = useRef<HTMLAudioElement>(null);
    const playbackRef = useRef<MixPlayback | null>(null);
    const [selected, setSelected] = useState<MixArtifact | null>(null);
    const [pending, setPending] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const selectionRef = useRef(0);

    useEffect(() => {
        const playback = new MixPlayback(audioRef.current!, getStemDownloadUrl);
        playbackRef.current = playback;
        return () => {
            playback.stop();
            playbackRef.current = null;
        };
    }, []);

    async function play(artifact: MixArtifact) {
        const selection = ++selectionRef.current;
        setSelected(artifact);
        setPending(true);
        setError(null);
        const playback = playbackRef.current;
        try {
            await playback?.play(artifact.fileId);
        } catch {
            if (selectionRef.current === selection && playbackRef.current === playback)
                setError("Не удалось воспроизвести микс. Попробуйте ещё раз.");
        } finally {
            if (selectionRef.current === selection && playbackRef.current === playback)
                setPending(false);
        }
    }

    function stop() {
        ++selectionRef.current;
        playbackRef.current?.stop();
        setSelected(null);
        setPending(false);
        setError(null);
    }

    return (
        <div className="space-y-6">
            <div className="space-y-2 rounded-lg border p-3">
                <div className="flex items-center justify-between gap-3">
                    <div className="text-sm font-medium" aria-live="polite">
                        {selected
                            ? `${selected.name} · ${selected.format.toUpperCase()}`
                            : "Выберите микс для прослушивания"}
                    </div>
                    {selected && (
                        <Button variant="ghost" size="sm" onClick={stop} aria-label="Остановить микс">
                            <Square className="h-4 w-4" />
                        </Button>
                    )}
                </div>
                <audio
                    ref={audioRef}
                    className="h-10 w-full"
                    controls
                    preload="none"
                    aria-label="Плеер выбранного микса"
                    onError={() => setError("Не удалось загрузить аудио. Выберите микс ещё раз.")}
                />
                {error && <div role="alert" className="text-sm text-destructive">{error}</div>}
            </div>
            {groups.map(([group, artifacts]) => (
                <section key={group}>
                    <h3 className="mb-2 font-semibold">{group}</h3>
                    <div className="grid gap-2 md:grid-cols-2">
                        {listeningMixes(artifacts).map(({ primary: artifact, formats }) => {
                            const active = formats.some(format => format.fileId === selected?.fileId);
                            return (
                                <div key={`${artifact.kind}:${artifact.targetStemId ?? artifact.name}`} className="space-y-3 rounded-lg border p-3">
                                    <div className="font-medium">{artifact.name}</div>
                                    <div className="flex gap-2">
                                        <Badge>{artifact.kind}</Badge>
                                        <Badge>{artifact.format.toUpperCase()}</Badge>
                                    </div>
                                    <div className="flex flex-wrap gap-2">
                                        <Button
                                            variant={active ? "default" : "outline"}
                                            size="sm"
                                            disabled={active && pending}
                                            aria-pressed={active}
                                            aria-label={`Слушать ${artifact.name} ${artifact.format.toUpperCase()}`}
                                            onClick={() => void play(artifact)}
                                        >
                                            {active && pending
                                                ? <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                                                : <Play className="mr-2 h-4 w-4" />}
                                            Слушать
                                        </Button>
                                        {formats.filter(format => format.downloadUrl).map(format => (
                                            <Button key={format.id} variant="outline" size="sm" asChild>
                                                <a href={format.downloadUrl!} target="_blank" rel="noreferrer"
                                                    aria-label={`Скачать ${format.name} ${format.format.toUpperCase()}`}>
                                                    <Download className="mr-2 h-4 w-4" />
                                                    Скачать {format.format.toUpperCase()}
                                                </a>
                                            </Button>
                                        ))}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </section>
            ))}
        </div>
    );
}
