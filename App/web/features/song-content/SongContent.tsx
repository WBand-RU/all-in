import { Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";
import type {
    PartitionResponse,
    SongContentResponse,
    SongSectionResponse,
} from "@/lib/generated/web-api/models";
import { splitChords } from "./chords";

export function ChordText({ text }: { text: string }) {
    return (
        <div className="font-mono leading-relaxed break-words whitespace-pre-wrap">
            {splitChords(text).map((part, index) => (
                <span
                    key={index}
                    className={
                        part.chord ? "font-semibold text-primary" : undefined
                    }
                >
                    {part.value}
                </span>
            ))}
        </div>
    );
}

export type SongAxis = PartitionResponse | SongSectionResponse;
export type AxisKind = "section" | "partition";

export function SongContent({
    content,
    onEditCell,
    onEditAxis,
    onDeleteAxis,
}: {
    content: SongContentResponse;
    onEditCell?: (
        section: SongSectionResponse,
        partition: PartitionResponse
    ) => void;
    onEditAxis?: (kind: AxisKind, axis: SongAxis) => void;
    onDeleteAxis?: (kind: AxisKind, axis: SongAxis) => void;
}) {
    const sections = [...content.sections].sort(
        (a, b) => Number(a.order) - Number(b.order)
    );
    const partitions = [...content.partitions].sort(
        (a, b) => Number(a.order) - Number(b.order)
    );
    const cells = new Map(
        content.cells.map((cell) => [
            `${cell.sectionId}/${cell.partitionId}`,
            cell,
        ])
    );
    function actions(kind: AxisKind, axis: SongAxis) {
        const label = kind === "section" ? "секцию" : "партию";
        return (
            <div className="flex shrink-0 gap-1">
                {onEditAxis && (
                    <Button
                        size="icon"
                        variant="ghost"
                        aria-label={`Изменить ${label} ${axis.name}`}
                        onClick={() => onEditAxis(kind, axis)}
                    >
                        <Pencil className="size-4" />
                    </Button>
                )}
                {onDeleteAxis && (
                    <Button
                        size="icon"
                        variant="ghost"
                        aria-label={`Удалить ${label} ${axis.name}`}
                        onClick={() => onDeleteAxis(kind, axis)}
                    >
                        <Trash2 className="size-4" />
                    </Button>
                )}
            </div>
        );
    }
    if (!sections.length && !partitions.length)
        return (
            <p className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
                Добавьте секции песни и партии, чтобы заполнить матрицу.
            </p>
        );
    return (
        <div className="min-w-0 space-y-3">
            <Table aria-label="Секции и партии песни">
                <TableHeader>
                    <TableRow>
                        <TableHead
                            scope="col"
                            className="sticky left-0 z-10 min-w-40 border-r bg-background p-4"
                        >
                            Секция / партия
                        </TableHead>
                        {partitions.map((partition) => (
                            <TableHead
                                key={partition.id}
                                scope="col"
                                className="min-w-64 p-4 align-top whitespace-normal"
                            >
                                <div className="flex items-start justify-between gap-2">
                                    <span className="min-w-0 font-semibold break-words">
                                        {partition.name}
                                    </span>
                                    {actions("partition", partition)}
                                </div>
                            </TableHead>
                        ))}
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {sections.map((section) => (
                        <TableRow key={section.id}>
                            <TableHead
                                scope="row"
                                className="sticky left-0 z-10 border-r bg-background p-4 align-top whitespace-normal"
                            >
                                <div className="space-y-2">
                                    <span className="block max-w-48 font-semibold break-words">
                                        {section.name}
                                    </span>
                                    {actions("section", section)}
                                </div>
                            </TableHead>
                            {partitions.map((partition) => {
                                const cell = cells.get(
                                    `${section.id}/${partition.id}`
                                );
                                return (
                                    <TableCell
                                        key={partition.id}
                                        className="max-w-lg min-w-64 p-4 align-top whitespace-normal"
                                        aria-label={`${section.name} · ${partition.name}`}
                                    >
                                        <div className="space-y-3">
                                            {cell?.content ? (
                                                <ChordText
                                                    text={cell.content}
                                                />
                                            ) : (
                                                <p className="text-muted-foreground">
                                                    —
                                                </p>
                                            )}
                                            {onEditCell && (
                                                <Button
                                                    size="sm"
                                                    variant="outline"
                                                    aria-label={`Редактировать ${section.name} · ${partition.name}`}
                                                    onClick={() =>
                                                        onEditCell(
                                                            section,
                                                            partition
                                                        )
                                                    }
                                                >
                                                    <Pencil className="size-3" />
                                                    {cell?.content
                                                        ? "Редактировать"
                                                        : "Добавить текст"}
                                                </Button>
                                            )}
                                        </div>
                                    </TableCell>
                                );
                            })}
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
            {!sections.length && (
                <p className="text-sm text-muted-foreground">
                    Секций пока нет. Добавьте секцию — она появится во всех
                    партиях.
                </p>
            )}
            {!partitions.length && (
                <p className="text-sm text-muted-foreground">
                    Партий пока нет. Добавьте слова, аккорды или заметки
                    музыканта.
                </p>
            )}
        </div>
    );
}
