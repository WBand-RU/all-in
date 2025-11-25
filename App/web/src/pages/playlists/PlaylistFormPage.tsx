import { useState } from "react";
import { useNavigate, useParams } from "react-router";
import {
    Save,
    ArrowLeft,
    Plus,
    X,
    GripVertical,
    Music,
    Clock,
    Pause,
} from "lucide-react";
import { DndContext, closestCenter, type DragEndEvent } from "@dnd-kit/core";
import {
    SortableContext,
    verticalListSortingStrategy,
    useSortable,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";
import { Textarea } from "@/shared/ui/textarea";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/shared/ui/card";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/shared/ui/select";
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

interface PlaylistItem {
    id: string;
    type: "song" | "block" | "pause";
    songId?: string;
    songTitle?: string;
    customKey?: string;
    blockTitle?: string;
    notes?: string;
    durationMinutes: number;
    order: number;
}

interface PlaylistForm {
    name: string;
    description: string;
    plannedDate: string;
    items: PlaylistItem[];
}

interface SortableItemProps {
    item: PlaylistItem;
    onRemove: (id: string) => void;
    onEdit: (item: PlaylistItem) => void;
}

function SortableItem({ item, onRemove, onEdit }: SortableItemProps) {
    const { attributes, listeners, setNodeRef, transform, transition } =
        useSortable({ id: item.id });

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
    };

    return (
        <div
            ref={setNodeRef}
            style={style}
            className="flex items-center gap-3 p-3 bg-background border rounded-lg"
        >
            <div
                {...attributes}
                {...listeners}
                className="cursor-grab hover:cursor-grabbing"
            >
                <GripVertical className="h-5 w-5 text-muted-foreground" />
            </div>

            <div className="flex-1">
                {item.type === "song" && (
                    <div className="flex items-center gap-2">
                        <Music className="h-4 w-4" />
                        <span className="font-medium">{item.songTitle}</span>
                        {item.customKey && (
                            <Badge variant="outline">{item.customKey}</Badge>
                        )}
                    </div>
                )}
                {item.type === "block" && (
                    <div className="flex items-center gap-2">
                        <Badge>Block</Badge>
                        <span className="font-medium">{item.blockTitle}</span>
                    </div>
                )}
                {item.type === "pause" && (
                    <div className="flex items-center gap-2">
                        <Pause className="h-4 w-4" />
                        <span className="font-medium">Pause</span>
                    </div>
                )}
                {item.notes && (
                    <p className="text-sm text-muted-foreground mt-1">
                        {item.notes}
                    </p>
                )}
            </div>

            <div className="flex items-center gap-2">
                <div className="flex items-center gap-1 text-sm text-muted-foreground">
                    <Clock className="h-3 w-3" />
                    {item.durationMinutes}m
                </div>
                <Button variant="ghost" size="sm" onClick={() => onEdit(item)}>
                    Edit
                </Button>
                <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => onRemove(item.id)}
                >
                    <X className="h-4 w-4" />
                </Button>
            </div>
        </div>
    );
}

export function PlaylistFormPage() {
    const navigate = useNavigate();
    const { id } = useParams();
    const { toast } = useToast();
    const isEditing = !!id;

    const [loading, setLoading] = useState(false);
    // const [currentBandId] = useState("default"); // TODO: Get from context when available
    const [addItemDialogOpen, setAddItemDialogOpen] = useState(false);
    // const [editItemDialogOpen, setEditItemDialogOpen] = useState(false); // TODO: Implement edit dialog
    // const [currentItem, setCurrentItem] = useState<PlaylistItem | null>(null); // TODO: Use for edit dialog
    const [itemType, setItemType] = useState<"song" | "block" | "pause">(
        "song",
    );

    const [form, setForm] = useState<PlaylistForm>({
        name: "",
        description: "",
        plannedDate: "",
        items: [],
    });

    const [newItem, setNewItem] = useState({
        songId: "",
        songTitle: "",
        customKey: "",
        blockTitle: "",
        notes: "",
        durationMinutes: 5,
    });

    const handleDragEnd = (event: DragEndEvent) => {
        const { active, over } = event;

        if (!over || active.id === over.id) return;

        const oldIndex = form.items.findIndex((item) => item.id === active.id);
        const newIndex = form.items.findIndex((item) => item.id === over.id);

        const newItems = [...form.items];
        const [movedItem] = newItems.splice(oldIndex, 1);
        newItems.splice(newIndex, 0, movedItem);

        // Update order
        newItems.forEach((item, index) => {
            item.order = index + 1;
        });

        setForm({ ...form, items: newItems });
    };

    const handleAddItem = () => {
        const newPlaylistItem: PlaylistItem = {
            id: `item-${Date.now()}`,
            type: itemType,
            songId: itemType === "song" ? newItem.songId : undefined,
            songTitle: itemType === "song" ? newItem.songTitle : undefined,
            customKey: itemType === "song" ? newItem.customKey : undefined,
            blockTitle: itemType === "block" ? newItem.blockTitle : undefined,
            notes: newItem.notes,
            durationMinutes: newItem.durationMinutes,
            order: form.items.length + 1,
        };

        setForm({
            ...form,
            items: [...form.items, newPlaylistItem],
        });

        // Reset form
        setNewItem({
            songId: "",
            songTitle: "",
            customKey: "",
            blockTitle: "",
            notes: "",
            durationMinutes: 5,
        });
        setAddItemDialogOpen(false);
    };

    const handleEditItem = (_item: PlaylistItem) => {
        // TODO: Implement edit dialog
        toast({
            title: "Edit feature coming soon",
            description:
                "Edit functionality will be available in the next update",
        });
    };

    const handleRemoveItem = (id: string) => {
        const newItems = form.items.filter((item) => item.id !== id);
        newItems.forEach((item, index) => {
            item.order = index + 1;
        });
        setForm({ ...form, items: newItems });
    };

    const getTotalDuration = () => {
        return form.items.reduce(
            (total, item) => total + item.durationMinutes,
            0,
        );
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!form.name.trim()) {
            toast({
                title: "Validation Error",
                description: "Playlist name is required",
                variant: "destructive",
            });
            return;
        }

        try {
            setLoading(true);

            // TODO: Implement API calls
            toast({
                title: "Success",
                description: `Playlist ${isEditing ? "updated" : "created"} successfully`,
            });
            navigate("/playlists");
        } catch (error) {
            toast({
                title: "Error",
                description: `Failed to ${isEditing ? "update" : "create"} playlist`,
                variant: "destructive",
            });
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="container mx-auto py-8">
            <Card>
                <CardHeader>
                    <div className="flex items-center gap-4">
                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => navigate("/playlists")}
                        >
                            <ArrowLeft className="h-4 w-4" />
                        </Button>
                        <div className="flex-1">
                            <CardTitle>
                                {isEditing ? "Edit Playlist" : "New Playlist"}
                            </CardTitle>
                            <CardDescription>
                                {isEditing
                                    ? "Update playlist details"
                                    : "Create a worship service setlist"}
                            </CardDescription>
                        </div>
                        <div className="text-sm text-muted-foreground">
                            Total Duration: {getTotalDuration()} minutes
                        </div>
                    </div>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-6">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div className="space-y-2">
                                <Label htmlFor="name">Playlist Name *</Label>
                                <Input
                                    id="name"
                                    value={form.name}
                                    onChange={(e) =>
                                        setForm({
                                            ...form,
                                            name: e.target.value,
                                        })
                                    }
                                    placeholder="Sunday Morning Service"
                                    required
                                />
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="plannedDate">
                                    Planned Date
                                </Label>
                                <Input
                                    id="plannedDate"
                                    type="date"
                                    value={form.plannedDate}
                                    onChange={(e) =>
                                        setForm({
                                            ...form,
                                            plannedDate: e.target.value,
                                        })
                                    }
                                />
                            </div>
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="description">Description</Label>
                            <Textarea
                                id="description"
                                value={form.description}
                                onChange={(e) =>
                                    setForm({
                                        ...form,
                                        description: e.target.value,
                                    })
                                }
                                placeholder="Enter playlist description..."
                                rows={3}
                            />
                        </div>

                        <div className="space-y-4">
                            <div className="flex justify-between items-center">
                                <Label>Playlist Items</Label>
                                <Button
                                    type="button"
                                    onClick={() => setAddItemDialogOpen(true)}
                                >
                                    <Plus className="mr-2 h-4 w-4" />
                                    Add Item
                                </Button>
                            </div>

                            {form.items.length === 0 ? (
                                <div className="text-center py-8 border-2 border-dashed rounded-lg">
                                    <Music className="mx-auto h-8 w-8 text-muted-foreground mb-2" />
                                    <p className="text-sm text-muted-foreground">
                                        No items in playlist yet. Add songs,
                                        blocks, or pauses.
                                    </p>
                                </div>
                            ) : (
                                <DndContext
                                    collisionDetection={closestCenter}
                                    onDragEnd={handleDragEnd}
                                >
                                    <SortableContext
                                        items={form.items.map(
                                            (item) => item.id,
                                        )}
                                        strategy={verticalListSortingStrategy}
                                    >
                                        <div className="space-y-2">
                                            {form.items.map((item) => (
                                                <SortableItem
                                                    key={item.id}
                                                    item={item}
                                                    onRemove={handleRemoveItem}
                                                    onEdit={handleEditItem}
                                                />
                                            ))}
                                        </div>
                                    </SortableContext>
                                </DndContext>
                            )}
                        </div>

                        <div className="flex justify-end gap-4">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => navigate("/playlists")}
                            >
                                Cancel
                            </Button>
                            <Button type="submit" disabled={loading}>
                                <Save className="mr-2 h-4 w-4" />
                                {isEditing ? "Update" : "Create"} Playlist
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>

            {/* Add Item Dialog */}
            <Dialog
                open={addItemDialogOpen}
                onOpenChange={setAddItemDialogOpen}
            >
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Add Playlist Item</DialogTitle>
                        <DialogDescription>
                            Add a song, block, or pause to your playlist
                        </DialogDescription>
                    </DialogHeader>

                    <div className="space-y-4">
                        <div className="space-y-2">
                            <Label>Item Type</Label>
                            <Select
                                value={itemType}
                                onValueChange={(
                                    value: "song" | "block" | "pause",
                                ) => setItemType(value)}
                            >
                                <SelectTrigger>
                                    <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="song">Song</SelectItem>
                                    <SelectItem value="block">Block</SelectItem>
                                    <SelectItem value="pause">Pause</SelectItem>
                                </SelectContent>
                            </Select>
                        </div>

                        {itemType === "song" && (
                            <>
                                <div className="space-y-2">
                                    <Label>Song</Label>
                                    <Input
                                        placeholder="Select or enter song title"
                                        value={newItem.songTitle}
                                        onChange={(e) =>
                                            setNewItem({
                                                ...newItem,
                                                songTitle: e.target.value,
                                            })
                                        }
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label>Custom Key (optional)</Label>
                                    <Input
                                        placeholder="e.g., G"
                                        value={newItem.customKey}
                                        onChange={(e) =>
                                            setNewItem({
                                                ...newItem,
                                                customKey: e.target.value,
                                            })
                                        }
                                    />
                                </div>
                            </>
                        )}

                        {itemType === "block" && (
                            <div className="space-y-2">
                                <Label>Block Title</Label>
                                <Input
                                    placeholder="e.g., Worship Block, Prayer"
                                    value={newItem.blockTitle}
                                    onChange={(e) =>
                                        setNewItem({
                                            ...newItem,
                                            blockTitle: e.target.value,
                                        })
                                    }
                                />
                            </div>
                        )}

                        <div className="space-y-2">
                            <Label>Duration (minutes)</Label>
                            <Input
                                type="number"
                                min="1"
                                value={newItem.durationMinutes}
                                onChange={(e) =>
                                    setNewItem({
                                        ...newItem,
                                        durationMinutes:
                                            parseInt(e.target.value) || 5,
                                    })
                                }
                            />
                        </div>

                        <div className="space-y-2">
                            <Label>Notes (optional)</Label>
                            <Textarea
                                placeholder="Additional notes..."
                                value={newItem.notes}
                                onChange={(e) =>
                                    setNewItem({
                                        ...newItem,
                                        notes: e.target.value,
                                    })
                                }
                                rows={2}
                            />
                        </div>
                    </div>

                    <DialogFooter>
                        <Button
                            variant="outline"
                            onClick={() => setAddItemDialogOpen(false)}
                        >
                            Cancel
                        </Button>
                        <Button onClick={handleAddItem}>Add Item</Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}
