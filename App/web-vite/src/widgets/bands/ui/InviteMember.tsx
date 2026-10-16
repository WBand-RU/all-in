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
import { toast } from "sonner";
import { PlusIcon } from "lucide-react";
import { useCreateBandInvitation } from "@/lib/generated-api/band-api/invitations";
import { useTranslation } from "react-i18next";

const validationSchema = z.object({
    email: z.string().email("Please enter a valid email address"),
    role: z.enum(["Member", "Admin"]).default("Member"),
});

type Schema = z.infer<typeof validationSchema>;

const defaultValues: Schema = {
    email: "",
    role: "Member",
};

type Props = {
    bandId: string;
    onSuccess?: () => void;
};

type InvitationError = { code?: string };

export function InviteMember({ bandId, onSuccess }: Props) {
    const { t } = useTranslation();
    const form = useForm({
        defaultValues,
        resolver: zodResolver(validationSchema),
        mode: "onChange",
    });
    const [open, setOpen] = useState(false);
    const inviteMember = useCreateBandInvitation();

    async function handleSubmit(values: Schema) {
        try {
            await inviteMember.mutateAsync({
                bandId,
                data: { inviteeEmail: values.email, role: values.role },
            });
            toast.success(t("band.invitationSent"));
            form.reset(defaultValues);
            setOpen(false);
            onSuccess?.();
        } catch (error) {
            const apiError = error as InvitationError;
            const description = apiError.code
                ? t(`errors.${apiError.code}`, { defaultValue: t("errors.generic") })
                : t("errors.generic");
            toast.error(
                apiError.code === "invitation_already_pending"
                    ? t("band.invitationPendingTitle")
                    : t("band.invitationFailed"),
                { description },
            );
        }
    }

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button variant="outline" size="sm">
                    <PlusIcon className="h-4 w-4 mr-2" />
                    {t("band.invite")}
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>{t("band.inviteTitle")}</DialogTitle>
                    <DialogDescription>
                        {t("band.inviteHint")}
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form
                        id="invite-member"
                        onSubmit={form.handleSubmit(handleSubmit)}
                        className="max-w-sm space-y-4"
                    >
                        <FormField
                            control={form.control}
                            name="email"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>{t("band.email")}</FormLabel>
                                    <FormControl>
                                        <Input
                                            type="email"
                                            placeholder="user@example.com"
                                            {...field}
                                        />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="role"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>{t("band.role")}</FormLabel>
                                    <FormControl>
                                        <select
                                            {...field}
                                            className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                                        >
                                            <option value="Member">
                                                {t("band.roles.Member")}
                                            </option>
                                            <option value="Admin">{t("band.roles.Admin")}</option>
                                        </select>
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
                                form="invite-member"
                                disabled={
                                    !form.formState.isValid ||
                                    form.formState.isSubmitting
                                }
                            >
                                {form.formState.isSubmitting
                                    ? t("band.sending")
                                    : t("band.send")}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
