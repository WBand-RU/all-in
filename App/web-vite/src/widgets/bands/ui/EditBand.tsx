import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";

import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import {
    Form,
    FormField,
    FormItem,
    FormLabel,
    FormControl,
    FormMessage,
} from "@/shared/ui/form";
import { useEffect } from "react";
import {
    Dialog,
    DialogClose,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/shared/ui/dialog";
import { useGetBand, useUpdateBand } from "@/lib/generated-api/band-api/bands";
import { ApiCodes } from "@/lib/generated-api/band-api/models";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";

const validationSchema = z.object({
    name: z.string().min(3).max(100),
});

type Schema = z.infer<typeof validationSchema>;

const defaultValues: Schema = {
    name: "",
};

type Props = {
    open: boolean;
    bandId: string;
    onClose: (options: { needForRefetch: boolean }) => Promise<void>;
};

export function EditBand({ open, bandId, onClose }: Props) {
    const { t } = useTranslation();
    const { data: loadedData, refetch } = useGetBand(bandId);
    const form = useForm({
        defaultValues,
        resolver: zodResolver(validationSchema),
        mode: "onChange",
    });
    const updateBandMutator = useUpdateBand();

    useEffect(() => {
        if (!loadedData || loadedData.code !== ApiCodes.Success) {
            return;
        }

        form.reset({
            name: loadedData.value!.name,
        });
    }, [loadedData, open]);

    useEffect(() => {
        if (open) {
            refetch();
        }
    }, [open]);

    async function handleSubmit(values: Schema) {
        try {
            await updateBandMutator.mutateAsync(
                {
                    bandId: bandId,
                    data: {
                        name: values.name,
                    },
                },
                {
                    onSuccess: (response) => {
                        if (response.code === ApiCodes.Success) {
                            toast.success(t("band.updated"));
                            form.reset(defaultValues);
                            onClose({ needForRefetch: true });
                        } else if (response.code === ApiCodes.Forbidden) {
                            toast.warning(
                                t("band.updateForbidden"),
                            );
                        } else if (response.code === ApiCodes.Conflict) {
                            toast.warning(t("band.exists"));
                        } else {
                            toast.error(t("errors.generic"));
                        }
                    },
                },
            );
        } catch (e) {
            console.error(e);
        }
    }

    return (
        <Dialog
            open={open}
            onOpenChange={() => onClose({ needForRefetch: false })}
        >
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>{t("band.editBand")}</DialogTitle>
                    <DialogDescription>
                        {t("band.addHint")}
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form
                        id="edit-moderator"
                        onSubmit={form.handleSubmit(handleSubmit)}
                        className="max-w-sm space-y-4"
                    >
                        <FormField
                            control={form.control}
                            name="name"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>{t("common.name")}</FormLabel>
                                    <FormControl>
                                        <Input
                                            type="text"
                                            placeholder={t("band.bandName")}
                                            {...field}
                                        />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        <DialogFooter>
                            <DialogClose asChild>
                                <Button variant="outline">{t("common.cancel")}</Button>
                            </DialogClose>

                            <Button
                                type="submit"
                                form="edit-moderator"
                                disabled={
                                    !form.formState.isValid ||
                                    form.formState.isSubmitting
                                }
                            >
                                {form.formState.isSubmitting
                                    ? t("common.updating")
                                    : t("common.update")}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
