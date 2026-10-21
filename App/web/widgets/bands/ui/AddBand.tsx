"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/components/ui/dialog";
import { useCreateBand } from "@/lib/generated/web-api/client";
import { BandNameForm } from "./BandNameForm";

export function AddBand({ onChange }: { onChange: () => Promise<void> }) {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const [open, setOpen] = useState(false);
    const create = useCreateBand();
    async function submit(values: { name: string }) {
        try {
            const response = await create.mutateAsync({ data: values });
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
            await Promise.all([
                onChange(),
                queryClient.invalidateQueries({ queryKey: ["my-band-access"] }),
            ]);
            setOpen(false);
            toast.success(t("band.created"));
        } catch {
            toast.error(t("errors.generic"));
        }
    }
    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger render={<Button variant="outline" />}>
                {t("band.addBand")}
            </DialogTrigger>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{t("band.addBand")}</DialogTitle>
                    <DialogDescription>{t("band.addHint")}</DialogDescription>
                </DialogHeader>
                {open && (
                    <BandNameForm
                        onSubmit={submit}
                        onCancel={() => setOpen(false)}
                    />
                )}
            </DialogContent>
        </Dialog>
    );
}
