"use client";

import { useCallback, useSyncExternalStore } from "react";
import { Label } from "@/components/ui/label";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { PartitionContent } from "@/features/song-content/PartitionContent";
import type { SongContentResponse } from "@/lib/generated/web-api/models";
import {
    rememberStagePartition,
    resolveStagePartition,
    stagePartitionSnapshot,
    subscribeStagePartition,
} from "./stage-partition-preference";

const serverSnapshot = () => '["",""]';

export function StagePartitionView({
    songId,
    userId,
    content,
}: {
    songId: string;
    userId?: string;
    content: SongContentResponse;
}) {
    const snapshot = useSyncExternalStore(
        subscribeStagePartition,
        useCallback(
            () => stagePartitionSnapshot(userId, songId),
            [userId, songId]
        ),
        serverSnapshot
    );
    const partitions = [...content.partitions].sort(
        (a, b) => Number(a.order) - Number(b.order)
    );
    const selected = resolveStagePartition(partitions, snapshot);
    if (!partitions.length)
        return (
            <p className="text-muted-foreground">
                В этой песне пока нет партий.
            </p>
        );
    return (
        <div className="space-y-6">
            <div className="space-y-2 text-base">
                <Label htmlFor={`stage-partition-${songId}`}>Моя партия</Label>
                <Select
                    items={partitions.map((partition) => ({
                        value: partition.id,
                        label: partition.name,
                    }))}
                    value={selected?.id ?? null}
                    onValueChange={(id) => {
                        const partition = partitions.find(
                            (partition) => partition.id === id
                        );
                        if (partition)
                            rememberStagePartition(userId, songId, partition);
                    }}
                    disabled={!userId}
                >
                    <SelectTrigger
                        id={`stage-partition-${songId}`}
                        className="w-full sm:max-w-sm"
                    >
                        <SelectValue placeholder="Выберите партию" />
                    </SelectTrigger>
                    <SelectContent>
                        {partitions.map((partition) => (
                            <SelectItem key={partition.id} value={partition.id}>
                                {partition.name}
                            </SelectItem>
                        ))}
                    </SelectContent>
                </Select>
            </div>
            {selected ? (
                <PartitionContent content={content} partitionId={selected.id} />
            ) : (
                <p role="status" className="text-muted-foreground">
                    Ранее выбранной партии в этой песне нет. Выберите другую
                    партию.
                </p>
            )}
        </div>
    );
}
