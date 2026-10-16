import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    closestCenter,
    DndContext,
    KeyboardSensor,
    PointerSensor,
    useDroppable,
    useSensor,
    useSensors,
    type DragEndEvent,
} from "@dnd-kit/core";
import {
    SortableContext,
    sortableKeyboardCoordinates,
    useSortable,
    verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
    FileAudio,
    FolderUp,
    GripVertical,
    LoaderCircle,
    Plus,
    RefreshCw,
    Trash2,
} from "lucide-react";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/shared/ui/card";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/shared/ui/dialog";
import { Input } from "@/shared/ui/input";
import { useToast } from "@/shared/ui/use-toast";
import {
    createStemGroup,
    deleteStem,
    deleteStems,
    formatBytes,
    getMixes,
    getStems,
    isSupportedAudio,
    queueMixes,
    reorderStems,
    stemQueryKey,
    type Stem,
    type StemCollection,
    type StemKind,
    type MixArtifact,
} from "./api";
import { MixPlayer } from "./MixPlayer";
import {
    uploadFolder,
    UploadOutcomeUnknownError,
    type UploadPhase,
} from "./upload-folder";

const kindLabels: Record<StemKind, string> = {
    Keys: "Клавиши",
    Guitar: "Гитары",
    Bass: "Бас",
    Drums: "Ударные",
    Vocal: "Вокал",
    Backing: "Бэкинг",
    Click: "Клик",
    Guide: "Гайд",
    Other: "Другое",
};
const groupPresets = ["Клавиши", "Гитары", "Бас", "Ударные", "Вокал"];
const ungroupedKey = "ungrouped";
const groupDndId = (groupId: string | null) =>
    `group:${groupId ?? ungroupedKey}`;
const stemDndId = (stemId: string) => `stem:${stemId}`;

function inferKind(fileName: string): StemKind {
    const value = fileName.toLowerCase();
    if (/key|piano|synth|organ|клав|пиан/.test(value)) return "Keys";
    if (/guitar|gtr|гитар/.test(value)) return "Guitar";
    if (/bass|бас/.test(value)) return "Bass";
    if (/drum|kick|snare|tom|overhead|перкус|удар/.test(value)) return "Drums";
    if (/vocal|vox|voice|вокал/.test(value)) return "Vocal";
    if (/click|клик|metronome/.test(value)) return "Click";
    if (/guide|гайд/.test(value)) return "Guide";
    if (/back|pad|бэк/.test(value)) return "Backing";
    return "Other";
}

export function StemsPanel({
    songId,
    canEdit,
}: {
    songId: string;
    canEdit: boolean;
}) {
    const queryClient = useQueryClient();
    const { toast } = useToast();
    const [uploadOpen, setUploadOpen] = useState(false);
    const [files, setFiles] = useState<File[]>([]);
    const [progress, setProgress] = useState(0);
    const [uploadPhase, setUploadPhase] = useState<UploadPhase>("preparing");
    const [uploadError, setUploadError] = useState<string | null>(null);
    const uploadController = useRef<AbortController | null>(null);
    useEffect(() => () => uploadController.current?.abort(), []);
    const [newGroupName, setNewGroupName] = useState("");
    const [stemToDelete, setStemToDelete] = useState<Stem | null>(null);
    const [batchToDelete, setBatchToDelete] = useState<{
        title: string;
        stems: Stem[];
        contentVersion: number;
    } | null>(null);
    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
        useSensor(KeyboardSensor, {
            coordinateGetter: sortableKeyboardCoordinates,
        }),
    );
    const stems = useQuery({
        queryKey: stemQueryKey(songId),
        queryFn: () => getStems(songId),
    });
    const mixes = useQuery({
        queryKey: ["mixes", songId],
        queryFn: () => getMixes(songId),
        refetchInterval: (query) =>
            ["Queued", "Processing"].includes(query.state.data?.status ?? "")
                ? 3000
                : false,
    });

    const stemGroups = useMemo(() => {
        const collection = stems.data;
        if (!collection) return [];
        const groups = [...collection.groups]
            .sort((left, right) => left.order - right.order)
            .map((group) => ({
                id: group.id as string | null,
                name: group.name,
            }));
        groups.push({ id: null, name: "Без группы" });
        return groups.map((group) => ({
            ...group,
            items: collection.stems
                .filter((stem) => (stem.groupId ?? null) === group.id)
                .sort(
                    (left, right) =>
                        left.order - right.order ||
                        left.name.localeCompare(right.name),
                ),
        }));
    }, [stems.data]);
    const groupedMixes = useMemo(
        () =>
            Object.entries(
                (mixes.data?.artifacts ?? []).reduce(
                    (result, artifact) => {
                        (result[artifact.group] ??= []).push(artifact);
                        return result;
                    },
                    {} as Record<string, MixArtifact[]>,
                ),
            ),
        [mixes.data],
    );

    const createGroup = useMutation({
        mutationFn: (name: string) =>
            createStemGroup(
                songId,
                name.trim(),
                stems.data?.groups.length ?? 0,
            ),
        onSuccess: async () => {
            setNewGroupName("");
            await queryClient.invalidateQueries({
                queryKey: stemQueryKey(songId),
            });
        },
        onError: () =>
            toast({
                title: "Не удалось создать группу",
                variant: "destructive",
            }),
    });
    const reorder = useMutation({
        mutationFn: ({
            collection,
            items,
        }: {
            collection: StemCollection;
            items: StemCollection["stems"];
        }) =>
            reorderStems(
                songId,
                collection.contentVersion,
                items.map((stem) => ({
                    stemId: stem.id,
                    groupId: stem.groupId ?? null,
                    order: stem.order,
                    expectedRevision: stem.revision,
                })),
            ),
        onMutate: async ({ collection, items }) => {
            await queryClient.cancelQueries({ queryKey: stemQueryKey(songId) });
            const previous = queryClient.getQueryData<StemCollection>(
                stemQueryKey(songId),
            );
            queryClient.setQueryData<StemCollection>(stemQueryKey(songId), {
                ...collection,
                stems: items,
            });
            return { previous };
        },
        onError: (_error, _variables, context) => {
            if (context?.previous)
                queryClient.setQueryData(
                    stemQueryKey(songId),
                    context.previous,
                );
            toast({
                title: "Не удалось изменить порядок стемов",
                description: "Список обновлён с сервера. Повторите перенос.",
                variant: "destructive",
            });
        },
        onSettled: () =>
            queryClient.invalidateQueries({ queryKey: stemQueryKey(songId) }),
    });

    function handleDragEnd(event: DragEndEvent) {
        if (
            !canEdit ||
            !event.over ||
            !stems.data ||
            event.active.id === event.over.id
        )
            return;
        const activeId = String(event.active.id).replace(/^stem:/, "");
        const activeStem = stems.data.stems.find(
            (stem) => stem.id === activeId,
        );
        if (!activeStem) return;

        const overId = String(event.over.id);
        const overStem = overId.startsWith("stem:")
            ? stems.data.stems.find(
                  (stem) => stem.id === overId.replace(/^stem:/, ""),
              )
            : undefined;
        const targetGroupId =
            overStem?.groupId ??
            (overId.startsWith("group:")
                ? overId.replace(/^group:/, "") === ungroupedKey
                    ? null
                    : overId.replace(/^group:/, "")
                : null);

        const buckets = new Map<string, Stem[]>();
        for (const stem of stems.data.stems) {
            const key = stem.groupId ?? ungroupedKey;
            const bucket = buckets.get(key) ?? [];
            bucket.push(stem);
            buckets.set(key, bucket);
        }
        for (const [key, bucket] of buckets) {
            buckets.set(
                key,
                bucket
                    .sort(
                        (left, right) =>
                            left.order - right.order ||
                            left.name.localeCompare(right.name),
                    )
                    .filter((stem) => stem.id !== activeStem.id),
            );
        }
        const targetKey = targetGroupId ?? ungroupedKey;
        const target = buckets.get(targetKey) ?? [];
        const overIndex = overStem
            ? target.findIndex((stem) => stem.id === overStem.id)
            : target.length;
        target.splice(overIndex < 0 ? target.length : overIndex, 0, {
            ...activeStem,
            groupId: targetGroupId,
        });
        buckets.set(targetKey, target);

        const ordered = Array.from(buckets.values()).flatMap((bucket) =>
            bucket.map((stem, order) => ({ ...stem, order })),
        );
        reorder.mutate({ collection: stems.data, items: ordered });
    }

    const upload = useMutation({
        mutationFn: (controller: AbortController) =>
            uploadFolder(
                songId,
                files.filter(isSupportedAudio),
                inferKind,
                setProgress,
                setUploadPhase,
                controller.signal,
            ),
        onSuccess: async () => {
            await Promise.all([
                queryClient.invalidateQueries({
                    queryKey: stemQueryKey(songId),
                }),
                queryClient.invalidateQueries({ queryKey: ["mixes", songId] }),
            ]);
            setUploadOpen(false);
            setFiles([]);
            setProgress(0);
            toast({ title: "Все мультитреки загружены" });
            try {
                await queueMixes(songId);
                await queryClient.invalidateQueries({
                    queryKey: ["mixes", songId],
                });
            } catch {
                toast({
                    title: "Файлы загружены, но миксы не запущены",
                    description: "Нажмите «Пересобрать», чтобы создать миксы.",
                    variant: "destructive",
                });
            }
        },
        onError: async (error, controller) => {
            if (error instanceof UploadOutcomeUnknownError) {
                setUploadError(
                    "Связь потеряна при сохранении. Обновите страницу и проверьте список стемов перед повторной загрузкой.",
                );
                toast({
                    title: "Не удалось подтвердить результат загрузки",
                    variant: "destructive",
                });
                await queryClient.invalidateQueries({
                    queryKey: stemQueryKey(songId),
                });
                return;
            }
            const cancelled = controller.signal.aborted;
            setUploadError(
                cancelled
                    ? "Загрузка отменена. Стемы из этой попытки не добавлены."
                    : "Папка не загружена. Стемы из этой попытки не добавлены; можно повторить загрузку.",
            );
            setProgress(0);
            toast({
                title: cancelled
                    ? "Загрузка отменена"
                    : "Не удалось загрузить папку",
                description: "Стемы из этой попытки не добавлены.",
                variant: cancelled ? "default" : "destructive",
            });
        },
        onSettled: () => {
            uploadController.current = null;
        },
    });
    const regenerate = useMutation({
        mutationFn: () => queueMixes(songId),
        onSuccess: () =>
            queryClient.invalidateQueries({ queryKey: ["mixes", songId] }),
        onError: () =>
            toast({
                title: "Не удалось поставить миксы в очередь",
                variant: "destructive",
            }),
    });

    const remove = useMutation({
        mutationFn: (stem: Stem) => deleteStem(songId, stem.id, stem.revision),
        onSuccess: () => toast({ title: "Стем и его файл удалены" }),
        onError: () =>
            toast({
                title: "Не удалось удалить стем",
                description:
                    "Список обновлён. Попробуйте удалить стем ещё раз.",
                variant: "destructive",
            }),
        onSettled: async () => {
            await Promise.all([
                queryClient.invalidateQueries({
                    queryKey: stemQueryKey(songId),
                }),
                queryClient.invalidateQueries({ queryKey: ["mixes", songId] }),
            ]);
            setStemToDelete(null);
        },
    });

    const removeBatch = useMutation({
        mutationFn: (batch: NonNullable<typeof batchToDelete>) =>
            deleteStems(songId, batch.contentVersion, batch.stems),
        onSuccess: () => toast({ title: "Стемы и их файлы удалены" }),
        onError: () =>
            toast({
                title: "Не удалось удалить стемы",
                description:
                    "Список обновлён. Проверьте его и повторите удаление.",
                variant: "destructive",
            }),
        onSettled: async () => {
            await Promise.all([
                queryClient.invalidateQueries({
                    queryKey: stemQueryKey(songId),
                }),
                queryClient.invalidateQueries({ queryKey: ["mixes", songId] }),
            ]);
            setBatchToDelete(null);
        },
    });
    const canChangeStems =
        canEdit &&
        !remove.isPending &&
        !removeBatch.isPending &&
        !reorder.isPending &&
        !upload.isPending;

    function confirmBatchDeletion(title: string, items: Stem[]) {
        if (stems.data && items.length > 0 && canChangeStems)
            setBatchToDelete({
                title,
                stems: [...items],
                contentVersion: stems.data.contentVersion,
            });
    }

    return (
        <>
            <Card className="mt-6">
                <CardHeader>
                    <div className="flex items-start justify-between gap-4">
                        <div>
                            <CardTitle className="flex items-center gap-2">
                                <FileAudio className="h-5 w-5" />
                                Мультитреки
                            </CardTitle>
                            <CardDescription>
                                Создавайте свои группы и перетаскивайте дорожки
                                между ними или внутри группы.
                            </CardDescription>
                        </div>
                        {canEdit && (
                            <div className="flex flex-wrap justify-end gap-2">
                                <Button
                                    variant="destructive"
                                    disabled={
                                        !canChangeStems ||
                                        !stems.data?.stems.length
                                    }
                                    onClick={() =>
                                        confirmBatchDeletion(
                                            "Удалить все стемы песни?",
                                            stems.data?.stems ?? [],
                                        )
                                    }
                                >
                                    <Trash2 className="mr-2 h-4 w-4" />
                                    Удалить все
                                </Button>
                                <Button
                                    disabled={!canChangeStems}
                                    onClick={() => setUploadOpen(true)}
                                >
                                    <FolderUp className="mr-2 h-4 w-4" />
                                    Загрузить папку
                                </Button>
                            </div>
                        )}
                    </div>
                </CardHeader>
                <CardContent className="space-y-5">
                    {canEdit && (
                        <div className="rounded-lg border bg-muted/30 p-3">
                            <div className="flex gap-2">
                                <Input
                                    aria-label="Название новой группы"
                                    placeholder="Новая группа"
                                    value={newGroupName}
                                    onChange={(event) =>
                                        setNewGroupName(event.target.value)
                                    }
                                    onKeyDown={(event) => {
                                        if (
                                            event.key === "Enter" &&
                                            newGroupName.trim()
                                        )
                                            createGroup.mutate(newGroupName);
                                    }}
                                />
                                <Button
                                    disabled={
                                        !newGroupName.trim() ||
                                        createGroup.isPending
                                    }
                                    onClick={() =>
                                        createGroup.mutate(newGroupName)
                                    }
                                >
                                    <Plus className="mr-2 h-4 w-4" />
                                    Создать
                                </Button>
                            </div>
                            <div className="mt-2 flex flex-wrap gap-2">
                                {groupPresets
                                    .filter(
                                        (name) =>
                                            !stems.data?.groups.some(
                                                (group) =>
                                                    group.name.toLocaleLowerCase() ===
                                                    name.toLocaleLowerCase(),
                                            ),
                                    )
                                    .map((name) => (
                                        <Button
                                            key={name}
                                            size="sm"
                                            variant="ghost"
                                            disabled={createGroup.isPending}
                                            onClick={() =>
                                                createGroup.mutate(name)
                                            }
                                        >
                                            + {name}
                                        </Button>
                                    ))}
                            </div>
                        </div>
                    )}
                    {stems.isLoading ? (
                        <div>Загрузка…</div>
                    ) : stems.isError ? (
                        <div className="text-sm text-destructive">
                            Не удалось загрузить стемы
                        </div>
                    ) : stems.data?.stems.length === 0 &&
                      stems.data.groups.length === 0 ? (
                        <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
                            Мультитреков пока нет
                        </div>
                    ) : (
                        <DndContext
                            sensors={sensors}
                            collisionDetection={closestCenter}
                            onDragEnd={handleDragEnd}
                        >
                            <div className="space-y-4">
                                {stemGroups.map((group) => (
                                    <StemGroupSection
                                        key={group.id ?? ungroupedKey}
                                        groupId={group.id}
                                        name={group.name}
                                        stems={group.items}
                                        canEdit={canChangeStems}
                                        onDelete={setStemToDelete}
                                        onDeleteGroup={() =>
                                            confirmBatchDeletion(
                                                `Удалить все стемы из группы «${group.name}»?`,
                                                group.items,
                                            )
                                        }
                                    />
                                ))}
                            </div>
                        </DndContext>
                    )}
                </CardContent>
            </Card>

            <Card className="mt-6">
                <CardHeader>
                    <div className="flex items-start justify-between gap-4">
                        <div>
                            <CardTitle>Миксы для репетиций</CardTitle>
                            <CardDescription>
                                Focus выделяет партию, Minus отключает выбранную
                                дорожку.
                            </CardDescription>
                        </div>
                        {canEdit &&
                            stems.data?.stems.some(
                                (stem) => stem.fileStatus === "Ready",
                            ) && (
                                <Button
                                    variant="outline"
                                    disabled={
                                        regenerate.isPending ||
                                        ["Queued", "Processing"].includes(
                                            mixes.data?.status ?? "",
                                        )
                                    }
                                    onClick={() => regenerate.mutate()}
                                >
                                    <RefreshCw className="mr-2 h-4 w-4" />
                                    Пересобрать
                                </Button>
                            )}
                    </div>
                </CardHeader>
                <CardContent className="space-y-5">
                    {(mixes.data?.status === "Queued" ||
                        mixes.data?.status === "Processing") && (
                        <div className="flex items-start gap-2 text-muted-foreground">
                            <LoaderCircle className="mt-0.5 h-4 w-4 animate-spin" />
                            <div>
                                <div>
                                    Миксы создаются:{" "}
                                    {mixes.data.completedOutputCount ??
                                        mixes.data.artifacts.length}
                                    {mixes.data.totalOutputCount > 0
                                        ? ` из ${mixes.data.totalOutputCount}`
                                        : ""}
                                </div>
                                {mixes.data.currentStage && (
                                    <div className="text-xs">
                                        Этап: {mixes.data.currentStage}
                                        {mixes.data.currentPlan
                                            ? ` · ${mixes.data.currentPlan}`
                                            : ""}
                                    </div>
                                )}
                            </div>
                        </div>
                    )}
                    {mixes.data?.status === "Failed" && (
                        <div className="text-destructive">
                            Ошибка: {mixes.data.error}
                        </div>
                    )}
                    {groupedMixes.length ? (
                        <MixPlayer key={songId} groups={groupedMixes} />
                    ) : (
                        !["Queued", "Processing", "Failed"].includes(
                            mixes.data?.status ?? "",
                        ) && (
                            <div className="text-sm text-muted-foreground">
                                Готовых миксов пока нет
                            </div>
                        )
                    )}
                </CardContent>
            </Card>

            <Dialog
                open={batchToDelete !== null}
                onOpenChange={(open) => {
                    if (!open && !removeBatch.isPending) setBatchToDelete(null);
                }}
            >
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>{batchToDelete?.title}</DialogTitle>
                        <DialogDescription>
                            Будут удалены стемы ({batchToDelete?.stems.length})
                            и их исходные файлы навсегда. Для восстановления
                            потребуется повторная загрузка.
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                        <Button
                            variant="outline"
                            disabled={removeBatch.isPending}
                            onClick={() => setBatchToDelete(null)}
                        >
                            Отмена
                        </Button>
                        <Button
                            variant="destructive"
                            disabled={removeBatch.isPending || !canEdit}
                            onClick={() => {
                                if (batchToDelete)
                                    removeBatch.mutate(batchToDelete);
                            }}
                        >
                            {removeBatch.isPending && (
                                <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                            )}
                            Удалить файлы
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            <Dialog
                open={stemToDelete !== null}
                onOpenChange={(open) => {
                    if (!open && !remove.isPending) setStemToDelete(null);
                }}
            >
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>
                            Удалить стем «{stemToDelete?.name}»?
                        </DialogTitle>
                        <DialogDescription>
                            Стем и его исходный файл будут удалены навсегда.
                            Восстановить их можно только повторной загрузкой.
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                        <Button
                            variant="outline"
                            disabled={remove.isPending}
                            onClick={() => setStemToDelete(null)}
                        >
                            Отмена
                        </Button>
                        <Button
                            variant="destructive"
                            disabled={remove.isPending || !canEdit}
                            onClick={() => {
                                if (stemToDelete) remove.mutate(stemToDelete);
                            }}
                        >
                            {remove.isPending && (
                                <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                            )}
                            Удалить файл
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            <Dialog
                open={uploadOpen}
                onOpenChange={(open) => {
                    if (!upload.isPending) setUploadOpen(open);
                }}
            >
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Загрузка папки мультитреков</DialogTitle>
                        <DialogDescription>
                            Название дорожки берётся из имени файла без
                            расширения. Тип определяется автоматически.
                        </DialogDescription>
                    </DialogHeader>
                    <Input
                        type="file"
                        multiple
                        accept="audio/*,.wav,.mp3,.ogg,.opus,.flac,.m4a,.aac,.wma"
                        {...({ webkitdirectory: "", directory: "" } as Record<
                            string,
                            string
                        >)}
                        disabled={upload.isPending}
                        onChange={(event) => {
                            setFiles(
                                Array.from(event.target.files ?? []).filter(
                                    isSupportedAudio,
                                ),
                            );
                            setUploadError(null);
                        }}
                    />
                    <div className="text-sm text-muted-foreground">
                        Выбрано аудиофайлов: {files.length}
                    </div>
                    {files.length > 100 && (
                        <div role="alert" className="text-sm text-destructive">
                            За одну загрузку можно выбрать не больше 100
                            аудиофайлов.
                        </div>
                    )}
                    {uploadError && (
                        <div role="alert" className="text-sm text-destructive">
                            {uploadError}
                        </div>
                    )}
                    {upload.isPending && (
                        <div className="space-y-2">
                            <div role="status" className="text-sm">
                                {uploadPhase === "preparing"
                                    ? "Подготовка файлов…"
                                    : uploadPhase === "committing"
                                      ? "Сохранение мультитрека…"
                                      : uploadPhase === "cleaning"
                                        ? "Очистка незавершённой загрузки…"
                                        : `Загрузка и проверка файлов: ${progress}%`}
                            </div>
                            <div className="h-2 overflow-hidden rounded-full bg-muted">
                                <div
                                    className="h-full bg-primary"
                                    style={{ width: `${progress}%` }}
                                />
                            </div>
                        </div>
                    )}
                    <DialogFooter>
                        <Button
                            variant="outline"
                            disabled={
                                upload.isPending &&
                                ["committing", "cleaning"].includes(uploadPhase)
                            }
                            onClick={() => {
                                if (upload.isPending)
                                    uploadController.current?.abort();
                                else setUploadOpen(false);
                            }}
                        >
                            Отмена
                        </Button>
                        <Button
                            disabled={
                                files.length === 0 ||
                                files.length > 100 ||
                                upload.isPending
                            }
                            onClick={() => {
                                const controller = new AbortController();
                                uploadController.current = controller;
                                setUploadError(null);
                                setProgress(0);
                                setUploadPhase("preparing");
                                upload.mutate(controller);
                            }}
                        >
                            {upload.isPending && (
                                <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                            )}
                            Загрузить
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </>
    );
}

function StemGroupSection({
    groupId,
    name,
    stems,
    canEdit,
    onDelete,
    onDeleteGroup,
}: {
    groupId: string | null;
    name: string;
    stems: Stem[];
    canEdit: boolean;
    onDelete: (stem: Stem) => void;
    onDeleteGroup: () => void;
}) {
    const { setNodeRef, isOver } = useDroppable({
        id: groupDndId(groupId),
        disabled: !canEdit,
    });
    return (
        <section
            ref={setNodeRef}
            className={`rounded-lg border p-3 transition-colors ${isOver ? "border-primary bg-primary/5" : ""}`}
        >
            <div className="mb-3 flex items-center justify-between gap-2">
                <h3 className="flex items-center gap-2 font-semibold">
                    <span>{name}</span>
                    <Badge>{stems.length}</Badge>
                </h3>
                {canEdit && stems.length > 0 && (
                    <Button
                        variant="ghost"
                        size="sm"
                        className="text-destructive hover:text-destructive"
                        aria-label={`Удалить все стемы из группы ${name}`}
                        onClick={onDeleteGroup}
                    >
                        <Trash2 className="mr-2 h-4 w-4" />
                        Удалить стемы
                    </Button>
                )}
            </div>
            <SortableContext
                items={stems.map((stem) => stemDndId(stem.id))}
                strategy={verticalListSortingStrategy}
            >
                <div className="grid min-h-14 gap-2 md:grid-cols-2">
                    {stems.map((stem) => (
                        <SortableStem
                            key={stem.id}
                            stem={stem}
                            canEdit={canEdit}
                            onDelete={onDelete}
                        />
                    ))}
                    {stems.length === 0 && (
                        <div className="flex items-center justify-center rounded-md border border-dashed p-4 text-sm text-muted-foreground md:col-span-2">
                            Перетащите стемы сюда
                        </div>
                    )}
                </div>
            </SortableContext>
        </section>
    );
}

function SortableStem({
    stem,
    canEdit,
    onDelete,
}: {
    stem: Stem;
    canEdit: boolean;
    onDelete: (stem: Stem) => void;
}) {
    const {
        attributes,
        listeners,
        setNodeRef,
        transform,
        transition,
        isDragging,
    } = useSortable({
        id: stemDndId(stem.id),
        disabled: !canEdit,
    });
    return (
        <div
            ref={setNodeRef}
            style={{ transform: CSS.Transform.toString(transform), transition }}
            className={`flex items-center justify-between rounded-lg border bg-background p-3 ${isDragging ? "z-10 opacity-60 shadow-lg" : ""}`}
        >
            <div className="flex min-w-0 items-center gap-2">
                {canEdit && (
                    <button
                        type="button"
                        className="cursor-grab touch-none text-muted-foreground active:cursor-grabbing"
                        aria-label={`Переместить ${stem.name}`}
                        {...attributes}
                        {...listeners}
                    >
                        <GripVertical className="h-5 w-5" />
                    </button>
                )}
                <div className="min-w-0">
                    <div className="truncate font-medium">{stem.name}</div>
                    <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <span>{kindLabels[stem.kind]}</span>
                        <span>{formatBytes(stem.size)}</span>
                    </div>
                </div>
            </div>
            <div className="flex shrink-0 items-center gap-2">
                <Badge>{stem.fileStatus}</Badge>
                {canEdit && (
                    <Button
                        variant="ghost"
                        size="icon"
                        className="text-destructive hover:text-destructive"
                        aria-label={`Удалить стем ${stem.name}`}
                        onClick={() => onDelete(stem)}
                    >
                        <Trash2 className="h-4 w-4" />
                    </Button>
                )}
            </div>
        </div>
    );
}
