import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";

import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Checkbox } from "@/shared/ui/checkbox";
import {
    Form,
    FormField,
    FormItem,
    FormLabel,
    FormControl,
    FormMessage,
} from "@/shared/ui/form";
import { useEffect, useState, type ComponentProps } from "react";
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
import {
    useGetBand,
    useUpdateBand,
} from "@/lib/generated-api/band-api/bands/bands";

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
    const { data: loadedData, refetch } = useGetBand(bandId);
    const form = useForm({
        defaultValues,
        resolver: zodResolver(validationSchema),
        mode: "onChange",
    });
    const updateBandMutator = useUpdateBand();

    useEffect(() => {
        if (!loadedData) {
            return;
        }

        form.reset({
            name: loadedData.name,
        });
    }, [loadedData, open]);

    useEffect(() => {
        if (open) {
            refetch();
        }
    }, [open]);

    async function handleSubmit(values: Schema) {
        try {
            await updateBandMutator.mutateAsync({
                bandId: bandId,
                data: {
                    name: values.name,
                },
            });

            form.reset(defaultValues);
            onClose({ needForRefetch: true });
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
                    <DialogTitle>Edit moderator</DialogTitle>
                    <DialogDescription>
                        Moderators can manage any content on the site. Be
                        careful!
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
                                    <FormLabel>Name</FormLabel>
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
                                <Button variant="outline">Cancel</Button>
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
                                    ? "Updating..."
                                    : "Update"}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
