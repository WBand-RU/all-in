import {
    type ColumnDef,
    type Row,
    getCoreRowModel,
    getFacetedRowModel,
    getFacetedUniqueValues,
    getFilteredRowModel,
    getPaginationRowModel,
    getSortedRowModel,
    useReactTable,
} from "@tanstack/react-table";
import { useQueryClient } from "@tanstack/react-query";
import { MoreVerticalIcon } from "lucide-react";
import { z } from "zod";

import { Button } from "@/shared/ui/button";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/shared/ui/dropdown-menu";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/shared/ui/table";
import { AddBand } from "../widgets/bands/ui/AddBand";
import { EditBand } from "../widgets/bands/ui/EditBand";
import { useNavigator } from "@/services/navigator";
import { useState } from "react";
import {
    useDeleteBand,
    useGetListOfBands,
    getGetListOfBandsQueryKey,
} from "@/lib/generated-api/band-api/bands";
import { toast } from "sonner";
import { AxiosError } from "axios";

const schema = z.object({
    id: z.string(),
    name: z.string(),
});

type Model = z.infer<typeof schema>;

const columns: ColumnDef<Model>[] = [
    {
        id: "id",
        header: () => "ID",
    },
    {
        id: "name",
        header: () => "Name",
    },
    {
        id: "actions",
        cell: () => (
            <DropdownMenu>
                <DropdownMenuTrigger asChild>
                    <Button
                        variant="ghost"
                        className="text-muted-foreground flex size-8 data-[state=open]:bg-muted"
                        size="icon"
                    >
                        <MoreVerticalIcon />
                        <span className="sr-only">Open menu</span>
                    </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-32">
                    <DropdownMenuItem>Edit</DropdownMenuItem>
                    <DropdownMenuItem>Make a copy</DropdownMenuItem>
                    <DropdownMenuItem>Favorite</DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem>Delete</DropdownMenuItem>
                </DropdownMenuContent>
            </DropdownMenu>
        ),
    },
];

export function BandListPage() {
    const queryClient = useQueryClient();
    const { data } = useGetListOfBands();
    const [isEditorOpen, setIsEditorOpen] = useState(false);
    const [editableBandId, setEditableBandId] = useState("");
    const deleteBandMutator = useDeleteBand();
    const { go } = useNavigator();

    const table = useReactTable({
        data: data?.value || [],
        columns,
        getRowId: (row: Model) => row.id.toString(),
        enableRowSelection: true,
        getCoreRowModel: getCoreRowModel(),
        getFilteredRowModel: getFilteredRowModel(),
        getPaginationRowModel: getPaginationRowModel(),
        getSortedRowModel: getSortedRowModel(),
        getFacetedRowModel: getFacetedRowModel(),
        getFacetedUniqueValues: getFacetedUniqueValues(),
    });

    async function handleDelete(bandId: string) {
        try {
            await deleteBandMutator.mutateAsync(
                {
                    bandId,
                },
                {
                    onError: (e) => {
                        console.error(e);
                        if (e instanceof AxiosError) {
                            toast(e.response?.data);
                        } else {
                            toast("Something went wrong");
                        }
                    },
                },
            );
            queryClient.invalidateQueries({
                queryKey: getGetListOfBandsQueryKey(),
            });
        } catch (e) {
            console.error(e);
            if (e instanceof AxiosError) {
                toast(e.response?.data);
            } else {
                toast("Something went wrong");
            }
        }
    }

    return (
        <div className="flex flex-col gap-4">
            <AddBand
                onChange={async () => {
                    queryClient.invalidateQueries({
                        queryKey: getGetListOfBandsQueryKey(),
                    });
                }}
            />

            <Table>
                <TableHeader className="bg-muted sticky top-0 z-10">
                    <TableRow>
                        <TableHead>ID</TableHead>
                        <TableHead>Name</TableHead>
                        <TableHead>Actions</TableHead>
                    </TableRow>
                </TableHeader>
                <TableBody className="**:data-[slot=table-cell]:first:w-8">
                    {table.getRowModel().rows?.length ? (
                        table.getRowModel().rows.map((row: Row<Model>) => (
                            <TableRow
                                key={row.id}
                                className="cursor-pointer"
                                onClick={() => go(row.id)}
                            >
                                <TableCell>{row.id}</TableCell>
                                <TableCell>{row.original.name}</TableCell>
                                <TableCell>
                                    <DropdownMenu>
                                        <DropdownMenuTrigger asChild>
                                            <Button
                                                variant="ghost"
                                                className="text-muted-foreground flex size-8 data-[state=open]:bg-muted"
                                                size="icon"
                                            >
                                                <MoreVerticalIcon />
                                                <span className="sr-only">
                                                    Open menu
                                                </span>
                                            </Button>
                                        </DropdownMenuTrigger>
                                        <DropdownMenuContent align="end">
                                            <DropdownMenuItem
                                                onSelect={(e) => {
                                                    e.stopPropagation();
                                                    setIsEditorOpen(true);
                                                    setEditableBandId(row.id);
                                                }}
                                            >
                                                Edit
                                            </DropdownMenuItem>
                                            <DropdownMenuSeparator />
                                            <DropdownMenuItem
                                                onSelect={(e) => {
                                                    e.stopPropagation();
                                                    handleDelete(row.id);
                                                }}
                                            >
                                                Delete
                                            </DropdownMenuItem>
                                        </DropdownMenuContent>
                                    </DropdownMenu>

                                    <EditBand
                                        key={"editor-" + row.id}
                                        open={
                                            isEditorOpen &&
                                            editableBandId === row.id
                                        }
                                        bandId={row.id}
                                        onClose={async ({ needForRefetch }) => {
                                            if (needForRefetch) {
                                                queryClient.invalidateQueries({
                                                    queryKey:
                                                        getGetListOfBandsQueryKey(),
                                                });
                                            }

                                            setIsEditorOpen(false);
                                        }}
                                    />
                                </TableCell>
                            </TableRow>
                        ))
                    ) : (
                        <TableRow>
                            <TableCell
                                colSpan={columns.length}
                                className="h-24 text-center"
                            >
                                No results.
                            </TableCell>
                        </TableRow>
                    )}
                </TableBody>
            </Table>
        </div>
    );
}
