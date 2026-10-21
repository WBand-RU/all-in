import type { SongContentResponse } from "@/lib/generated/web-api/models";
import { ChordText } from "./SongContent";

/** A single partition displayed as consecutive shared song sections. */
export function PartitionContent({
    content,
    partitionId,
}: {
    content: SongContentResponse;
    partitionId: string;
}) {
    const cells = new Map(
        content.cells
            .filter((cell) => cell.partitionId === partitionId)
            .map((cell) => [cell.sectionId, cell])
    );
    if (!content.sections.length)
        return (
            <p className="text-muted-foreground">Секции пока не добавлены.</p>
        );
    return (
        <div className="space-y-6">
            {[...content.sections]
                .sort((a, b) => Number(a.order) - Number(b.order))
                .map((section) => (
                    <section
                        key={section.id}
                        className="space-y-2"
                        aria-label={section.name}
                    >
                        <h3 className="font-semibold">{section.name}</h3>
                        {cells.get(section.id)?.content ? (
                            <ChordText text={cells.get(section.id)!.content} />
                        ) : (
                            <p className="text-muted-foreground">
                                В этой секции пока нет текста.
                            </p>
                        )}
                    </section>
                ))}
        </div>
    );
}
