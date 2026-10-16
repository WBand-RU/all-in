import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Calendar, Edit, ListMusic, MapPin, Music2 } from "lucide-react";
import { useNavigate, useParams } from "react-router";
import { useTranslation } from "react-i18next";
import { apiClient } from "@/lib/axios-instance";
import { useGetListOfBands } from "@/lib/generated-api/band-api/bands";
import { useBandAccess } from "@/hooks/use-band-access";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import type { PlaylistDto, PlaylistItemDto } from "./PlaylistsListPage";

function Transition({ item }: { item: PlaylistItemDto }) {
    const { t } = useTranslation();
    if (item.transition === "AutoStart") return <Badge variant="secondary">{t("playlists.autoStart")}</Badge>;
    if (item.transition === "Crossfade") return <Badge variant="secondary">{t("playlists.crossfadeValue", { seconds: item.crossfadeSeconds })}</Badge>;
    return <Badge variant="outline">{t("playlists.pauseValue", { seconds: item.pauseSeconds })}</Badge>;
}

export function PlaylistViewPage() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { t, i18n } = useTranslation();
    const { access } = useBandAccess();
    const { data: bandsData } = useGetListOfBands();
    const { data: playlist, isLoading, isError } = useQuery({
        queryKey: ["playlist", id], enabled: !!id,
        queryFn: async () => (await apiClient.get<{ value: PlaylistDto }>(`/playlist-api/playlists/${id}`)).data.value,
    });

    if (isLoading) return <div className="container mx-auto py-8 text-center">{t("common.loading")}</div>;
    if (isError || !playlist) return <div className="container mx-auto py-8"><Card><CardContent className="py-12 text-center text-muted-foreground">{t("playlists.notFound")}</CardContent></Card></div>;

    const bandName = bandsData?.value?.find((band) => band.id === playlist.bandId)?.name;
    const canEdit = access.some((item) => item.bandId === playlist.bandId && item.canEditContent);
    const orderedItems = [...playlist.items].sort((left, right) => left.order - right.order);

    return <div className="container mx-auto space-y-6 py-8">
        <div className="flex items-center gap-3"><Button variant="ghost" size="icon" onClick={() => navigate("/app/playlists")}><ArrowLeft className="h-4 w-4" /></Button><div className="min-w-0 flex-1"><h1 className="truncate text-2xl font-bold">{playlist.title}</h1><p className="text-muted-foreground">{bandName ?? t("common.band")}</p></div>{canEdit && <Button variant="outline" onClick={() => navigate(`/app/playlists/${playlist.id}/edit`)}><Edit className="mr-2 h-4 w-4" />{t("common.edit")}</Button>}</div>
        <div className="grid gap-4 md:grid-cols-3">
            <Card><CardContent className="flex items-center gap-3 pt-6"><Calendar className="h-5 w-5 text-muted-foreground" /><div><div className="text-sm text-muted-foreground">{t("playlists.dateTime")}</div><div className="font-medium">{playlist.eventDateTime ? new Date(playlist.eventDateTime).toLocaleString(i18n.resolvedLanguage) : "—"}</div></div></CardContent></Card>
            <Card><CardContent className="flex items-center gap-3 pt-6"><MapPin className="h-5 w-5 text-muted-foreground" /><div><div className="text-sm text-muted-foreground">{t("playlists.venue")}</div><div className="font-medium">{playlist.venue || "—"}</div></div></CardContent></Card>
            <Card><CardContent className="flex items-center gap-3 pt-6"><ListMusic className="h-5 w-5 text-muted-foreground" /><div><div className="text-sm text-muted-foreground">{t("playlists.contentVersion")}</div><div className="font-medium">{playlist.contentVersion}</div></div></CardContent></Card>
        </div>
        {playlist.description && <Card><CardHeader><CardTitle className="text-base">{t("playlists.notes")}</CardTitle></CardHeader><CardContent className="whitespace-pre-wrap">{playlist.description}</CardContent></Card>}
        <Card><CardHeader><CardTitle>{t("playlists.program")}</CardTitle><CardDescription>{t("playlists.programHint", { count: orderedItems.length })}</CardDescription></CardHeader><CardContent>
            {orderedItems.length === 0 ? <div className="py-8 text-center text-muted-foreground">{t("playlists.noItems")}</div> : <div className="space-y-3">{orderedItems.map((item) => <div key={item.id} className="rounded-lg border p-4"><div className="flex flex-wrap items-center gap-3"><div className="flex h-8 w-8 items-center justify-center rounded-full bg-muted font-semibold">{item.order}</div><Music2 className="h-4 w-4" /><div className="mr-auto font-semibold">{item.songTitle}</div><Transition item={item} /></div><div className="mt-3 flex flex-wrap gap-2 pl-11">{item.keyOverride && <Badge variant="outline">{t("playlists.keyValue", { value: item.keyOverride })}</Badge>}{item.bpmOverride && <Badge variant="outline">{item.bpmOverride} BPM</Badge>}{item.structureOverride.length > 0 && <Badge variant="outline">{t("playlists.structureSections", { count: item.structureOverride.length })}</Badge>}{item.notes && <span className="text-sm text-muted-foreground">{item.notes}</span>}</div></div>)}</div>}
        </CardContent></Card>
    </div>;
}
