import {
    createSongSection as createSection,
    deleteSongSection as deleteSection,
    getSongSections as getSections,
    updateSongSection as updateSection,
} from "@/lib/generated-api/song-api/songs";
import type { CreateSongSectionRequest, SongSectionResponse } from "@/lib/generated-api/song-api/models";

export type SongSection = SongSectionResponse;
export type SongSectionInput = CreateSongSectionRequest;

export const songSectionsKey = (songId: string) => ["song-sections", songId] as const;
export const getSongSections = (songId: string): Promise<SongSection[]> => getSections(songId);
export const createSongSection = (songId: string, input: SongSectionInput): Promise<SongSection> =>
    createSection(songId, input);
export const updateSongSection = (section: SongSection, input: SongSectionInput): Promise<SongSection> =>
    updateSection(section.id, { ...input, lyrics: input.lyrics ?? null, chords: input.chords ?? null, expectedRevision: section.revision });
export async function deleteSongSection(sectionId: string): Promise<void> {
    await deleteSection(sectionId);
}
