import { useForm } from "react-hook-form";
import z from "zod";
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
import { useState } from "react";
import {
    Dialog,
    DialogClose,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/shared/ui/dialog";
import { useCreateBand } from "@/lib/generated-api/band-api/bands";
import { toast } from "sonner";
import { ApiCodes } from "@/lib/generated-api/band-api/models";
import { useTranslation } from "react-i18next";

const validationSchema = z.object({
    name: z.string().min(3).max(100),
});

type Schema = z.infer<typeof validationSchema>;

const defaultValues: Schema = {
    name: "",
};

type Props = {
    onChange: () => Promise<void>;
};

export function AddBand({ onChange }: Props) {
    const { t } = useTranslation();
    const form = useForm({
        defaultValues,
        resolver: zodResolver(validationSchema),
        mode: "onChange",
    });
    const createBandMutator = useCreateBand();
    const [open, setOpen] = useState(false);

    async function handleSubmit(values: Schema) {
        try {
            await createBandMutator.mutateAsync(
                {
                    data: {
                        name: values.name,
                    },
                },
                {
                    onSuccess: async (response) => {
                        if (response.code === ApiCodes.Success) {
                            toast.success(t("band.created"));
                            form.reset(defaultValues);
                            setOpen(false);
                            await onChange();
                        } else if (response.code === ApiCodes.Forbidden) {
                            toast.warning(
                                t("band.createForbidden"),
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
            toast(t("errors.generic"));
        }
    }

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button variant="outline" type="button">
                    {t("band.addBand")}
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>{t("band.addBand")}</DialogTitle>
                    <DialogDescription>
                        {t("band.addHint")}
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form
                        id="add-moderator"
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
                                            placeholder="Band name"
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
                                form="add-moderator"
                                disabled={
                                    !form.formState.isValid ||
                                    form.formState.isSubmitting
                                }
                            >
                                {form.formState.isSubmitting
                                    ? t("common.adding")
                                    : t("common.add")}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
