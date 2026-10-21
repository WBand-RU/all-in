import { describe, expect, it } from "vitest";
import { axisSchema, cellSchema } from "./forms";

describe("song matrix forms", () => {
    it("accepts custom axis names while preserving cell text verbatim", () => {
        const content = "  [Am]Куплет\n  сбивка на [G/B]  \n";
        expect(axisSchema.parse({ name: "  Заметки клавишника  " })).toEqual({
            name: "Заметки клавишника",
        });
        expect(cellSchema.parse({ content })).toEqual({ content });
    });
    it("allows empty cells and rejects blank names and oversized text", () => {
        expect(cellSchema.safeParse({ content: "" }).success).toBe(true);
        expect(axisSchema.safeParse({ name: "   " }).success).toBe(false);
        expect(axisSchema.safeParse({ name: "a".repeat(101) }).success).toBe(
            false
        );
        expect(
            cellSchema.safeParse({ content: "a".repeat(50001) }).success
        ).toBe(false);
    });
});
