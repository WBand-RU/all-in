"use client";

import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Field, FieldError, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import type { AxisKind, SongAxis } from "@/features/song-content/SongContent";
import { axisSchema, partitionPresets, sectionPresets } from "./forms";

export function AxisDialog({
    kind,
    axis,
    onSave,
    onClose,
}: {
    kind: AxisKind;
    axis?: SongAxis;
    onSave: (name: string) => Promise<void>;
    onClose: () => void;
}) {
    const section = kind === "section";
    const form = useForm<z.infer<typeof axisSchema>>({
        resolver: zodResolver(axisSchema),
        defaultValues: { name: axis?.name ?? (section ? "Куплет" : "Слова") },
    });
    const pending = form.formState.isSubmitting;
    return (
        <Dialog
            open
            onOpenChange={(open) => {
                if (!open && !pending) onClose();
            }}
        >
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>
                        {axis
                            ? section
                                ? "Изменить секцию"
                                : "Изменить партию"
                            : section
                              ? "Новая секция"
                              : "Новая партия"}
                    </DialogTitle>
                    <DialogDescription>
                        {section
                            ? "Секция и её название общие для всех партий песни."
                            : "Выберите готовое название или введите своё. В партии будут все секции песни."}
                    </DialogDescription>
                </DialogHeader>
                <form
                    className="space-y-4"
                    onSubmit={form.handleSubmit(async ({ name }) => {
                        try {
                            await onSave(name);
                        } catch (error) {
                            const conflict =
                                typeof error === "object" &&
                                error !== null &&
                                "code" in error &&
                                String(error.code).endsWith("version_conflict");
                            form.setError("root", {
                                message: conflict
                                    ? "Название изменено другим участником. Закройте окно и откройте его заново."
                                    : "Не удалось сохранить. Попробуйте ещё раз.",
                            });
                        }
                    })}
                >
                    <fieldset disabled={pending} className="space-y-4">
                        <Field data-invalid={!!form.formState.errors.name}>
                            <FieldLabel htmlFor="axis-name">
                                {section
                                    ? "Название секции"
                                    : "Название партии"}
                            </FieldLabel>
                            <Controller
                                name="name"
                                control={form.control}
                                render={({ field }) => (
                                    <Input
                                        id="axis-name"
                                        maxLength={100}
                                        autoFocus
                                        aria-invalid={
                                            !!form.formState.errors.name
                                        }
                                        aria-describedby="axis-name-error"
                                        {...field}
                                    />
                                )}
                            />
                            <FieldError
                                id="axis-name-error"
                                errors={[form.formState.errors.name]}
                            />
                        </Field>
                        <div
                            className="flex flex-wrap gap-2"
                            role="group"
                            aria-label="Готовые названия"
                        >
                            {(section ? sectionPresets : partitionPresets).map(
                                (name) => (
                                    <Button
                                        key={name}
                                        type="button"
                                        variant="outline"
                                        size="sm"
                                        onClick={() =>
                                            form.setValue("name", name, {
                                                shouldValidate: true,
                                            })
                                        }
                                    >
                                        {name}
                                    </Button>
                                )
                            )}
                        </div>
                    </fieldset>
                    {form.formState.errors.root && (
                        <p role="alert" className="text-sm text-destructive">
                            {form.formState.errors.root.message}
                        </p>
                    )}
                    <DialogFooter>
                        <Button
                            type="button"
                            variant="outline"
                            disabled={pending}
                            onClick={onClose}
                        >
                            Отмена
                        </Button>
                        <Button type="submit" disabled={pending}>
                            {pending
                                ? "Сохранение…"
                                : axis
                                  ? "Сохранить"
                                  : "Добавить"}
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    );
}
