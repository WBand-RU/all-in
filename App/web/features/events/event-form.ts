import { z } from "zod";

export const eventSchema = z.object({
    bandId: z.string().uuid("Выберите группу"),
    title: z.string().trim().min(1, "Введите название").max(200),
    startsAt: z
        .string()
        .min(1, "Укажите дату и время")
        .refine(
            (value) => Number.isFinite(new Date(value).getTime()),
            "Некорректная дата"
        ),
    venue: z.string().max(500),
    description: z.string().max(5000),
    songIds: z
        .array(z.string().uuid())
        .max(200)
        .refine((ids) => new Set(ids).size === ids.length),
});
export type EventFormValues = z.infer<typeof eventSchema>;

export function localDateTime(iso: string) {
    const date = new Date(iso);
    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
    return local.toISOString().slice(0, 16);
}
