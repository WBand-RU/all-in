"use client";

import { useTranslation } from "react-i18next";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import {
    getGetBandQueryKey,
    useGetBand,
    useUpdateBand,
} from "@/lib/generated/web-api/client";
import { BandNameForm } from "./BandNameForm";

export function EditBand({
    open,
    bandId,
    onClose,
}: {
    open: boolean;
    bandId: string;
    onClose: (options: { needForRefetch: boolean }) => Promise<void>;
}) {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const query = useGetBand(bandId, { query: { enabled: open } });
    const update = useUpdateBand();
    async function submit(values: { name: string }) {
        try {
            const response = await update.mutateAsync({ bandId, data: values });
            if (response.code !== "Success") {
                toast.error(
                    t(
                        response.code === "Conflict"
                            ? "band.exists"
                            : "errors.generic"
                    )
                );
                return;
            }
            await queryClient.invalidateQueries({
                queryKey: getGetBandQueryKey(bandId),
            });
            await onClose({ needForRefetch: true });
            toast.success(t("band.updated"));
        } catch {
            toast.error(t("errors.generic"));
        }
    }
    return (
        <Dialog
            open={open}
            onOpenChange={(value) => {
                if (!value) void onClose({ needForRefetch: false });
            }}
        >
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{t("band.editBand")}</DialogTitle>
                    <DialogDescription>{t("band.addHint")}</DialogDescription>
                </DialogHeader>
                {query.isError ? (
                    <p role="alert">{t("errors.generic")}</p>
                ) : open && query.data?.value ? (
                    <BandNameForm
                        key={bandId}
                        name={query.data.value.name}
                        onSubmit={submit}
                        onCancel={() => {
                            void onClose({ needForRefetch: false });
                        }}
                    />
                ) : (
                    <p role="status">{t("common.loading")}</p>
                )}
            </DialogContent>
        </Dialog>
    );
}
