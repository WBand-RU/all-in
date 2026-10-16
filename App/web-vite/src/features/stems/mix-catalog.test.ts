import { describe, expect, it } from "vitest";
import type { MixArtifact } from "./api";
import { listeningMixes } from "./mix-catalog";

const wave: MixArtifact = { id: "wave", fileId: "wave-file", kind: "Full",
    group: "Общий микс", name: "Полный микс", format: "wav" };
const mp3: MixArtifact = { ...wave, id: "mp3", fileId: "mp3-file", format: "mp3" };

describe("listening formats", () => {
    it("prefers MP3 over WAV regardless of output arrival order", () => {
        expect(listeningMixes([wave, mp3])).toEqual([{ primary: mp3, formats: [wave, mp3] }]);
        expect(listeningMixes([mp3, wave])[0].primary).toBe(mp3);
    });
    it("permits listening before the compressed output has been generated", () => {
        expect(listeningMixes([wave])).toEqual([{ primary: wave, formats: [wave] }]);
    });
    it("keeps focus and minus mixes and identically named stems distinct", () => {
        const first = { ...mp3, kind: "Focus" as const, targetStemId: "first" };
        const second = { ...first, targetStemId: "second" };
        const minus = { ...first, kind: "Minus" as const };
        expect(listeningMixes([wave, first, second, minus])).toHaveLength(4);
    });

    it("deduplicates a retried format even if its display name changed", () => {
        const original = { ...mp3, kind: "Focus" as const, targetStemId: "first" };
        const retry = { ...original, id: "retry", name: "Renamed stem" };
        expect(listeningMixes([original, retry])).toEqual([{ primary: retry, formats: [retry] }]);
    });
});
