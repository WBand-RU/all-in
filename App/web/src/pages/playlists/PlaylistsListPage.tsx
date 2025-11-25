import { useState, useEffect } from "react";
import { useNavigate } from "react-router";
import {
    Plus,
    Search,
    Edit,
    Trash2,
    ListMusic,
    Clock,
    Calendar,
} from "lucide-react";
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
import { Badge } from "@/shared/ui/badge";
import { useToast } from "@/shared/ui/use-toast";

interface Playlist {
    id: string;
    name: string;
    description?: string;
    itemsCount: number;
    durationMinutes: number;
    plannedDate?: string;
    createdAt: string;
}

export function PlaylistsListPage() {
    const navigate = useNavigate();
    const { toast } = useToast();
    const [playlists, setPlaylists] = useState<Playlist[]>([]);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState("");
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [playlistToDelete, setPlaylistToDelete] = useState<Playlist | null>(
        null,
    );
    // const [currentBandId] = useState("default"); // TODO: Get from context when available

    useEffect(() => {
        loadPlaylists();
    }, [searchTerm]);

    const loadPlaylists = async () => {
        try {
            setLoading(true);
            // TODO: Implement API call when available
            // Mock data for now
            const mockData: Playlist[] = [
                {
                    id: "1",
                    name: "Sunday Morning Service",
                    description: "Regular Sunday worship",
                    itemsCount: 8,
                    durationMinutes: 45,
                    plannedDate: new Date().toISOString(),
                    createdAt: new Date().toISOString(),
                },
                {
                    id: "2",
                    name: "Youth Night Worship",
                    description: "Contemporary songs for youth service",
                    itemsCount: 6,
                    durationMinutes: 30,
                    createdAt: new Date().toISOString(),
                },
            ];
            setPlaylists(mockData);
        } catch (error) {
            toast({
                title: "Error",
                description: "Failed to load playlists",
                variant: "destructive",
            });
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async () => {
        if (!playlistToDelete) return;

        try {
            // TODO: Implement API call
            toast({
                title: "Success",
                description: "Playlist deleted successfully",
            });
            loadPlaylists();
            setDeleteDialogOpen(false);
        } catch (error) {
            toast({
                title: "Error",
                description: "Failed to delete playlist",
                variant: "destructive",
            });
        }
    };

    const formatDuration = (minutes: number) => {
        const hours = Math.floor(minutes / 60);
        const mins = minutes % 60;
        return hours > 0 ? `${hours}h ${mins}m` : `${mins}m`;
    };

    return (
        <div className="container mx-auto py-8">
            <Card>
                <CardHeader>
                    <div className="flex justify-between items-center">
                        <div>
                            <CardTitle>Playlists</CardTitle>
                            <CardDescription>
                                Manage your worship service setlists
                            </CardDescription>
                        </div>
                        <Button onClick={() => navigate("/playlists/new")}>
                            <Plus className="mr-2 h-4 w-4" />
                            Create Playlist
                        </Button>
                    </div>
                </CardHeader>
                <CardContent>
                    <div className="mb-6">
                        <div className="relative">
                            <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                            <Input
                                placeholder="Search playlists..."
                                className="pl-8"
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                            />
                        </div>
                    </div>

                    {loading ? (
                        <div className="text-center py-8">Loading...</div>
                    ) : playlists.length === 0 ? (
                        <div className="text-center py-12">
                            <ListMusic className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
                            <h3 className="text-lg font-semibold mb-2">
                                No playlists yet
                            </h3>
                            <p className="text-muted-foreground mb-4">
                                Create your first worship setlist
                            </p>
                            <Button onClick={() => navigate("/playlists/new")}>
                                <Plus className="mr-2 h-4 w-4" />
                                Create Your First Playlist
                            </Button>
                        </div>
                    ) : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Name</TableHead>
                                    <TableHead>Description</TableHead>
                                    <TableHead>Songs</TableHead>
                                    <TableHead>Duration</TableHead>
                                    <TableHead>Planned Date</TableHead>
                                    <TableHead className="text-right">
                                        Actions
                                    </TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {playlists.map((playlist) => (
                                    <TableRow key={playlist.id}>
                                        <TableCell className="font-medium">
                                            {playlist.name}
                                        </TableCell>
                                        <TableCell>
                                            {playlist.description || "-"}
                                        </TableCell>
                                        <TableCell>
                                            <Badge variant="secondary">
                                                {playlist.itemsCount} items
                                            </Badge>
                                        </TableCell>
                                        <TableCell>
                                            <div className="flex items-center gap-1">
                                                <Clock className="h-3 w-3" />
                                                {formatDuration(
                                                    playlist.durationMinutes,
                                                )}
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            {playlist.plannedDate ? (
                                                <div className="flex items-center gap-1">
                                                    <Calendar className="h-3 w-3" />
                                                    {new Date(
                                                        playlist.plannedDate,
                                                    ).toLocaleDateString()}
                                                </div>
                                            ) : (
                                                "-"
                                            )}
                                        </TableCell>
                                        <TableCell className="text-right">
                                            <div className="flex justify-end gap-2">
                                                <Button
                                                    variant="outline"
                                                    size="sm"
                                                    onClick={() =>
                                                        navigate(
                                                            `/playlists/${playlist.id}/edit`,
                                                        )
                                                    }
                                                >
                                                    <Edit className="h-4 w-4" />
                                                </Button>
                                                <Button
                                                    variant="outline"
                                                    size="sm"
                                                    onClick={() => {
                                                        setPlaylistToDelete(
                                                            playlist,
                                                        );
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
                        <DialogTitle>Delete Playlist</DialogTitle>
                        <DialogDescription>
                            Are you sure you want to delete "
                            {playlistToDelete?.name}"? This action cannot be
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
