"use client";

import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import { bandNameSchema } from "@/lib/form-schemas";

export function BandNameForm({
    name = "",
    onSubmit,
    onCancel,
}: {
    name?: string;
    onSubmit: (values: { name: string }) => Promise<void>;
    onCancel: () => void;
}) {
    const { t } = useTranslation();
    const form = useForm({
        defaultValues: { name },
        resolver: zodResolver(bandNameSchema),
    });
    const error = form.formState.errors.name;
    return (
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <Field data-invalid={!!error}>
                <FieldLabel htmlFor="band-name">
                    {t("band.bandName")}
                </FieldLabel>
                <Controller
                    name="name"
                    control={form.control}
                    render={({ field }) => (
                        <Input
                            id="band-name"
                            autoComplete="off"
                            maxLength={120}
                            required
                            aria-invalid={!!error}
                            aria-describedby={
                                error ? "band-name-error" : undefined
                            }
                            {...field}
                        />
                    )}
                />
                {error && <FieldError id="band-name-error" errors={[error]} />}
            </Field>
            <div className="flex justify-end gap-2">
                <Button type="button" variant="outline" onClick={onCancel}>
                    {t("common.cancel")}
                </Button>
                <Button type="submit" disabled={form.formState.isSubmitting}>
                    {t(
                        form.formState.isSubmitting
                            ? "common.updating"
                            : "common.save"
                    )}
                </Button>
            </div>
        </form>
    );
}
