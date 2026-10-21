import { describe, expect, it } from "vitest";
import { bandNameSchema, invitationSchema, songSchema } from "./form-schemas";

const song = {
    bandId: "band",
    title: "Song",
    author: "Author",
    key: "F#m",
    bpm: 120,
};
describe("forms and API validation", () => {
    it("normalizes names and invitation email before sending a mutation", () => {
        expect(bandNameSchema.parse({ name: "  Band  " })).toEqual({
            name: "Band",
        });
        expect(
            invitationSchema.parse({
                email: "  MUSICIAN@EXAMPLE.COM  ",
                role: "Admin",
            }).email
        ).toBe("musician@example.com");
    });
    it("accepts optional BPM but rejects invalid tempos, keys, and empty titles", () => {
        expect(songSchema.safeParse({ ...song, bpm: null }).success).toBe(true);
        for (const value of [
            { ...song, bpm: 19 },
            { ...song, bpm: 401 },
            { ...song, bpm: 120.5 },
            { ...song, key: "H" },
            { ...song, title: "  " },
        ])
            expect(songSchema.safeParse(value).success).toBe(false);
    });
    it("preserves API author limits and prevents assigning the owner role through an invitation", () => {
        expect(
            songSchema.safeParse({
                ...song,
                author: Array(21).fill("Author").join(","),
            }).success
        ).toBe(false);
        expect(
            invitationSchema.safeParse({
                email: "a@example.com",
                role: "Owner",
            }).success
        ).toBe(false);
    });
});
