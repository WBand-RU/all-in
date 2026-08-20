import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronDown, FileText, Plus, Save, Trash2 } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/shared/ui/collapsible";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/shared/ui/dialog";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";
import { Textarea } from "@/shared/ui/textarea";
import { useToast } from "@/shared/ui/use-toast";
import { createSongSection, deleteSongSection, getSongSections, songSectionsKey,
    type SongSection, updateSongSection } from "./api";

const sectionPresets = ["Интро", "Куплет", "Припев", "Бридж", "Проигрыш", "Аутро"];

export function SongSectionsPanel({ songId, canEdit }: { songId: string; canEdit: boolean }) {
    const queryClient = useQueryClient();
    const { toast } = useToast();
    const [createOpen, setCreateOpen] = useState(false);
    const [newName, setNewName] = useState("Куплет");
    const [newBarCount, setNewBarCount] = useState(8);
    const query = useQuery({ queryKey: songSectionsKey(songId), queryFn: () => getSongSections(songId) });
    const invalidate = () => queryClient.invalidateQueries({ queryKey: songSectionsKey(songId) });
    const create = useMutation({
        mutationFn: () => createSongSection(songId, { name: newName.trim(),
            order: query.data?.length ?? 0, barCount: newBarCount, lyrics: null, chords: null }),
        onSuccess: async () => { await invalidate(); setCreateOpen(false); },
        onError: () => toast({ title: "Не удалось добавить секцию", variant: "destructive" }),
    });
    const remove = useMutation({ mutationFn: deleteSongSection, onSuccess: invalidate,
        onError: () => toast({ title: "Не удалось удалить секцию", variant: "destructive" }) });

    return <Card className="mt-6">
        <CardHeader><div className="flex items-start justify-between gap-4">
            <div><CardTitle className="flex items-center gap-2"><FileText className="h-5 w-5" />Текст и аккорды</CardTitle>
                <CardDescription>Один набор секций для текста и аккордов. Секции свернуты по умолчанию.</CardDescription></div>
            {canEdit && <Button onClick={() => setCreateOpen(true)}><Plus className="mr-2 h-4 w-4" />Добавить секцию</Button>}
        </div></CardHeader>
        <CardContent>{query.isLoading ? <div className="text-sm text-muted-foreground">Загрузка…</div>
            : query.isError ? <div className="text-sm text-destructive">Не удалось загрузить секции</div>
            : query.data?.length ? <div className="space-y-3">{query.data.map(section =>
                <SectionEditor key={section.id} section={section} canEdit={canEdit}
                    onSaved={invalidate} onDelete={() => remove.mutate(section.id)} />)}</div>
            : <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">Секций пока нет</div>}
        </CardContent>
        <Dialog open={createOpen} onOpenChange={setCreateOpen}><DialogContent>
            <DialogHeader><DialogTitle>Новая секция</DialogTitle></DialogHeader>
            <div className="space-y-3"><Label htmlFor="section-name">Название</Label>
                <Input id="section-name" value={newName} onChange={event => setNewName(event.target.value)} />
                <div className="flex flex-wrap gap-2">{sectionPresets.map(name =>
                    <Button key={name} type="button" size="sm" variant="outline" onClick={() => setNewName(name)}>{name}</Button>)}</div>
                <Label htmlFor="section-bars">Количество тактов</Label>
                <Input id="section-bars" type="number" min={1} max={10000} value={newBarCount}
                    onChange={event => setNewBarCount(Number(event.target.value))} />
            </div>
            <DialogFooter><Button variant="outline" onClick={() => setCreateOpen(false)}>Отмена</Button>
                <Button disabled={!newName.trim() || create.isPending} onClick={() => create.mutate()}>Добавить</Button></DialogFooter>
        </DialogContent></Dialog>
    </Card>;
}

function SectionEditor({ section, canEdit, onSaved, onDelete }: { section: SongSection;
    canEdit: boolean; onSaved: () => Promise<unknown>; onDelete: () => void }) {
    const { toast } = useToast();
    const [open, setOpen] = useState(false);
    const [name, setName] = useState(section.name);
    const [barCount, setBarCount] = useState(section.barCount);
    const [lyrics, setLyrics] = useState(section.lyrics ?? "");
    const [chords, setChords] = useState(section.chords ?? "");
    const save = useMutation({
        mutationFn: () => updateSongSection(section, { name: name.trim(), order: section.order,
            barCount, lyrics: lyrics || null, chords: chords || null }),
        onSuccess: async () => { await onSaved(); toast({ title: "Секция сохранена" }); },
        onError: () => toast({ title: "Не удалось сохранить секцию", variant: "destructive" }),
    });
    return <Collapsible open={open} onOpenChange={setOpen} className="rounded-lg border">
        <div className="flex items-center gap-2 p-3"><CollapsibleTrigger asChild>
            <button type="button" className="flex flex-1 items-center justify-between text-left font-medium">
                <span className="flex items-center gap-2"><span>{section.name}</span>
                    <span className="text-sm font-normal text-muted-foreground">{section.barCount} такт.</span></span>
                <ChevronDown className={`h-4 w-4 transition-transform ${open ? "rotate-180" : ""}`} />
            </button></CollapsibleTrigger>
            {canEdit && <Button size="icon" variant="ghost" title="Удалить секцию" onClick={onDelete}><Trash2 className="h-4 w-4" /></Button>}
        </div>
        <CollapsibleContent><div className="grid gap-4 border-t p-4 md:grid-cols-2">
            {canEdit && <><div className="space-y-2"><Label>Название</Label><Input value={name} onChange={event => setName(event.target.value)} /></div>
                <div className="space-y-2"><Label>Количество тактов</Label><Input type="number" min={1} max={10000}
                    value={barCount} onChange={event => setBarCount(Number(event.target.value))} /></div></>}
            <div className="space-y-2"><Label>Текст</Label>{canEdit
                ? <Textarea rows={10} value={lyrics} onChange={event => setLyrics(event.target.value)} />
                : <div className="whitespace-pre-wrap rounded-md bg-muted p-4">{lyrics || "—"}</div>}</div>
            <div className="space-y-2"><Label>Аккорды</Label>{canEdit
                ? <Textarea rows={10} className="font-mono" value={chords} onChange={event => setChords(event.target.value)} />
                : <div className="whitespace-pre-wrap rounded-md bg-muted p-4 font-mono">{chords || "—"}</div>}</div>
            {canEdit && <div className="flex justify-end md:col-span-2"><Button disabled={!name.trim() || barCount < 1 || save.isPending} onClick={() => save.mutate()}>
                <Save className="mr-2 h-4 w-4" />Сохранить</Button></div>}
        </div></CollapsibleContent>
    </Collapsible>;
}
