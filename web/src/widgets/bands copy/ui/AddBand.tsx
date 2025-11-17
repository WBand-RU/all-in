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
import { useCreateBand } from "@/lib/generated-api/band-api/bands/bands";
import { toast } from "sonner";
import { AxiosError } from "axios";

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
    const form = useForm({
        defaultValues,
        resolver: zodResolver(validationSchema),
        mode: "onChange",
    });
    const createBandMutator = useCreateBand();
    const [open, setOpen] = useState(false);

    async function handleSubmit(values: Schema) {
        try {
            await createBandMutator.mutateAsync({
                data: {
                    name: values.name,
                },
            });

            form.reset(defaultValues);
            setOpen(false);
            await onChange();
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
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button variant="outline" type="button">
                    Add new band
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>Add new band</DialogTitle>
                    <DialogDescription>
                        Bands are groups of musicians who perform music
                        together.
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
                                form="add-moderator"
                                disabled={
                                    !form.formState.isValid ||
                                    form.formState.isSubmitting
                                }
                            >
                                {form.formState.isSubmitting
                                    ? "Adding..."
                                    : "Add"}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
