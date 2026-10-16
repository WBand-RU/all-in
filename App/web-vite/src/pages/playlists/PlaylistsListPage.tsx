import { useState } from "react";
import { useNavigate } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Calendar, Edit, ListMusic, Plus, Search, Trash2 } from "lucide-react";
import { useTranslation } from "react-i18next";
import { apiClient } from "@/lib/axios-instance";
import { useGetListOfBands } from "@/lib/generated-api/band-api/bands";
import { useBandAccess } from "@/hooks/use-band-access";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/shared/ui/dialog";
import { Input } from "@/shared/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/shared/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/shared/ui/table";
import { useToast } from "@/shared/ui/use-toast";

export interface PlaylistItemDto {
    id: string;
    songId: string;
    songTitle: string;
    songContentVersion: number;
    order: number;
    keyOverride?: string | null;
    bpmOverride?: number | null;
    structureOverride: { name: string; startBar: number; endBar: number }[];
    stemMixOverrides: { stemId: string; volume: number; pan: number }[];
    transition: "AutoStart" | "Pause" | "Crossfade";
    pauseSeconds: number;
    crossfadeSeconds: number;
    notes?: string | null;
}

export interface PlaylistDto {
    id: string;
    bandId: string;
    title: string;
    description?: string | null;
    eventDateTime?: string | null;
    venue?: string | null;
    items: PlaylistItemDto[];
    contentVersion: number;
    createdAt: string;
}

export function PlaylistsListPage() {
    const navigate = useNavigate();
    const { t, i18n } = useTranslation();
    const { toast } = useToast();
    const queryClient = useQueryClient();
    const { access } = useBandAccess();
    const { data: bandsData } = useGetListOfBands();
    const bands = bandsData?.value ?? [];
    const bandNames = new Map(bands.map((band) => [band.id, band.name]));
    const [bandId, setBandId] = useState("all");
    const [searchTerm, setSearchTerm] = useState("");
    const [playlistToDelete, setPlaylistToDelete] = useState<PlaylistDto | null>(null);
    const canCreate = access.some((item) => item.canEditContent);

    const { data: playlists = [], isLoading } = useQuery({
        queryKey: ["playlists", bandId, searchTerm],
        queryFn: async () => (await apiClient.get<{ value: PlaylistDto[] }>("/playlist-api/playlists", {
            params: { bandId: bandId === "all" ? undefined : bandId, searchTerm: searchTerm || undefined },
        })).data.value ?? [],
    });
    const remove = useMutation({
        mutationFn: (id: string) => apiClient.delete(`/playlist-api/playlists/${id}`),
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ["playlists"] });
            setPlaylistToDelete(null);
            toast({ title: t("playlists.deleted") });
        },
        onError: () => toast({ title: t("playlists.deleteFailed"), variant: "destructive" }),
    });

    return <div className="container mx-auto py-8">
        <Card>
            <CardHeader>
                <div className="flex items-center justify-between gap-4">
                    <div><CardTitle>{t("playlists.title")}</CardTitle><CardDescription>{t("playlists.description")}</CardDescription></div>
                    {canCreate && <Button onClick={() => navigate("/app/playlists/new")}><Plus className="mr-2 h-4 w-4" />{t("playlists.create")}</Button>}
                </div>
            </CardHeader>
            <CardContent>
                <div className="mb-6 grid gap-4 md:grid-cols-2">
                    <Select value={bandId} onValueChange={setBandId}><SelectTrigger><SelectValue /></SelectTrigger><SelectContent>
                        <SelectItem value="all">{t("common.allBands")}</SelectItem>{bands.map((band) => <SelectItem key={band.id} value={band.id}>{band.name}</SelectItem>)}
                    </SelectContent></Select>
                    <div className="relative"><Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" /><Input className="pl-8" value={searchTerm} onChange={(event) => setSearchTerm(event.target.value)} placeholder={t("playlists.search")} /></div>
                </div>
                {isLoading ? <div className="py-8 text-center">{t("common.loading")}</div> : playlists.length === 0 ? <div className="py-12 text-center">
                    <ListMusic className="mx-auto mb-4 h-12 w-12 text-muted-foreground" /><h3 className="mb-2 text-lg font-semibold">{t("playlists.empty")}</h3><p className="text-muted-foreground">{t("playlists.emptyHint")}</p>
                </div> : <Table><TableHeader><TableRow>
                    <TableHead>{t("common.name")}</TableHead><TableHead>{t("common.band")}</TableHead><TableHead>{t("playlists.event")}</TableHead><TableHead>{t("playlists.venue")}</TableHead><TableHead>{t("playlists.songsCount")}</TableHead><TableHead className="text-right">{t("common.actions")}</TableHead>
                </TableRow></TableHeader><TableBody>{playlists.map((playlist) => <TableRow key={playlist.id} className="cursor-pointer" onClick={() => navigate(`/app/playlists/${playlist.id}`)}>
                    <TableCell className="font-medium">{playlist.title}</TableCell><TableCell>{bandNames.get(playlist.bandId) ?? "—"}</TableCell>
                    <TableCell>{playlist.eventDateTime ? <span className="flex items-center gap-1"><Calendar className="h-3 w-3" />{new Date(playlist.eventDateTime).toLocaleString(i18n.resolvedLanguage)}</span> : "—"}</TableCell>
                    <TableCell>{playlist.venue || "—"}</TableCell><TableCell>{playlist.items.length}</TableCell><TableCell><div className="flex justify-end gap-2">
                        {access.some((item) => item.bandId === playlist.bandId && item.canEditContent) && <><Button variant="outline" size="icon" onClick={(event) => { event.stopPropagation(); navigate(`/app/playlists/${playlist.id}/edit`); }}><Edit className="h-4 w-4" /></Button><Button variant="outline" size="icon" onClick={(event) => { event.stopPropagation(); setPlaylistToDelete(playlist); }}><Trash2 className="h-4 w-4" /></Button></>}
                    </div></TableCell>
                </TableRow>)}</TableBody></Table>}
            </CardContent>
        </Card>
        <Dialog open={playlistToDelete !== null} onOpenChange={(open) => !open && setPlaylistToDelete(null)}><DialogContent><DialogHeader><DialogTitle>{t("playlists.deleteTitle")}</DialogTitle><DialogDescription>{t("playlists.deleteConfirm", { title: playlistToDelete?.title })}</DialogDescription></DialogHeader><DialogFooter><Button variant="outline" onClick={() => setPlaylistToDelete(null)}>{t("common.cancel")}</Button><Button variant="destructive" disabled={remove.isPending} onClick={() => playlistToDelete && remove.mutate(playlistToDelete.id)}>{t("common.delete")}</Button></DialogFooter></DialogContent></Dialog>
    </div>;
}
