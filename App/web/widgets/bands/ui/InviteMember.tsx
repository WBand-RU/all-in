"use client";

import { useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/components/ui/dialog";
import {
    getGetBandInvitationListQueryKey,
    useCreateBandInvitation,
} from "@/lib/generated/web-api/client";
import { invitationSchema } from "@/lib/form-schemas";

type Values = { email: string; role: "Member" | "Admin" };
export function InviteMember({ bandId }: { bandId: string }) {
    const { t } = useTranslation();
    const [open, setOpen] = useState(false);
    const queryClient = useQueryClient();
    const form = useForm<Values>({
        defaultValues: { email: "", role: "Member" },
        resolver: zodResolver(invitationSchema),
    });
    const create = useCreateBandInvitation();
    const roles = [
        { value: "Member", label: t("band.roles.Member") },
        { value: "Admin", label: t("band.roles.Admin") },
    ];
    async function submit(values: Values) {
        try {
            await create.mutateAsync({
                bandId,
                data: { inviteeEmail: values.email, role: values.role },
            });
            await queryClient.invalidateQueries({
                queryKey: getGetBandInvitationListQueryKey(bandId),
            });
            setOpen(false);
            form.reset();
            toast.success(t("band.invitationSent"));
        } catch (error) {
            const code = (error as { code?: string })?.code;
            toast.error(t("band.invitationFailed"), {
                description: code
                    ? t(`errors.${code}`, { defaultValue: t("errors.generic") })
                    : t("errors.generic"),
            });
        }
    }
    const errors = form.formState.errors;
    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger render={<Button variant="outline" size="sm" />}>
                {t("band.invite")}
            </DialogTrigger>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{t("band.inviteTitle")}</DialogTitle>
                    <DialogDescription>
                        {t("band.inviteHint")}
                    </DialogDescription>
                </DialogHeader>
                <form
                    onSubmit={form.handleSubmit(submit)}
                    className="space-y-4"
                >
                    <Field data-invalid={!!errors.email}>
                        <FieldLabel htmlFor="invite-email">
                            {t("band.email")}
                        </FieldLabel>
                        <Controller
                            name="email"
                            control={form.control}
                            render={({ field }) => (
                                <Input
                                    id="invite-email"
                                    type="email"
                                    autoComplete="email"
                                    required
                                    aria-invalid={!!errors.email}
                                    aria-describedby={
                                        errors.email
                                            ? "invite-email-error"
                                            : undefined
                                    }
                                    {...field}
                                />
                            )}
                        />
                        {errors.email && (
                            <FieldError
                                id="invite-email-error"
                                errors={[errors.email]}
                            />
                        )}
                    </Field>
                    <Field>
                        <FieldLabel htmlFor="invite-role">
                            {t("band.role")}
                        </FieldLabel>
                        <Controller
                            control={form.control}
                            name="role"
                            render={({ field }) => (
                                <Select
                                    items={roles}
                                    name={field.name}
                                    value={field.value}
                                    onValueChange={(value) => {
                                        if (value) field.onChange(value);
                                    }}
                                >
                                    <SelectTrigger
                                        id="invite-role"
                                        onBlur={field.onBlur}
                                        ref={field.ref}
                                    >
                                        <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {roles.map((role) => (
                                            <SelectItem
                                                key={role.value}
                                                value={role.value}
                                            >
                                                {role.label}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            )}
                        />
                    </Field>
                    <div className="flex justify-end gap-2">
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => setOpen(false)}
                        >
                            {t("common.cancel")}
                        </Button>
                        <Button
                            type="submit"
                            disabled={form.formState.isSubmitting}
                        >
                            {t(
                                form.formState.isSubmitting
                                    ? "band.sending"
                                    : "band.send"
                            )}
                        </Button>
                    </div>
                </form>
            </DialogContent>
        </Dialog>
    );
}
