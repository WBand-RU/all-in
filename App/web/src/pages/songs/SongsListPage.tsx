import { useState, useEffect } from "react";
import { useNavigator } from "@/services/navigator";
import { Plus, Search, Edit, Trash2, Music } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/shared/ui/table";
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
import { useQueryClient } from "@tanstack/react-query";
import { useToast } from "@/shared/ui/use-toast";
import {
    useGetListOfSongs,
    useDeleteSong,
} from "@/lib/generated-api/song-api/songs";
import { useGetListOfBands } from "@/lib/generated-api/band-api/bands";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/shared/ui/select";
import { useBandAccess } from "@/hooks/use-band-access";
import { useTranslation } from "react-i18next";

interface Song {
    id: string;
    title: string;
    author?: string | null;
    key?: string | null;
    bpm?: number | string | null;
    createdAt: string;
}

export function SongsListPage() {
    const navigator = useNavigator();
    const { toast } = useToast();
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const [searchTerm, setSearchTerm] = useState("");
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [songToDelete, setSongToDelete] = useState<Song | null>(null);
    const [currentBandId, setCurrentBandId] = useState("all");
    const { data: bandsData } = useGetListOfBands();
    const bands = bandsData?.value ?? [];
    const { access, canEditBand } = useBandAccess();
    const hasEditableBands = access.some(item => item.canEditContent);
    const {
        data: songs,
        isLoading,
        error,
    } = useGetListOfSongs({
        bandId: currentBandId === "all" ? undefined : currentBandId,
        searchTerm: searchTerm || undefined,
        page: 1,
        pageSize: 50,
    });

    useEffect(() => {
        if (error) {
            toast({
                title: t("common.error"),
                description: t("songs.loadFailed"),
                variant: "destructive",
            });
        }
    }, [error, toast]);

    const deleteSong = useDeleteSong({
        mutation: {
            onSuccess: () => {
                toast({
                    title: t("common.success"),
                    description: t("songs.deleted"),
                });
                setDeleteDialogOpen(false);
                setSongToDelete(null);
                // Invalidate and refetch songs query
                queryClient.invalidateQueries({
                    queryKey: ["/song-api/songs/list"],
                });
            },
            onError: () => {
                toast({
                    title: t("common.error"),
                    description: t("songs.deleteFailed"),
                    variant: "destructive",
                });
            },
        },
    });

    const handleDelete = async () => {
        if (!songToDelete) return;

        deleteSong.mutate({
            songId: songToDelete.id,
        });
    };

    return (
        <div className="container mx-auto py-8">
            <Card>
                <CardHeader>
                    <div className="flex justify-between items-center">
                        <div>
                            <CardTitle>{t("songs.title")}</CardTitle>
                            <CardDescription>
                                {t("songs.description")}
                            </CardDescription>
                        </div>
                        {hasEditableBands && <Button onClick={() => navigator.go("/songs/new")}>
                            <Plus className="mr-2 h-4 w-4" />
                            {t("songs.add")}
                        </Button>}
                    </div>
                </CardHeader>
                <CardContent>
                    <div className="mb-6 grid gap-4 md:grid-cols-2">
                        <Select value={currentBandId} onValueChange={setCurrentBandId}>
                            <SelectTrigger><SelectValue placeholder={t("common.selectBand")} /></SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">{t("common.allBands")}</SelectItem>
                                {bands.map((band) => <SelectItem key={band.id} value={band.id}>{band.name}</SelectItem>)}
                            </SelectContent>
                        </Select>
                        <div className="relative">
                            <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                            <Input
                                placeholder={t("songs.search")}
                                className="pl-8"
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                            />
                        </div>
                    </div>

                    {isLoading ? (
                        <div className="text-center py-8">{t("common.loading")}</div>
                    ) : songs?.value?.data?.length === 0 ? (
                        <div className="text-center py-12">
                            <Music className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
                            <h3 className="text-lg font-semibold mb-2">
                                {t("songs.empty")}
                            </h3>
                            <p className="text-muted-foreground mb-4">
                                {t("songs.emptyHint")}
                            </p>
                            {hasEditableBands && <Button onClick={() => navigator.go("/songs/new")}>
                                <Plus className="mr-2 h-4 w-4" />
                                {t("songs.addFirst")}
                            </Button>}
                        </div>
                    ) : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>{t("songs.fields.title")}</TableHead>
                                    <TableHead>{t("common.author")}</TableHead>
                                    <TableHead>{t("songs.groupColumn")}</TableHead>
                                    <TableHead>{t("songs.fields.key")}</TableHead>
                                    <TableHead>BPM</TableHead>
                                    <TableHead>{t("common.added")}</TableHead>
                                    <TableHead className="text-right">
                                        {t("common.actions")}
                                    </TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {songs?.value?.data?.map((song) => (
                                    <TableRow
                                        key={song.id}
                                        className="cursor-pointer hover:bg-muted/50"
                                        onClick={() =>
                                            navigator.go(`/songs/${song.id}`)
                                        }
                                    >
                                        <TableCell className="font-medium">
                                            {song.title}
                                        </TableCell>
                                        <TableCell>
                                            {song.author || "-"}
                                        </TableCell>
                                        <TableCell>
                                            {bands.find((band) => band.id === song.bandId)?.name ?? "-"}
                                        </TableCell>
                                        <TableCell>{song.key || "-"}</TableCell>
                                        <TableCell>{song.bpm || "-"}</TableCell>
                                        <TableCell>
                                            {new Date(
                                                song.createdAt,
                                            ).toLocaleDateString()}
                                        </TableCell>
                                        <TableCell className="text-right">
                                            <div className="flex justify-end gap-2">
                                                {canEditBand(song.bandId) && <Button
                                                    variant="outline"
                                                    size="sm"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        navigator.go(
                                                            `/songs/${song.id}/edit`,
                                                        );
                                                    }}
                                                >
                                                    <Edit className="h-4 w-4" />
                                                </Button>}
                                                {canEditBand(song.bandId) && <Button
                                                    variant="outline"
                                                    size="sm"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        setSongToDelete(song);
                                                        setDeleteDialogOpen(
                                                            true,
                                                        );
                                                    }}
                                                >
                                                    <Trash2 className="h-4 w-4" />
                                                </Button>}
                                            </div>
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>

            <Dialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>{t("songs.deleteTitle")}</DialogTitle>
                        <DialogDescription>
                            {t("songs.deleteConfirm", { title: songToDelete?.title })}
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                        <Button
                            variant="outline"
                            onClick={() => setDeleteDialogOpen(false)}
                        >
                            {t("common.cancel")}
                        </Button>
                        <Button variant="destructive" onClick={handleDelete}>
                            {t("common.delete")}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}
