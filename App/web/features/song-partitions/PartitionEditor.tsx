"use client";

import { useEffect, useState } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Pencil, Save, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Field, FieldError, FieldLabel } from "@/components/ui/field";
import { Textarea } from "@/components/ui/textarea";
import type {
    PartitionResponse,
    SongContentCellResponse,
    SongContentResponse,
    SongSectionResponse,
} from "@/lib/generated/web-api/models";
import { updatePartitionContent, updateSongContentCell } from "./api";
import {
    createPartitionDraft,
    partitionDraftSchema,
    reconcilePartitionDraft,
    type PartitionDraft,
} from "./partition-draft";

export function PartitionEditor({
    songId,
    partition,
    matrix,
    onSaved,
    onReload,
    onEditSection,
    onDeleteSection,
}: {
    songId: string;
    partition: PartitionResponse;
    matrix: SongContentResponse;
    onSaved: (cells: SongContentCellResponse[]) => Promise<void>;
    onReload: () => Promise<unknown>;
    onEditSection: (section: SongSectionResponse) => void;
    onDeleteSection: (section: SongSectionResponse) => void;
}) {
    const form = useForm<PartitionDraft>({
        resolver: zodResolver(partitionDraftSchema),
        defaultValues: createPartitionDraft(matrix, partition.id),
    });
    const draft = useWatch({ control: form.control });
    const [saving, setSaving] = useState<string>();
    const [conflicts, setConflicts] = useState<string[]>([]);
    const [error, setError] = useState<string>();
    const sections = [...matrix.sections].sort(
        (a, b) => Number(a.order) - Number(b.order)
    );
    const liveIds = new Set(sections.map((section) => section.id));
    const dirtyIds = sections
        .filter((section) => {
            const entry = draft.sections?.[section.id];
            return entry && entry.content !== entry.savedContent;
        })
        .map((section) => section.id);
    const removedDrafts = Object.entries(draft.sections ?? {}).filter(
        ([id, entry]) =>
            !liveIds.has(id) && entry && entry.content !== entry.savedContent
    );

    useEffect(() => {
        const current = form.getValues();
        const next = reconcilePartitionDraft(current, matrix, partition.id);
        if (JSON.stringify(current) !== JSON.stringify(next)) form.reset(next);
    }, [form, matrix, partition.id]);

    async function save(ids: string[], all: boolean) {
        if (saving || !ids.length || ids.some((id) => conflicts.includes(id)))
            return;
        setSaving(all ? "all" : ids[0]);
        setError(undefined);
        try {
            if (
                !(await form.trigger(
                    ids.map((id) => `sections.${id}.content` as const)
                ))
            )
                return;
            const entries = form.getValues().sections;
            const cells = ids.map((sectionId) => ({
                sectionId,
                content: entries[sectionId].content,
                expectedRevision: entries[sectionId].revision,
            }));
            const saved = all
                ? await updatePartitionContent(songId, partition.id, { cells })
                : [
                      await updateSongContentCell(
                          songId,
                          ids[0],
                          partition.id,
                          {
                              content: cells[0].content,
                              expectedRevision: cells[0].expectedRevision,
                          }
                      ),
                  ];
            for (const cell of saved) {
                const entry = form.getValues(`sections.${cell.sectionId}`);
                form.setValue(`sections.${cell.sectionId}`, {
                    ...entry,
                    content: cell.content,
                    savedContent: cell.content,
                    revision: cell.revision,
                });
            }
            setConflicts((current) =>
                current.filter((id) => !ids.includes(id))
            );
            await onSaved(saved);
            toast.success(
                all ? "Все изменения партии сохранены" : "Секция сохранена"
            );
        } catch (failure) {
            const code =
                typeof failure === "object" &&
                failure !== null &&
                "code" in failure
                    ? failure.code
                    : undefined;
            const conflict =
                code === "song_cell_version_conflict" ||
                code === "song_partition_content_conflict";
            if (conflict) {
                const reported =
                    typeof failure === "object" &&
                    failure !== null &&
                    "sectionIds" in failure &&
                    Array.isArray(failure.sectionIds)
                        ? failure.sectionIds.filter(
                              (id): id is string => typeof id === "string"
                          )
                        : ids;
                setConflicts((current) => [
                    ...new Set([...current, ...reported]),
                ]);
                await onReload();
            }
            setError(
                conflict
                    ? "Ничего не сохранено: отмеченные секции изменены другим участником. Ваши черновики остались в редакторе."
                    : "Не удалось сохранить изменения. Ваши черновики остались в редакторе."
            );
        } finally {
            setSaving(undefined);
        }
    }

    function reloadSection(sectionId: string) {
        const latest = createPartitionDraft(matrix, partition.id).sections[
            sectionId
        ];
        if (latest) form.setValue(`sections.${sectionId}`, latest);
        setConflicts((current) => current.filter((id) => id !== sectionId));
        setError(undefined);
    }

    return (
        <form
            onSubmit={(event) => {
                event.preventDefault();
                void save(dirtyIds, true);
            }}
            className="space-y-5"
            aria-label={`Редактор партии ${partition.name}`}
        >
            <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border p-4">
                <div>
                    <h3 className="font-semibold">{partition.name}</h3>
                    <p className="text-sm text-muted-foreground">
                        {dirtyIds.length
                            ? `Несохранённых секций: ${dirtyIds.length}`
                            : "Все изменения сохранены"}
                    </p>
                </div>
                <Button
                    type="submit"
                    disabled={
                        !!saving ||
                        !dirtyIds.length ||
                        dirtyIds.some((id) => conflicts.includes(id))
                    }
                >
                    <Save />
                    {saving === "all" ? "Сохранение…" : "Сохранить всё"}
                </Button>
            </div>
            {error && (
                <p role="alert" className="text-sm text-destructive">
                    {error}
                </p>
            )}
            {!sections.length && (
                <p className="rounded-lg border border-dashed p-6 text-muted-foreground">
                    Добавьте секции песни — они появятся здесь и во всех
                    остальных партиях.
                </p>
            )}
            {sections.map((section) => {
                const dirty = dirtyIds.includes(section.id);
                const conflict = conflicts.includes(section.id);
                return (
                    <section
                        key={section.id}
                        className="space-y-3 rounded-lg border p-4"
                        aria-label={`Секция ${section.name}`}
                    >
                        <div className="flex flex-wrap items-center justify-between gap-2">
                            <h4 className="font-semibold">
                                {section.name}
                                {dirty && (
                                    <span className="ml-2 text-sm font-normal text-muted-foreground">
                                        Не сохранено
                                    </span>
                                )}
                            </h4>
                            <div className="flex flex-wrap gap-1">
                                <Button
                                    type="button"
                                    size="icon"
                                    variant="ghost"
                                    disabled={!!saving}
                                    aria-label={`Изменить секцию ${section.name}`}
                                    onClick={() => onEditSection(section)}
                                >
                                    <Pencil className="size-4" />
                                </Button>
                                <Button
                                    type="button"
                                    size="icon"
                                    variant="ghost"
                                    disabled={!!saving}
                                    aria-label={`Удалить секцию ${section.name}`}
                                    onClick={() => onDeleteSection(section)}
                                >
                                    <Trash2 className="size-4" />
                                </Button>
                                <Button
                                    type="button"
                                    size="sm"
                                    variant="outline"
                                    disabled={!!saving || !dirty || conflict}
                                    onClick={() =>
                                        void save([section.id], false)
                                    }
                                    aria-label={`Сохранить секцию ${section.name}`}
                                >
                                    <Save />
                                    {saving === section.id
                                        ? "Сохранение…"
                                        : "Сохранить секцию"}
                                </Button>
                            </div>
                        </div>
                        <Field
                            data-invalid={
                                !!form.formState.errors.sections?.[section.id]
                                    ?.content
                            }
                        >
                            <FieldLabel
                                htmlFor={`content-${partition.id}-${section.id}`}
                                className="sr-only"
                            >
                                {section.name} · {partition.name}
                            </FieldLabel>
                            <Controller
                                name={`sections.${section.id}.content`}
                                control={form.control}
                                render={({ field }) => (
                                    <Textarea
                                        id={`content-${partition.id}-${section.id}`}
                                        rows={7}
                                        maxLength={50000}
                                        className="font-mono"
                                        disabled={!!saving}
                                        {...field}
                                        value={field.value ?? ""}
                                        aria-invalid={
                                            !!form.formState.errors.sections?.[
                                                section.id
                                            ]?.content
                                        }
                                        aria-describedby={`error-${partition.id}-${section.id}`}
                                    />
                                )}
                            />
                            <FieldError
                                id={`error-${partition.id}-${section.id}`}
                                errors={[
                                    form.formState.errors.sections?.[section.id]
                                        ?.content,
                                ]}
                            />
                        </Field>
                        {conflict && (
                            <div role="alert" className="space-y-2 text-sm">
                                <p>
                                    Секция изменена другим участником. Загрузка
                                    текущей версии заменит черновик только этой
                                    секции.
                                </p>
                                <Button
                                    type="button"
                                    size="sm"
                                    variant="outline"
                                    disabled={!!saving}
                                    onClick={() => reloadSection(section.id)}
                                >
                                    Загрузить текущую версию секции
                                </Button>
                            </div>
                        )}
                    </section>
                );
            })}
            {removedDrafts.map(([id, entry]) => (
                <section key={id} className="space-y-2 rounded-lg border p-4">
                    <h4 className="font-semibold">
                        {entry?.name} — секция удалена
                    </h4>
                    <p className="text-sm text-muted-foreground">
                        Черновик сохранён здесь, чтобы вы могли скопировать
                        текст.
                    </p>
                    <Textarea
                        readOnly
                        value={entry?.content ?? ""}
                        aria-label={`Черновик удалённой секции ${entry?.name}`}
                    />
                    <Button
                        type="button"
                        variant="outline"
                        onClick={() => form.unregister(`sections.${id}`)}
                    >
                        Убрать черновик
                    </Button>
                </section>
            ))}
            <p className="text-sm text-muted-foreground">
                Аккорды записываются как [Am] и [G/B]. При просмотре они
                выделяются цветом без скобок и фона.
            </p>
        </form>
    );
}
