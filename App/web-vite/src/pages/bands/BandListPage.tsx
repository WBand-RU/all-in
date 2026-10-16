import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { MoreVerticalIcon, Undo2Icon } from "lucide-react";

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
import { AddBand } from "../../widgets/bands/ui/AddBand";
import { EditBand } from "../../widgets/bands/ui/EditBand";
import { useNavigator } from "@/services/navigator";
import { useState } from "react";
import {
    useDeleteBand,
    useGetListOfBands,
    getGetListOfBandsQueryKey,
} from "@/lib/generated-api/band-api/bands";
import { toast } from "sonner";
import { apiClient } from "@/lib/axios-instance";
import { useBandAccess } from "@/hooks/use-band-access";
import { useTranslation } from "react-i18next";

interface FormerBand {
    bandId: string;
    bandName: string;
    leftAt: string;
}

export function BandListPage() {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const { data } = useGetListOfBands();
    const [isEditorOpen, setIsEditorOpen] = useState(false);
    const [editableBandId, setEditableBandId] = useState("");
    const deleteBandMutator = useDeleteBand();
    const { go } = useNavigator();
    const { access } = useBandAccess();
    const { data: formerBands = [] } = useQuery({
        queryKey: ["former-bands"],
        queryFn: async () => (await apiClient.get<{ value: FormerBand[] }>(
            "/band-api/user/former-bands",
        )).data.value ?? [],
    });
    const rejoinBand = useMutation({
        mutationFn: (bandId: string) =>
            apiClient.post(`/band-api/bands/${bandId}/membership/rejoin`),
        onSuccess: async () => {
            await Promise.all([
                queryClient.invalidateQueries({ queryKey: ["former-bands"] }),
                queryClient.invalidateQueries({ queryKey: getGetListOfBandsQueryKey() }),
                queryClient.invalidateQueries({ queryKey: ["my-band-access"] }),
            ]);
            toast.success(t("band.rejoined"));
        },
        onError: () => toast.error(t("band.rejoinFailed")),
    });

    async function handleDelete(bandId: string) {
        try {
            await deleteBandMutator.mutateAsync(
                {
                    bandId,
                },
                {
                    onError: (error) => {
                        console.error(error);
                        toast.error(t("errors.generic"));
                    },
                },
            );
            queryClient.invalidateQueries({
                queryKey: getGetListOfBandsQueryKey(),
            });
        } catch (error) {
            console.error(error);
            toast.error(t("errors.generic"));
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
                        <TableHead>{t("common.name")}</TableHead>
                        <TableHead>{t("common.actions")}</TableHead>
                    </TableRow>
                </TableHeader>
                <TableBody className="**:data-[slot=table-cell]:first:w-8">
                    {data?.value?.length ? (
                        data.value.map((band) => (
                            <TableRow
                                key={band.id}
                                className="cursor-pointer"
                                onClick={() => go(band.id)}
                            >
                                <TableCell>{band.id}</TableCell>
                                <TableCell>{band.name}</TableCell>
                                <TableCell>
                                    {access.some(item => item.bandId === band.id && item.role === "Owner") && <DropdownMenu>
                                        <DropdownMenuTrigger asChild>
                                            <Button
                                                variant="ghost"
                                                className="text-muted-foreground flex size-8 data-[state=open]:bg-muted"
                                                size="icon"
                                            >
                                                <MoreVerticalIcon />
                                                <span className="sr-only">
                                                    {t("common.actions")}
                                                </span>
                                            </Button>
                                        </DropdownMenuTrigger>
                                        <DropdownMenuContent align="end">
                                            <DropdownMenuItem
                                                onSelect={(e) => {
                                                    e.stopPropagation();
                                                    setIsEditorOpen(true);
                                                    setEditableBandId(band.id);
                                                }}
                                            >
                                                {t("common.edit")}
                                            </DropdownMenuItem>
                                            <DropdownMenuSeparator />
                                            <DropdownMenuItem
                                                onSelect={(e) => {
                                                    e.stopPropagation();
                                                    handleDelete(band.id);
                                                }}
                                            >
                                                {t("common.delete")}
                                            </DropdownMenuItem>
                                        </DropdownMenuContent>
                                    </DropdownMenu>}

                                    <EditBand
                                        key={"editor-" + band.id}
                                        open={
                                            isEditorOpen &&
                                            editableBandId === band.id
                                        }
                                        bandId={band.id}
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
                                colSpan={3}
                                className="h-24 text-center"
                            >
                                {t("common.noResults")}
                            </TableCell>
                        </TableRow>
                    )}
                </TableBody>
            </Table>

            {formerBands.length > 0 && (
                <div className="mt-6 space-y-3">
                    <div>
                        <h2 className="text-xl font-semibold">{t("band.formerTitle")}</h2>
                        <p className="text-sm text-muted-foreground">
                            {t("band.formerHint")}
                        </p>
                    </div>
                    {formerBands.map(band => (
                        <div key={band.bandId} className="flex items-center justify-between rounded-lg border p-4">
                            <div>
                                <div className="font-medium">{band.bandName}</div>
                                <div className="text-sm text-muted-foreground">
                                    {t("band.leftAt", { date: new Date(band.leftAt).toLocaleDateString() })}
                                </div>
                            </div>
                            <Button
                                variant="outline"
                                disabled={rejoinBand.isPending}
                                onClick={() => rejoinBand.mutate(band.bandId)}
                            >
                                <Undo2Icon className="mr-2 h-4 w-4" />
                                {t("band.rejoin")}
                            </Button>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
