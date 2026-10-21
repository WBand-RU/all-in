import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { ChordText, SongContent } from "./SongContent";

describe("song content display", () => {
    it("renders highlighted chords without delimiters and escapes literal HTML", () => {
        const html = renderToStaticMarkup(
            createElement(ChordText, {
                text: "[Am]слова [G/B]\n[| C | F |] <script>",
            })
        );
        expect(html).toContain("text-primary");
        expect(html).toContain(">Am</span>");
        expect(html).toContain(">G/B</span>");
        expect(html).toContain(">| C | F |</span>");
        expect(html).not.toContain("[Am]");
        expect(html).not.toContain("[G/B]");
        expect(html).toContain("&lt;script&gt;");
    });
    it("renders sections as shared rows and partitions as ordered columns with independent text", () => {
        const base = {
            songId: "song",
            revision: 1,
            createdAt: "2026-10-20T00:00:00Z",
            updatedAt: null,
        };
        const html = renderToStaticMarkup(
            createElement(SongContent, {
                content: {
                    sections: [
                        { ...base, id: "chorus", name: "Припев", order: 1 },
                        { ...base, id: "verse", name: "Куплет", order: 0 },
                    ],
                    partitions: [
                        { ...base, id: "drums", name: "Барабаны", order: 1 },
                        { ...base, id: "lyrics", name: "Слова", order: 0 },
                    ],
                    cells: [
                        {
                            sectionId: "verse",
                            partitionId: "lyrics",
                            content: "[Am]Текст куплета",
                            revision: 1,
                        },
                        {
                            sectionId: "verse",
                            partitionId: "drums",
                            content: "Сбивка",
                            revision: 1,
                        },
                        {
                            sectionId: "chorus",
                            partitionId: "lyrics",
                            content: "Текст припева",
                            revision: 1,
                        },
                    ],
                },
            })
        );
        expect(html.indexOf("Слова")).toBeLessThan(html.indexOf("Барабаны"));
        expect(html.indexOf("Куплет")).toBeLessThan(html.indexOf("Припев"));
        expect(html.match(/scope="row"/g)).toHaveLength(2);
        expect(html.match(/<td/g)).toHaveLength(4);
        expect(html).toContain('aria-label="Куплет · Слова"');
        expect(html).toContain('aria-label="Куплет · Барабаны"');
        expect(html).toContain('aria-label="Припев · Барабаны"');
        expect(html).toContain("Сбивка");
        expect(html).not.toContain("[Am]");
        expect(html).not.toContain("bg-primary");
    });
});
