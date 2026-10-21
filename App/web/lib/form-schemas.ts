import { z } from "zod";

export const bandNameSchema = z.object({
    name: z.string().trim().min(1).max(120),
});
export const invitationSchema = z.object({
    email: z.string().trim().toLowerCase().pipe(z.email()),
    role: z.enum(["Member", "Admin"]),
});
export const songSchema = z.object({
    bandId: z.string().min(1),
    title: z.string().trim().min(1).max(200),
    author: z.string().refine((value) => {
        const authors = value
            .split(",")
            .map((author) => author.trim())
            .filter(Boolean);
        return (
            authors.length <= 20 &&
            authors.every((author) => author.length <= 200)
        );
    }, "Укажите не более 20 авторов, до 200 символов каждый"),
    key: z
        .string()
        .refine(
            (value) =>
                !value || /^[CDEFGAB](#|b)?(m|maj|min|dim|aug)?$/.test(value),
            "Укажите тональность, например C, F#m или Bb"
        ),
    bpm: z.number().int().min(20).max(400).nullable(),
});
