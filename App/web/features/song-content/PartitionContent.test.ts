import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { PartitionContent } from "./PartitionContent";

describe("single partition view", () => {
    it("shows every shared section in order and only text from the selected partition", () => {
        const base = {
            songId: "song",
            revision: 1,
            createdAt: "2026-10-20T00:00:00Z",
            updatedAt: null,
        };
        const html = renderToStaticMarkup(
            createElement(PartitionContent, {
                partitionId: "drums",
                content: {
                    sections: [
                        { ...base, id: "chorus", name: "Припев", order: 1 },
                        { ...base, id: "verse", name: "Куплет", order: 0 },
                    ],
                    partitions: [
                        { ...base, id: "drums", name: "Барабаны", order: 0 },
                    ],
                    cells: [
                        {
                            sectionId: "verse",
                            partitionId: "drums",
                            content: "[Am]Сбивка",
                            revision: 1,
                        },
                        {
                            sectionId: "verse",
                            partitionId: "words",
                            content: "Чужая партия",
                            revision: 1,
                        },
                    ],
                },
            })
        );
        expect(html.indexOf("Куплет")).toBeLessThan(html.indexOf("Припев"));
        expect(html).toContain("Сбивка");
        expect(html).toContain("В этой секции пока нет текста.");
        expect(html).not.toContain("Чужая партия");
        expect(html).not.toContain("[Am]");
    });
});
