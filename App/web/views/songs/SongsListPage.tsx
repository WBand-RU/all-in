"use client";

import { hasId } from "@/lib/api-data";
import { useState, useEffect } from "react";
import { useNavigator } from "@/services/navigator";
import { Plus, Search, Edit, Trash2, Music } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";
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
import { useQueryClient } from "@tanstack/react-query";
import { useToast } from "@/hooks/use-toast";
import {
    useGetListOfSongs,
    getGetListOfSongsQueryKey,
    useDeleteSong,
} from "@/lib/generated/web-api/client";
import { useGetListOfBands } from "@/lib/generated/web-api/client";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
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
    const hasEditableBands = access.some((item) => item.canEditContent);
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
    }, [error, toast, t]);

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
                    queryKey: getGetListOfSongsQueryKey(),
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
                    <div className="flex items-center justify-between">
                        <div>
                            <CardTitle>{t("songs.title")}</CardTitle>
                            <CardDescription>
                                {t("songs.description")}
                            </CardDescription>
                        </div>
                        {hasEditableBands && (
                            <Button onClick={() => navigator.go("/songs/new")}>
                                <Plus className="mr-2 h-4 w-4" />
                                {t("songs.add")}
                            </Button>
                        )}
                    </div>
                </CardHeader>
                <CardContent>
                    <div className="mb-6 grid gap-4 md:grid-cols-2">
                        <Select
                            items={[
                                { value: "all", label: t("common.allBands") },
                                ...bands.filter(hasId).map((band) => ({
                                    value: band.id,
                                    label: band.name,
                                })),
                            ]}
                            value={currentBandId}
                            onValueChange={(value) =>
                                setCurrentBandId(value ?? "all")
                            }
                        >
                            <SelectTrigger aria-label={t("common.band")}>
                                <SelectValue
                                    placeholder={t("common.selectBand")}
                                />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">
                                    {t("common.allBands")}
                                </SelectItem>
                                {bands.map((band) => (
                                    <SelectItem key={band.id} value={band.id}>
                                        {band.name}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                        <div className="relative">
                            <Search className="absolute top-2.5 left-2 h-4 w-4 text-muted-foreground" />
                            <Input
                                placeholder={t("songs.search")}
                                aria-label={t("songs.search")}
                                className="pl-8"
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                            />
                        </div>
                    </div>

                    {isLoading ? (
                        <div className="py-8 text-center">
                            {t("common.loading")}
                        </div>
                    ) : songs?.value?.data?.length === 0 ? (
                        <div className="py-12 text-center">
                            <Music className="mx-auto mb-4 h-12 w-12 text-muted-foreground" />
                            <h3 className="mb-2 text-lg font-semibold">
                                {t("songs.empty")}
                            </h3>
                            <p className="mb-4 text-muted-foreground">
                                {t("songs.emptyHint")}
                            </p>
                            {hasEditableBands && (
                                <Button
                                    onClick={() => navigator.go("/songs/new")}
                                >
                                    <Plus className="mr-2 h-4 w-4" />
                                    {t("songs.addFirst")}
                                </Button>
                            )}
                        </div>
                    ) : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>
                                        {t("songs.fields.title")}
                                    </TableHead>
                                    <TableHead>{t("common.author")}</TableHead>
                                    <TableHead>
                                        {t("songs.groupColumn")}
                                    </TableHead>
                                    <TableHead>
                                        {t("songs.fields.key")}
                                    </TableHead>
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
                                            {bands.find(
                                                (band) =>
                                                    band.id === song.bandId
                                            )?.name ?? "-"}
                                        </TableCell>
                                        <TableCell>{song.key || "-"}</TableCell>
                                        <TableCell>{song.bpm || "-"}</TableCell>
                                        <TableCell>
                                            {new Date(
                                                song.createdAt
                                            ).toLocaleDateString()}
                                        </TableCell>
                                        <TableCell className="text-right">
                                            <div className="flex justify-end gap-2">
                                                {canEditBand(song.bandId) && (
                                                    <Button
                                                        variant="outline"
                                                        size="sm"
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            navigator.go(
                                                                `/songs/${song.id}/edit`
                                                            );
                                                        }}
                                                    >
                                                        <Edit className="h-4 w-4" />
                                                    </Button>
                                                )}
                                                {canEditBand(song.bandId) && (
                                                    <Button
                                                        variant="outline"
                                                        size="sm"
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            setSongToDelete(
                                                                song
                                                            );
                                                            setDeleteDialogOpen(
                                                                true
                                                            );
                                                        }}
                                                    >
                                                        <Trash2 className="h-4 w-4" />
                                                    </Button>
                                                )}
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
                            {t("songs.deleteConfirm", {
                                title: songToDelete?.title,
                            })}
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
