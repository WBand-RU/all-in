import { describe, expect, it } from "vitest";
import { splitChords } from "./chords";

describe("chord annotations", () => {
    it("highlights bracketed progressions and inline chords while retaining whitespace", () => {
        const text =
            "Интро:\n[| Am | Dm | C | G |]\nКуплет:\n  [Am]Текст [G/B] песни\n";
        const parts = splitChords(text);
        expect(
            parts.filter((part) => part.chord).map((part) => part.value)
        ).toEqual(["| Am | Dm | C | G |", "Am", "G/B"]);
        expect(parts.map((part) => part.value).join("")).toBe(
            text.replaceAll("[", "").replaceAll("]", "")
        );
    });
    it("keeps incomplete annotations and HTML as literal text", () => {
        const text = "<script>alert(1)</script> [Am\nDm] []";
        expect(splitChords(text).some((part) => part.chord)).toBe(false);
        expect(
            splitChords(text)
                .map((part) => part.value)
                .join("")
        ).toBe(text);
    });
});
