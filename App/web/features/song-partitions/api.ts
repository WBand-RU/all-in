import {
    createPartition,
    createSongSection,
    deletePartition,
    deleteSongSection,
    getSongContent,
    updatePartition,
    updateSongSection,
    updateSongContentCell,
    updatePartitionContent,
} from "@/lib/generated/web-api/client";
import type { SongContentResponse } from "@/lib/generated/web-api/models";

export {
    createPartition,
    createSongSection,
    deletePartition,
    deleteSongSection,
    getSongContent,
    updatePartition,
    updateSongSection,
    updateSongContentCell,
    updatePartitionContent,
};
export type SongMatrix = SongContentResponse;
export const songContentKey = (songId: string) =>
    ["song-content", songId] as const;
