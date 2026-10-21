"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FileText, Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/components/ui/card";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import type { AxisKind, SongAxis } from "@/features/song-content/SongContent";
import { PartitionContent } from "@/features/song-content/PartitionContent";
import {
    createPartition,
    createSongSection,
    deletePartition,
    deleteSongSection,
    getSongContent,
    songContentKey,
    updatePartition,
    updateSongSection,
    type SongMatrix,
} from "./api";
import { AxisDialog } from "./AxisDialog";
import { PartitionEditor } from "./PartitionEditor";

type AxisSelection = { kind: AxisKind; axis?: SongAxis };

export function SongPartitionsPanel({
    songId,
    canEdit,
}: {
    songId: string;
    canEdit: boolean;
}) {
    const queryClient = useQueryClient();
    const [axisForm, setAxisForm] = useState<AxisSelection>();
    const [deleteAxis, setDeleteAxis] = useState<{
        kind: AxisKind;
        axis: SongAxis;
    }>();
    const [selectedId, setSelectedId] = useState<string>();
    const query = useQuery({
        queryKey: songContentKey(songId),
        queryFn: () => getSongContent(songId),
    });
    const invalidate = () =>
        queryClient.invalidateQueries({ queryKey: songContentKey(songId) });
    const partitions = [...(query.data?.partitions ?? [])].sort(
        (a, b) => Number(a.order) - Number(b.order)
    );
    const active =
        partitions.find((part) => part.id === selectedId) ?? partitions[0];
    const remove = useMutation({
        mutationFn: ({ kind, axis }: { kind: AxisKind; axis: SongAxis }) =>
            kind === "section"
                ? deleteSongSection(axis.id)
                : deletePartition(axis.id),
        onSuccess: async () => {
            setDeleteAxis(undefined);
            await invalidate();
        },
        onError: () => toast.error("Не удалось удалить. Попробуйте ещё раз."),
    });
    async function saveAxis(name: string) {
        if (!axisForm || !query.data) return;
        const { kind, axis } = axisForm;
        const values = {
            name,
            order:
                axis?.order ??
                Math.max(
                    -1,
                    ...(kind === "section"
                        ? query.data.sections
                        : query.data.partitions
                    ).map((item) => Number(item.order))
                ) + 1,
        };
        try {
            const saved =
                kind === "section"
                    ? axis
                        ? await updateSongSection(axis.id, {
                              ...values,
                              expectedRevision: axis.revision,
                          })
                        : await createSongSection(songId, values)
                    : axis
                      ? await updatePartition(axis.id, {
                            ...values,
                            expectedRevision: axis.revision,
                        })
                      : await createPartition(songId, values);
            queryClient.setQueryData<SongMatrix>(
                songContentKey(songId),
                (current) => {
                    if (!current) return current;
                    const key = kind === "section" ? "sections" : "partitions";
                    return {
                        ...current,
                        [key]: axis
                            ? current[key].map((item) =>
                                  item.id === saved.id ? saved : item
                              )
                            : [...current[key], saved],
                    };
                }
            );
            setAxisForm(undefined);
            if (!axis && kind === "partition") setSelectedId(saved.id);
            await invalidate();
        } catch (error) {
            await invalidate();
            throw error;
        }
    }
    return (
        <Card className="mt-6 min-w-0">
            <CardHeader>
                <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                        <CardTitle className="flex items-center gap-2">
                            <FileText className="size-5" />
                            Партии
                        </CardTitle>
                        <CardDescription>
                            Выберите партию и редактируйте все её секции. Список
                            секций общий для всей песни.
                        </CardDescription>
                    </div>
                    {canEdit && (
                        <div className="flex flex-wrap gap-2">
                            <Button
                                variant="outline"
                                disabled={!query.data}
                                onClick={() => setAxisForm({ kind: "section" })}
                            >
                                <Plus />
                                Добавить секцию
                            </Button>
                            <Button
                                disabled={!query.data}
                                onClick={() =>
                                    setAxisForm({ kind: "partition" })
                                }
                            >
                                <Plus />
                                Добавить партию
                            </Button>
                        </div>
                    )}
                </div>
            </CardHeader>
            <CardContent className="min-w-0 space-y-5">
                {query.isPending && (
                    <p role="status">Загрузка содержимого песни…</p>
                )}
                {query.isError && (
                    <div role="alert">
                        <p>Не удалось загрузить секции и партии.</p>
                        <Button
                            variant="outline"
                            onClick={() => query.refetch()}
                        >
                            Повторить
                        </Button>
                    </div>
                )}
                {query.data && (
                    <>
                        {active ? (
                            <div className="flex flex-wrap items-end gap-3">
                                <div className="min-w-0 space-y-2">
                                    <Label
                                        htmlFor={`partition-select-${songId}`}
                                    >
                                        Партия
                                    </Label>
                                    <Select
                                        items={partitions.map((part) => ({
                                            value: part.id,
                                            label: part.name,
                                        }))}
                                        value={active.id}
                                        onValueChange={(value) => {
                                            if (value) setSelectedId(value);
                                        }}
                                    >
                                        <SelectTrigger
                                            id={`partition-select-${songId}`}
                                            className="w-full max-w-full min-w-52"
                                        >
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {partitions.map((part) => (
                                                <SelectItem
                                                    key={part.id}
                                                    value={part.id}
                                                >
                                                    {part.name}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                                {canEdit && (
                                    <>
                                        <Button
                                            variant="outline"
                                            onClick={() =>
                                                setAxisForm({
                                                    kind: "partition",
                                                    axis: active,
                                                })
                                            }
                                        >
                                            <Pencil />
                                            Изменить название
                                        </Button>
                                        <Button
                                            size="icon"
                                            variant="ghost"
                                            aria-label={`Удалить партию ${active.name}`}
                                            onClick={() =>
                                                setDeleteAxis({
                                                    kind: "partition",
                                                    axis: active,
                                                })
                                            }
                                        >
                                            <Trash2 />
                                        </Button>
                                    </>
                                )}
                            </div>
                        ) : (
                            <p className="text-muted-foreground">
                                Партий пока нет. Добавьте слова, аккорды или
                                партию музыканта.
                            </p>
                        )}
                        {canEdit
                            ? partitions.map((partition) => (
                                  <div
                                      key={partition.id}
                                      hidden={partition.id !== active?.id}
                                  >
                                      <PartitionEditor
                                          songId={songId}
                                          partition={partition}
                                          matrix={query.data}
                                          onSaved={async (saved) => {
                                              const ids = new Set(
                                                  saved.map(
                                                      (cell) =>
                                                          `${cell.sectionId}/${cell.partitionId}`
                                                  )
                                              );
                                              queryClient.setQueryData<SongMatrix>(
                                                  songContentKey(songId),
                                                  (current) =>
                                                      current
                                                          ? {
                                                                ...current,
                                                                cells: [
                                                                    ...current.cells.filter(
                                                                        (
                                                                            cell
                                                                        ) =>
                                                                            !ids.has(
                                                                                `${cell.sectionId}/${cell.partitionId}`
                                                                            )
                                                                    ),
                                                                    ...saved,
                                                                ],
                                                            }
                                                          : current
                                              );
                                              await invalidate();
                                          }}
                                          onReload={invalidate}
                                          onEditSection={(section) =>
                                              setAxisForm({
                                                  kind: "section",
                                                  axis: section,
                                              })
                                          }
                                          onDeleteSection={(section) =>
                                              setDeleteAxis({
                                                  kind: "section",
                                                  axis: section,
                                              })
                                          }
                                      />
                                  </div>
                              ))
                            : active && (
                                  <PartitionContent
                                      content={query.data}
                                      partitionId={active.id}
                                  />
                              )}
                        {canEdit &&
                            !partitions.length &&
                            query.data.sections.map((section) => (
                                <div
                                    key={section.id}
                                    className="flex items-center justify-between gap-2 rounded-lg border p-3"
                                >
                                    <span>{section.name}</span>
                                    <div className="flex gap-1">
                                        <Button
                                            size="icon"
                                            variant="ghost"
                                            aria-label={`Изменить секцию ${section.name}`}
                                            onClick={() =>
                                                setAxisForm({
                                                    kind: "section",
                                                    axis: section,
                                                })
                                            }
                                        >
                                            <Pencil />
                                        </Button>
                                        <Button
                                            size="icon"
                                            variant="ghost"
                                            aria-label={`Удалить секцию ${section.name}`}
                                            onClick={() =>
                                                setDeleteAxis({
                                                    kind: "section",
                                                    axis: section,
                                                })
                                            }
                                        >
                                            <Trash2 />
                                        </Button>
                                    </div>
                                </div>
                            ))}
                    </>
                )}
            </CardContent>
            {canEdit && axisForm && (
                <AxisDialog
                    key={`${axisForm.kind}/${axisForm.axis?.id ?? "new"}`}
                    {...axisForm}
                    onSave={saveAxis}
                    onClose={() => setAxisForm(undefined)}
                />
            )}
            <Dialog
                open={!!deleteAxis}
                onOpenChange={(open) => {
                    if (!open && !remove.isPending) setDeleteAxis(undefined);
                }}
            >
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>
                            Удалить{" "}
                            {deleteAxis?.kind === "section"
                                ? "секцию"
                                : "партию"}{" "}
                            «{deleteAxis?.axis.name}»?
                        </DialogTitle>
                        <DialogDescription>
                            {deleteAxis?.kind === "section"
                                ? "Секция будет удалена из всех партий вместе с её содержимым."
                                : "Партия будет удалена вместе с её содержимым во всех секциях."}
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                        <Button
                            variant="outline"
                            disabled={remove.isPending}
                            onClick={() => setDeleteAxis(undefined)}
                        >
                            Отмена
                        </Button>
                        <Button
                            variant="destructive"
                            disabled={remove.isPending}
                            onClick={() => {
                                if (deleteAxis) remove.mutate(deleteAxis);
                            }}
                        >
                            {remove.isPending ? "Удаление…" : "Удалить"}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </Card>
    );
}
