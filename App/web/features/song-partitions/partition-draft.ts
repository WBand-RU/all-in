import { z } from "zod";
import type { SongContentResponse } from "@/lib/generated/web-api/models";

export const partitionDraftSchema = z.object({
    sections: z.record(
        z.string(),
        z.object({
            content: z.string().max(50000, "Не более 50 000 символов"),
            savedContent: z.string(),
            revision: z.union([z.number(), z.string()]),
            name: z.string(),
            order: z.number(),
        })
    ),
});
export type PartitionDraft = z.infer<typeof partitionDraftSchema>;

export function createPartitionDraft(
    matrix: SongContentResponse,
    partitionId: string
): PartitionDraft {
    const cells = new Map(
        matrix.cells
            .filter((cell) => cell.partitionId === partitionId)
            .map((cell) => [cell.sectionId, cell])
    );
    return {
        sections: Object.fromEntries(
            matrix.sections.map((section) => {
                const cell = cells.get(section.id);
                return [
                    section.id,
                    {
                        content: cell?.content ?? "",
                        savedContent: cell?.content ?? "",
                        revision: cell?.revision ?? 0,
                        name: section.name,
                        order: Number(section.order),
                    },
                ];
            })
        ),
    };
}

/** Refresh clean sections, while preserving both the text and revision of every unsaved draft. */
export function reconcilePartitionDraft(
    current: PartitionDraft,
    matrix: SongContentResponse,
    partitionId: string
): PartitionDraft {
    const next = createPartitionDraft(matrix, partitionId);
    for (const [id, draft] of Object.entries(current.sections)) {
        if (draft.content !== draft.savedContent) {
            next.sections[id] = {
                ...(next.sections[id] ?? draft),
                content: draft.content,
                savedContent: draft.savedContent,
                revision: draft.revision,
            };
        }
    }
    return next;
}
