import { beforeEach, describe, expect, it, vi } from "vitest";
import { customInstance } from "@/lib/axios-instance";
import {
    createPartition,
    createSongSection,
    getSongContent,
    updateSongContentCell,
    updatePartitionContent,
} from "./api";

vi.mock("@/lib/axios-instance", () => ({ customInstance: vi.fn() }));
const request = vi.mocked(customInstance);
beforeEach(() => {
    request.mockReset();
});

describe("song matrix API", () => {
    it("loads the common axes and cells together", async () => {
        const content = { sections: [], partitions: [], cells: [] };
        request.mockResolvedValue(content);
        expect(await getSongContent("song")).toEqual(content);
        expect(request).toHaveBeenLastCalledWith(
            expect.objectContaining({
                url: "/song-api/songs/song/content",
                method: "GET",
            })
        );
    });
    it("adds axes independently without embedding text or copying sections", async () => {
        request.mockResolvedValue({});
        await createPartition("song", {
            name: "Заметки барабанщика",
            order: 1,
        });
        expect(request).toHaveBeenLastCalledWith(
            expect.objectContaining({
                url: "/song-api/songs/song/partitions",
                data: { name: "Заметки барабанщика", order: 1 },
            })
        );
        await createSongSection("song", { name: "Куплет", order: 0 });
        expect(request).toHaveBeenLastCalledWith(
            expect.objectContaining({
                url: "/song-api/songs/song/sections",
                data: { name: "Куплет", order: 0 },
            })
        );
    });
    it("saves text only at the intersection and retains whitespace and revision zero", async () => {
        const data = {
            content: "  [Am]Line one\nLine two [G]  \n",
            expectedRevision: 0,
        };
        request.mockResolvedValue({});
        await updateSongContentCell("song", "verse", "lyrics", data);
        expect(request).toHaveBeenLastCalledWith(
            expect.objectContaining({
                url: "/song-api/songs/song/sections/verse/partitions/lyrics",
                method: "PUT",
                data,
            })
        );
    });
    it("sends all changed sections in one partition update with independent revisions", async () => {
        const cells = [
            { sectionId: "verse", content: "  Куплет\n", expectedRevision: 4 },
            { sectionId: "chorus", content: "Припев", expectedRevision: 0 },
        ];
        request.mockResolvedValue([]);
        await updatePartitionContent("song", "lyrics", { cells });
        expect(request).toHaveBeenLastCalledWith(
            expect.objectContaining({
                url: "/song-api/songs/song/partitions/lyrics/content",
                method: "PUT",
                data: { cells },
            })
        );
        expect(request).toHaveBeenCalledTimes(1);
    });
    it("passes cell conflicts to the editor without retrying", async () => {
        const conflict = {
            code: "song_cell_version_conflict",
            currentRevision: 4,
        };
        request.mockRejectedValue(conflict);
        await expect(
            updateSongContentCell("song", "verse", "lyrics", {
                content: "draft",
                expectedRevision: 3,
            })
        ).rejects.toEqual(conflict);
        expect(request).toHaveBeenCalledTimes(1);
    });
});
