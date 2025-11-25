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
    const queryClient = useQueryClient();
    const [searchTerm, setSearchTerm] = useState("");
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [songToDelete, setSongToDelete] = useState<Song | null>(null);
    const [currentBandId] = useState("default"); // TODO: Get from context
    const {
        data: songs,
        isLoading,
        error,
    } = useGetListOfSongs({
        bandId: currentBandId,
        searchTerm: searchTerm || undefined,
        page: 1,
        pageSize: 50,
    });

    useEffect(() => {
        if (error) {
            toast({
                title: "Error",
                description: "Failed to load songs",
                variant: "destructive",
            });
        }
    }, [error, toast]);

    const deleteSong = useDeleteSong({
        mutation: {
            onSuccess: () => {
                toast({
                    title: "Success",
                    description: "Song deleted successfully",
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
                    title: "Error",
                    description: "Failed to delete song",
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
                            <CardTitle>Songs</CardTitle>
                            <CardDescription>
                                Manage your worship songs collection
                            </CardDescription>
                        </div>
                        <Button onClick={() => navigator.go("/songs/new")}>
                            <Plus className="mr-2 h-4 w-4" />
                            Add Song
                        </Button>
                    </div>
                </CardHeader>
                <CardContent>
                    <div className="mb-6">
                        <div className="relative">
                            <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                            <Input
                                placeholder="Search songs..."
                                className="pl-8"
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                            />
                        </div>
                    </div>

                    {isLoading ? (
                        <div className="text-center py-8">Loading...</div>
                    ) : songs?.value?.data?.length === 0 ? (
                        <div className="text-center py-12">
                            <Music className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
                            <h3 className="text-lg font-semibold mb-2">
                                No songs yet
                            </h3>
                            <p className="text-muted-foreground mb-4">
                                Start building your worship library
                            </p>
                            <Button onClick={() => navigator.go("/songs/new")}>
                                <Plus className="mr-2 h-4 w-4" />
                                Add Your First Song
                            </Button>
                        </div>
                    ) : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Title</TableHead>
                                    <TableHead>Author</TableHead>
                                    <TableHead>Key</TableHead>
                                    <TableHead>BPM</TableHead>
                                    <TableHead>Added</TableHead>
                                    <TableHead className="text-right">
                                        Actions
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
                                        <TableCell>{song.key || "-"}</TableCell>
                                        <TableCell>{song.bpm || "-"}</TableCell>
                                        <TableCell>
                                            {new Date(
                                                song.createdAt,
                                            ).toLocaleDateString()}
                                        </TableCell>
                                        <TableCell className="text-right">
                                            <div className="flex justify-end gap-2">
                                                <Button
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
                                                </Button>
                                                <Button
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
                                                </Button>
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
                        <DialogTitle>Delete Song</DialogTitle>
                        <DialogDescription>
                            Are you sure you want to delete "
                            {songToDelete?.title}"? This action cannot be
                            undone.
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                        <Button
                            variant="outline"
                            onClick={() => setDeleteDialogOpen(false)}
                        >
                            Cancel
                        </Button>
                        <Button variant="destructive" onClick={handleDelete}>
                            Delete
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}
