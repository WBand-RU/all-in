import { apiClient } from "@/lib/axios-instance";

export interface SongSection {
    id: string;
    songId: string;
    name: string;
    order: number;
    barCount: number;
    lyrics?: string | null;
    chords?: string | null;
    revision: number;
    createdAt: string;
    updatedAt?: string | null;
}

export interface SongSectionInput {
    name: string;
    order: number;
    barCount: number;
    lyrics?: string | null;
    chords?: string | null;
}

export const songSectionsKey = (songId: string) => ["song-sections", songId] as const;
export async function getSongSections(songId: string): Promise<SongSection[]> {
    return (await apiClient.get<SongSection[]>(`/song-api/songs/${songId}/sections`)).data;
}
export async function createSongSection(songId: string, input: SongSectionInput): Promise<SongSection> {
    return (await apiClient.post<SongSection>(`/song-api/songs/${songId}/sections`, input)).data;
}
export async function updateSongSection(section: SongSection, input: SongSectionInput): Promise<SongSection> {
    return (await apiClient.put<SongSection>(`/song-api/sections/${section.id}`, {
        ...input, expectedRevision: section.revision,
    })).data;
}
export async function deleteSongSection(sectionId: string): Promise<void> {
    await apiClient.delete(`/song-api/sections/${sectionId}`);
}
