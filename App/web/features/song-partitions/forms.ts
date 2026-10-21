import { z } from "zod";

export const partitionPresets = [
    "Слова",
    "Аккорды",
    "Заметки барабанщика",
    "Гитарная партия",
    "Басовая партия",
    "Вокальная партия",
];
export const sectionPresets = [
    "Интро",
    "Куплет",
    "Припев",
    "Бридж",
    "Проигрыш",
    "Аутро",
];
export const axisSchema = z.object({
    name: z
        .string()
        .trim()
        .min(1, "Введите название")
        .max(100, "Не более 100 символов"),
});
export const cellSchema = z.object({
    content: z.string().max(50000, "Не более 50 000 символов"),
});
