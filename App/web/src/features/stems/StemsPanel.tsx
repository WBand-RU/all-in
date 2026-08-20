import { useMemo, useState } from "react";
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
import { Download, FileAudio, FolderUp, GripVertical, LoaderCircle, Plus, RefreshCw } from "lucide-react";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/shared/ui/dialog";
import { Input } from "@/shared/ui/input";
import { useToast } from "@/shared/ui/use-toast";
import {
    audioMimeType,
    completeStemFile,
    createStemBatch,
    createStemGroup,
    formatBytes,
    getMixes,
    getStems,
    isSupportedAudio,
    queueMixes,
    reorderStems,
    sha256,
    stemQueryKey,
    type Stem,
    type StemCollection,
    type StemKind,
    uploadToSignedUrl,
} from "./api";

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
const groupDndId = (groupId: string | null) => `group:${groupId ?? ungroupedKey}`;
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

export function StemsPanel({ songId, canEdit }: { songId: string; canEdit: boolean }) {
    const queryClient = useQueryClient();
    const { toast } = useToast();
    const [uploadOpen, setUploadOpen] = useState(false);
    const [files, setFiles] = useState<File[]>([]);
    const [progress, setProgress] = useState(0);
    const [newGroupName, setNewGroupName] = useState("");
    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
        useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
    );
    const stems = useQuery({ queryKey: stemQueryKey(songId), queryFn: () => getStems(songId) });
    const mixes = useQuery({ queryKey: ["mixes", songId], queryFn: () => getMixes(songId),
        refetchInterval: query => ["Queued", "Processing"].includes(query.state.data?.status ?? "") ? 3000 : false });

    const stemGroups = useMemo(() => {
        const collection = stems.data;
        if (!collection) return [];
        const groups = [...collection.groups].sort((left, right) => left.order - right.order)
            .map(group => ({ id: group.id as string | null, name: group.name }));
        groups.push({ id: null, name: "Без группы" });
        return groups.map(group => ({ ...group, items: collection.stems
            .filter(stem => (stem.groupId ?? null) === group.id)
            .sort((left, right) => left.order - right.order || left.name.localeCompare(right.name)) }));
    }, [stems.data]);
    const groupedMixes = useMemo(() => Object.entries((mixes.data?.artifacts ?? []).reduce(
        (result, artifact) => { (result[artifact.group] ??= []).push(artifact); return result; },
        {} as Record<string, NonNullable<typeof mixes.data>["artifacts"]>)), [mixes.data]);

    const createGroup = useMutation({
        mutationFn: (name: string) => createStemGroup(songId, name.trim(), stems.data?.groups.length ?? 0),
        onSuccess: async () => {
            setNewGroupName("");
            await queryClient.invalidateQueries({ queryKey: stemQueryKey(songId) });
        },
        onError: () => toast({ title: "Не удалось создать группу", variant: "destructive" }),
    });
    const reorder = useMutation({
        mutationFn: ({ collection, items }: { collection: StemCollection; items: StemCollection["stems"] }) =>
            reorderStems(songId, collection.contentVersion, items.map(stem => ({
                stemId: stem.id,
                groupId: stem.groupId ?? null,
                order: stem.order,
                expectedRevision: stem.revision,
            }))),
        onMutate: async ({ collection, items }) => {
            await queryClient.cancelQueries({ queryKey: stemQueryKey(songId) });
            const previous = queryClient.getQueryData<StemCollection>(stemQueryKey(songId));
            queryClient.setQueryData<StemCollection>(stemQueryKey(songId), { ...collection, stems: items });
            return { previous };
        },
        onError: (_error, _variables, context) => {
            if (context?.previous) queryClient.setQueryData(stemQueryKey(songId), context.previous);
            toast({ title: "Не удалось изменить порядок стемов", description: "Список обновлён с сервера. Повторите перенос.", variant: "destructive" });
        },
        onSettled: () => queryClient.invalidateQueries({ queryKey: stemQueryKey(songId) }),
    });

    function handleDragEnd(event: DragEndEvent) {
        if (!canEdit || !event.over || !stems.data || event.active.id === event.over.id) return;
        const activeId = String(event.active.id).replace(/^stem:/, "");
        const activeStem = stems.data.stems.find(stem => stem.id === activeId);
        if (!activeStem) return;

        const overId = String(event.over.id);
        const overStem = overId.startsWith("stem:")
            ? stems.data.stems.find(stem => stem.id === overId.replace(/^stem:/, ""))
            : undefined;
        const targetGroupId = overStem?.groupId ?? (overId.startsWith("group:")
            ? (overId.replace(/^group:/, "") === ungroupedKey ? null : overId.replace(/^group:/, ""))
            : null);

        const buckets = new Map<string, Stem[]>();
        for (const stem of stems.data.stems) {
            const key = stem.groupId ?? ungroupedKey;
            const bucket = buckets.get(key) ?? [];
            bucket.push(stem);
            buckets.set(key, bucket);
        }
        for (const [key, bucket] of buckets) {
            buckets.set(key, bucket.sort((left, right) => left.order - right.order || left.name.localeCompare(right.name))
                .filter(stem => stem.id !== activeStem.id));
        }
        const targetKey = targetGroupId ?? ungroupedKey;
        const target = buckets.get(targetKey) ?? [];
        const overIndex = overStem ? target.findIndex(stem => stem.id === overStem.id) : target.length;
        target.splice(overIndex < 0 ? target.length : overIndex, 0, { ...activeStem, groupId: targetGroupId });
        buckets.set(targetKey, target);

        const ordered = Array.from(buckets.values()).flatMap(bucket => bucket.map((stem, order) => ({ ...stem, order })));
        reorder.mutate({ collection: stems.data, items: ordered });
    }

    const upload = useMutation({
        mutationFn: async () => {
            const audioFiles = files.filter(isSupportedAudio);
            const inputs = [];
            for (const file of audioFiles) inputs.push({ name: file.name.replace(/\.[^.]+$/, ""),
                kind: inferKind(file.name), groupId: null, fileName: file.name,
                mimeType: audioMimeType(file), size: file.size, sha256: await sha256(file) });
            const batch = await createStemBatch(songId, inputs);
            for (let index = 0; index < batch.files.length; index++) {
                const ticket = batch.files[index];
                await uploadToSignedUrl(audioFiles[index], ticket.uploadUrl,
                    current => setProgress(Math.round((index + current / 100) / batch.files.length * 100)));
                await completeStemFile(ticket.stem.fileId);
            }
            return queueMixes(songId);
        },
        onSuccess: async () => {
            await Promise.all([queryClient.invalidateQueries({ queryKey: stemQueryKey(songId) }),
                queryClient.invalidateQueries({ queryKey: ["mixes", songId] })]);
            setUploadOpen(false); setFiles([]); setProgress(0);
            toast({ title: "Мультитреки загружены", description: "Создание миксов поставлено в очередь." });
        },
        onError: () => toast({ title: "Не удалось загрузить папку", variant: "destructive" }),
    });
    const regenerate = useMutation({ mutationFn: () => queueMixes(songId),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ["mixes", songId] }),
        onError: () => toast({ title: "Не удалось поставить миксы в очередь", variant: "destructive" }) });

    return <>
        <Card className="mt-6"><CardHeader><div className="flex items-start justify-between gap-4">
            <div><CardTitle className="flex items-center gap-2"><FileAudio className="h-5 w-5" />Мультитреки</CardTitle>
                <CardDescription>Создавайте свои группы и перетаскивайте дорожки между ними или внутри группы.</CardDescription></div>
            {canEdit && <Button onClick={() => setUploadOpen(true)}><FolderUp className="mr-2 h-4 w-4" />Загрузить папку</Button>}
        </div></CardHeader><CardContent className="space-y-5">
            {canEdit && <div className="rounded-lg border bg-muted/30 p-3"><div className="flex gap-2">
                <Input aria-label="Название новой группы" placeholder="Новая группа" value={newGroupName}
                    onChange={event => setNewGroupName(event.target.value)}
                    onKeyDown={event => { if (event.key === "Enter" && newGroupName.trim()) createGroup.mutate(newGroupName); }} />
                <Button disabled={!newGroupName.trim() || createGroup.isPending} onClick={() => createGroup.mutate(newGroupName)}>
                    <Plus className="mr-2 h-4 w-4" />Создать</Button>
            </div><div className="mt-2 flex flex-wrap gap-2">{groupPresets
                .filter(name => !stems.data?.groups.some(group => group.name.toLocaleLowerCase() === name.toLocaleLowerCase()))
                .map(name => <Button key={name} size="sm" variant="ghost" disabled={createGroup.isPending}
                    onClick={() => createGroup.mutate(name)}>+ {name}</Button>)}</div></div>}
            {stems.isLoading ? <div>Загрузка…</div> : stems.isError
                ? <div className="text-sm text-destructive">Не удалось загрузить стемы</div>
                : stems.data?.stems.length === 0 && stems.data.groups.length === 0
                    ? <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">Мультитреков пока нет</div>
                    : <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
                        <div className="space-y-4">{stemGroups.map(group => <StemGroupSection key={group.id ?? ungroupedKey}
                            groupId={group.id} name={group.name} stems={group.items} canEdit={canEdit} />)}</div>
                    </DndContext>}
        </CardContent></Card>

        <Card className="mt-6"><CardHeader><div className="flex items-start justify-between gap-4">
            <div><CardTitle>Миксы для репетиций</CardTitle><CardDescription>Focus выделяет партию, Minus отключает выбранную дорожку.</CardDescription></div>
            {canEdit && stems.data?.stems.some(stem => stem.fileStatus === "Ready") &&
                <Button variant="outline" disabled={regenerate.isPending || ["Queued", "Processing"].includes(mixes.data?.status ?? "")}
                    onClick={() => regenerate.mutate()}><RefreshCw className="mr-2 h-4 w-4" />Пересобрать</Button>}
        </div></CardHeader><CardContent>
            {mixes.data?.status === "Queued" || mixes.data?.status === "Processing"
                ? <div className="flex items-start gap-2 text-muted-foreground"><LoaderCircle className="mt-0.5 h-4 w-4 animate-spin" />
                    <div><div>Миксы создаются: {mixes.data.completedOutputCount ?? mixes.data.artifacts.length}
                        {mixes.data.totalOutputCount > 0 ? ` из ${mixes.data.totalOutputCount}` : ""}</div>
                        {mixes.data.currentStage && <div className="text-xs">Этап: {mixes.data.currentStage}
                            {mixes.data.currentPlan ? ` · ${mixes.data.currentPlan}` : ""}</div>}</div></div>
                : mixes.data?.status === "Failed" ? <div className="text-destructive">Ошибка: {mixes.data.error}</div>
                : groupedMixes.length ? <div className="space-y-6">{groupedMixes.map(([group, artifacts]) => <section key={group}>
                    <h3 className="mb-2 font-semibold">{group}</h3><div className="grid gap-2 md:grid-cols-2">
                    {artifacts.map(artifact => <div key={artifact.id} className="flex items-center justify-between rounded-lg border p-3">
                        <div><div className="font-medium">{artifact.name}</div><div className="flex gap-2"><Badge>{artifact.kind}</Badge><Badge>{artifact.format.toUpperCase()}</Badge></div></div>
                        {artifact.downloadUrl && <Button variant="outline" size="sm" asChild><a href={artifact.downloadUrl} target="_blank" rel="noreferrer">
                            <Download className="mr-2 h-4 w-4" />Скачать</a></Button>}
                    </div>)}</div></section>)}</div>
                : <div className="text-sm text-muted-foreground">Готовых миксов пока нет</div>}
        </CardContent></Card>

        <Dialog open={uploadOpen} onOpenChange={open => { if (!upload.isPending) setUploadOpen(open); }}>
            <DialogContent><DialogHeader><DialogTitle>Загрузка папки мультитреков</DialogTitle>
                <DialogDescription>Название дорожки берётся из имени файла без расширения. Тип определяется автоматически.</DialogDescription></DialogHeader>
                <Input type="file" multiple accept="audio/*,.wav,.mp3,.ogg,.opus,.flac,.m4a,.aac,.wma"
                    {...({ webkitdirectory: "", directory: "" } as Record<string, string>)}
                    disabled={upload.isPending} onChange={event => setFiles(Array.from(event.target.files ?? []).filter(isSupportedAudio))} />
                <div className="text-sm text-muted-foreground">Выбрано аудиофайлов: {files.length}</div>
                {upload.isPending && <div className="space-y-2"><div className="text-sm">Загрузка: {progress}%</div>
                    <div className="h-2 overflow-hidden rounded-full bg-muted"><div className="h-full bg-primary" style={{ width: `${progress}%` }} /></div></div>}
                <DialogFooter><Button variant="outline" disabled={upload.isPending} onClick={() => setUploadOpen(false)}>Отмена</Button>
                    <Button disabled={files.length === 0 || upload.isPending} onClick={() => upload.mutate()}>
                        {upload.isPending && <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />}Загрузить</Button></DialogFooter>
            </DialogContent>
        </Dialog>
    </>;
}

function StemGroupSection({ groupId, name, stems, canEdit }: {
    groupId: string | null; name: string; stems: Stem[]; canEdit: boolean;
}) {
    const { setNodeRef, isOver } = useDroppable({ id: groupDndId(groupId), disabled: !canEdit });
    return <section ref={setNodeRef} className={`rounded-lg border p-3 transition-colors ${isOver ? "border-primary bg-primary/5" : ""}`}>
        <h3 className="mb-3 flex items-center gap-2 font-semibold"><span>{name}</span><Badge>{stems.length}</Badge></h3>
        <SortableContext items={stems.map(stem => stemDndId(stem.id))} strategy={verticalListSortingStrategy}>
            <div className="grid min-h-14 gap-2 md:grid-cols-2">{stems.map(stem =>
                <SortableStem key={stem.id} stem={stem} canEdit={canEdit} />)}
                {stems.length === 0 && <div className="flex items-center justify-center rounded-md border border-dashed p-4 text-sm text-muted-foreground md:col-span-2">
                    Перетащите стемы сюда
                </div>}
            </div>
        </SortableContext>
    </section>;
}

function SortableStem({ stem, canEdit }: { stem: Stem; canEdit: boolean }) {
    const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
        id: stemDndId(stem.id),
        disabled: !canEdit,
    });
    return <div ref={setNodeRef} style={{ transform: CSS.Transform.toString(transform), transition }}
        className={`flex items-center justify-between rounded-lg border bg-background p-3 ${isDragging ? "z-10 opacity-60 shadow-lg" : ""}`}>
        <div className="flex min-w-0 items-center gap-2">
            {canEdit && <button type="button" className="cursor-grab touch-none text-muted-foreground active:cursor-grabbing"
                aria-label={`Переместить ${stem.name}`} {...attributes} {...listeners}><GripVertical className="h-5 w-5" /></button>}
            <div className="min-w-0"><div className="truncate font-medium">{stem.name}</div>
                <div className="flex items-center gap-2 text-sm text-muted-foreground"><span>{kindLabels[stem.kind]}</span><span>{formatBytes(stem.size)}</span></div></div>
        </div>
        <Badge>{stem.fileStatus}</Badge>
    </div>;
}
