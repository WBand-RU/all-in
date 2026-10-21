import { describe, expect, it } from "vitest";
import type { SongContentResponse } from "@/lib/generated/web-api/models";
import {
    createPartitionDraft,
    reconcilePartitionDraft,
} from "./partition-draft";

const base = {
    songId: "song",
    revision: 1,
    createdAt: "2026-10-20T00:00:00Z",
    updatedAt: null,
};
function matrix(): SongContentResponse {
    return {
        sections: [
            { ...base, id: "verse", name: "Куплет", order: 0 },
            { ...base, id: "chorus", name: "Припев", order: 1 },
        ],
        partitions: [{ ...base, id: "lyrics", name: "Слова", order: 0 }],
        cells: [
            {
                sectionId: "verse",
                partitionId: "lyrics",
                content: "  [Am]Куплет\n",
                revision: 3,
            },
        ],
    };
}

describe("partition drafts", () => {
    it("includes every shared section with a separate revision, including empty sections", () => {
        const draft = createPartitionDraft(matrix(), "lyrics");
        expect(Object.keys(draft.sections)).toEqual(["verse", "chorus"]);
        expect(draft.sections.verse.content).toBe("  [Am]Куплет\n");
        expect(draft.sections.verse.revision).toBe(3);
        expect(draft.sections.chorus).toMatchObject({
            content: "",
            savedContent: "",
            revision: 0,
        });
    });
    it("retains unsaved text and its original revision during a refetch while refreshing clean sections", () => {
        const initial = matrix();
        const draft = createPartitionDraft(initial, "lyrics");
        draft.sections.verse.content = "Мой черновик";
        initial.cells = [
            {
                sectionId: "verse",
                partitionId: "lyrics",
                content: "Чужая версия",
                revision: 4,
            },
            {
                sectionId: "chorus",
                partitionId: "lyrics",
                content: "Новый припев",
                revision: 2,
            },
        ];
        const next = reconcilePartitionDraft(draft, initial, "lyrics");
        expect(next.sections.verse).toMatchObject({
            content: "Мой черновик",
            savedContent: "  [Am]Куплет\n",
            revision: 3,
        });
        expect(next.sections.chorus).toMatchObject({
            content: "Новый припев",
            savedContent: "Новый припев",
            revision: 2,
        });
    });
    it("adds new shared sections and retains drafts from deleted sections for copying", () => {
        const initial = matrix();
        const draft = createPartitionDraft(initial, "lyrics");
        draft.sections.verse.content = "Не терять этот текст";
        initial.sections = [{ ...base, id: "bridge", name: "Бридж", order: 2 }];
        const next = reconcilePartitionDraft(draft, initial, "lyrics");
        expect(next.sections.bridge).toMatchObject({
            content: "",
            revision: 0,
        });
        expect(next.sections.verse.content).toBe("Не терять этот текст");
        expect(next.sections).not.toHaveProperty("chorus");
    });
});
