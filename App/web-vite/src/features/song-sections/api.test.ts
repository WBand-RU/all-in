import { beforeEach, describe, expect, it, vi } from "vitest";
import { customInstance } from "@/lib/axios-instance";
import { createSongSection, deleteSongSection, getSongSections, updateSongSection, type SongSection } from "./api";

vi.mock("@/lib/axios-instance", () => ({ customInstance: vi.fn() }));
const request = vi.mocked(customInstance);

beforeEach(() => { request.mockReset(); });

describe("plain text song sections", () => {
    it("uses the WebAPI section endpoints and preserves whitespace in the payload", async () => {
        const input = { name: "Verse", order: 0, lyrics: "  Line one\nLine two  \n", chords: "  Am   G\nC  F " };
        const section: SongSection = { ...input, id: "section", songId: "song", revision: 3,
            createdAt: "2026-10-13T00:00:00Z", updatedAt: null };
        request.mockResolvedValue(section);
        await createSongSection("song", input);
        expect(request).toHaveBeenLastCalledWith(expect.objectContaining({
            url: "/song-api/songs/song/sections", method: "POST", data: input,
        }));
        await updateSongSection(section, input);
        expect(request).toHaveBeenLastCalledWith(expect.objectContaining({
            url: "/song-api/sections/section", method: "PUT", data: { ...input, expectedRevision: 3 },
        }));
        const config = request.mock.lastCall?.[0];
        expect(config?.data).not.toHaveProperty("barCount");
    });

    it("loads and deletes sections through WebAPI", async () => {
        request.mockResolvedValue([]);
        expect(await getSongSections("song")).toEqual([]);
        expect(request).toHaveBeenLastCalledWith(expect.objectContaining({ url: "/song-api/songs/song/sections", method: "GET" }));
        await deleteSongSection("section");
        expect(request).toHaveBeenLastCalledWith(expect.objectContaining({ url: "/song-api/sections/section", method: "DELETE" }));
    });

    it("passes version conflicts to the editor without retrying the stale write", async () => {
        const conflict = { code: "song_section_version_conflict", currentRevision: 4 };
        request.mockRejectedValue(conflict);
        const section: SongSection = { id: "section", songId: "song", name: "Verse", order: 0,
            lyrics: "old", chords: null, revision: 3, createdAt: "2026-10-13T00:00:00Z", updatedAt: null };
        await expect(updateSongSection(section, { name: "Verse", order: 0, lyrics: "new", chords: null })).rejects.toEqual(conflict);
        expect(request).toHaveBeenCalledTimes(1);
    });
});
