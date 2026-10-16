import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronDown, FileText, Plus, Save, Trash2 } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/shared/ui/collapsible";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/shared/ui/dialog";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";
import { Textarea } from "@/shared/ui/textarea";
import { useToast } from "@/shared/ui/use-toast";
import {
    createSongSection, deleteSongSection, getSongSections, songSectionsKey,
    type SongSection, updateSongSection,
} from "./api";

const sectionPresets = ["Интро", "Куплет", "Припев", "Бридж", "Проигрыш", "Аутро"];

export function SongSectionsPanel({ songId, canEdit }: { songId: string; canEdit: boolean }) {
    const queryClient = useQueryClient();
    const { toast } = useToast();
    const [createOpen, setCreateOpen] = useState(false);
    const [newName, setNewName] = useState("Куплет");
    const query = useQuery({ queryKey: songSectionsKey(songId), queryFn: () => getSongSections(songId) });
    const invalidate = () => queryClient.invalidateQueries({ queryKey: songSectionsKey(songId) });
    const create = useMutation({
        mutationFn: () => createSongSection(songId, {
            name: newName.trim(),
            order: Math.max(-1, ...(query.data ?? []).map(section => Number(section.order))) + 1,
            lyrics: null, chords: null,
        }),
        onSuccess: async () => { await invalidate(); setCreateOpen(false); },
        onError: () => toast({ title: "Не удалось добавить секцию", variant: "destructive" }),
    });
    const remove = useMutation({
        mutationFn: deleteSongSection,
        onSuccess: invalidate,
        onError: () => toast({ title: "Не удалось удалить секцию", variant: "destructive" }),
    });

    return <Card className="mt-6">
        <CardHeader>
            <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                    <CardTitle className="flex items-center gap-2"><FileText className="h-5 w-5" />Текст и аккорды</CardTitle>
                    <CardDescription>Секции с обычным текстом и аккордами.</CardDescription>
                </div>
                {canEdit && <Button onClick={() => setCreateOpen(true)}><Plus className="mr-2 h-4 w-4" />Добавить секцию</Button>}
            </div>
        </CardHeader>
        <CardContent>
            {query.isLoading ? <div className="text-sm text-muted-foreground">Загрузка…</div>
                : query.isError ? <div className="text-sm text-destructive" role="alert">Не удалось загрузить секции</div>
                : query.data?.length ? <div className="space-y-3">{query.data.map(section =>
                    <SectionEditor key={section.id} section={section} canEdit={canEdit}
                        onSaved={invalidate} deleting={remove.isPending && remove.variables === section.id}
                        onDelete={() => remove.mutate(section.id)} />)}</div>
                : <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">Секций пока нет</div>}
        </CardContent>
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
            <DialogContent>
                <DialogHeader><DialogTitle>Новая секция</DialogTitle><DialogDescription>Назовите секцию, затем добавьте текст и аккорды.</DialogDescription></DialogHeader>
                <div className="space-y-3">
                    <Label htmlFor="section-name">Название</Label>
                    <Input id="section-name" maxLength={100} value={newName} onChange={event => setNewName(event.target.value)} />
                    <div className="flex flex-wrap gap-2">{sectionPresets.map(name =>
                        <Button key={name} type="button" size="sm" variant="outline" onClick={() => setNewName(name)}>{name}</Button>)}</div>
                </div>
                <DialogFooter>
                    <Button variant="outline" onClick={() => setCreateOpen(false)}>Отмена</Button>
                    <Button disabled={!newName.trim() || create.isPending} onClick={() => create.mutate()}>Добавить</Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    </Card>;
}

function SectionEditor({ section, canEdit, onSaved, onDelete, deleting }: {
    section: SongSection; canEdit: boolean; onSaved: () => Promise<unknown>; onDelete: () => void; deleting: boolean;
}) {
    const { toast } = useToast();
    const [open, setOpen] = useState(false);
    const [name, setName] = useState(section.name);
    const [lyrics, setLyrics] = useState(section.lyrics ?? "");
    const [chords, setChords] = useState(section.chords ?? "");
    const [revision, setRevision] = useState(section.revision);
    const [conflict, setConflict] = useState(false);
    const save = useMutation({
        mutationFn: () => updateSongSection({ ...section, revision }, {
            name: name.trim(), order: section.order, lyrics, chords,
        }),
        onSuccess: async saved => {
            setRevision(saved.revision);
            setName(saved.name);
            setConflict(false);
            await onSaved();
            toast({ title: "Секция сохранена" });
        },
        onError: async error => {
            const isConflict = typeof error === "object" && error !== null && "code" in error
                && error.code === "song_section_version_conflict";
            setConflict(isConflict);
            if (isConflict) await onSaved();
            toast({ title: isConflict ? "Секция изменена другим участником" : "Не удалось сохранить секцию", variant: "destructive" });
        },
    });
    const reload = () => {
        setName(section.name);
        setLyrics(section.lyrics ?? "");
        setChords(section.chords ?? "");
        setRevision(section.revision);
        setConflict(false);
    };

    return <Collapsible open={open} onOpenChange={setOpen} className="rounded-lg border">
        <div className="flex items-center gap-2 p-3">
            <CollapsibleTrigger asChild>
                <button type="button" className="flex flex-1 items-center justify-between text-left font-medium">
                    <span>{section.name}</span>
                    <ChevronDown className={`h-4 w-4 transition-transform ${open ? "rotate-180" : ""}`} />
                </button>
            </CollapsibleTrigger>
            {canEdit && <Button size="icon" variant="ghost" aria-label={`Удалить секцию ${section.name}`} disabled={deleting || save.isPending} onClick={onDelete}><Trash2 className="h-4 w-4" /></Button>}
        </div>
        <CollapsibleContent>
            <div className="grid gap-4 border-t p-4 md:grid-cols-2">
                {canEdit && <div className="space-y-2 md:col-span-2">
                    <Label htmlFor={`section-${section.id}-name`}>Название</Label>
                    <Input id={`section-${section.id}-name`} maxLength={100} disabled={save.isPending} value={name} onChange={event => setName(event.target.value)} />
                </div>}
                <div className="space-y-2">
                    <Label htmlFor={`section-${section.id}-lyrics`}>Текст</Label>
                    {canEdit ? <Textarea id={`section-${section.id}-lyrics`} rows={10} maxLength={50000} disabled={save.isPending} value={lyrics} onChange={event => setLyrics(event.target.value)} />
                        : <div className="whitespace-pre-wrap rounded-md bg-muted p-4">{section.lyrics || "—"}</div>}
                </div>
                <div className="space-y-2">
                    <Label htmlFor={`section-${section.id}-chords`}>Аккорды</Label>
                    {canEdit ? <Textarea id={`section-${section.id}-chords`} rows={10} maxLength={50000} className="font-mono" disabled={save.isPending} value={chords} onChange={event => setChords(event.target.value)} />
                        : <div className="whitespace-pre-wrap rounded-md bg-muted p-4 font-mono">{section.chords || "—"}</div>}
                </div>
                {conflict && <div className="space-y-2 md:col-span-2" role="alert">
                    <p>Ваш текст сохранён в редакторе. Загрузите текущую версию, чтобы продолжить редактирование; она заменит несохранённый текст.</p>
                    <Button variant="outline" onClick={reload}>Загрузить текущую версию</Button>
                </div>}
                {canEdit && <div className="flex justify-end md:col-span-2">
                    <Button disabled={!name.trim() || save.isPending || conflict} onClick={() => save.mutate()}><Save className="mr-2 h-4 w-4" />Сохранить</Button>
                </div>}
            </div>
        </CollapsibleContent>
    </Collapsible>;
}
