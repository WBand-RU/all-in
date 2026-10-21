import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { PartitionResponse } from "@/lib/generated/web-api/models";
import {
    rememberStagePartition,
    resolveStagePartition,
    stagePartitionSnapshot,
    subscribeStagePartition,
} from "./stage-partition-preference";

const base = {
    songId: "song",
    revision: 1,
    createdAt: "2026-10-20T00:00:00Z",
    updatedAt: null,
};
const parts: PartitionResponse[] = [
    { ...base, id: "words", name: "Слова", order: 0 },
    { ...base, id: "drums", name: "Заметки барабанщика", order: 1 },
];
let sequence = 0;
let user: string;
class Browser extends EventTarget {
    values = new Map<string, string>();
    localStorage = {
        getItem: (key: string) => this.values.get(key) ?? null,
        setItem: (key: string, value: string) => {
            this.values.set(key, value);
        },
    };
}
let browser: Browser;
beforeEach(() => {
    user = `stage-test-user-${++sequence}`;
    browser = new Browser();
    vi.stubGlobal("window", browser);
});
afterEach(() => vi.unstubAllGlobals());

describe("stage partition preference", () => {
    it("restores the selected ID from browser storage after a fresh module load", async () => {
        rememberStagePartition(user, "song", parts[1]);
        vi.resetModules();
        const fresh = await import("./stage-partition-preference");
        expect(
            fresh.resolveStagePartition(
                parts,
                fresh.stagePartitionSnapshot(user, "song")
            )?.id
        ).toBe("drums");
    });
    it("keeps choices separate for different users and songs and suggests the same kind for another song", () => {
        rememberStagePartition(user, "song-a", parts[1]);
        const another = [
            { ...parts[0], id: "other-words" },
            { ...parts[1], id: "other-drums" },
        ];
        expect(
            resolveStagePartition(
                another,
                stagePartitionSnapshot(user, "song-b")
            )?.id
        ).toBe("other-drums");
        expect(
            resolveStagePartition(
                parts,
                stagePartitionSnapshot("different-user", "song-a")
            )?.id
        ).toBe("words");
        rememberStagePartition(user, "song-b", parts[0]);
        expect(
            resolveStagePartition(parts, stagePartitionSnapshot(user, "song-a"))
                ?.id
        ).toBe("drums");
    });
    it("survives renaming and handles a removed partition without displaying a different part", () => {
        rememberStagePartition(user, "song", parts[1]);
        expect(
            resolveStagePartition(
                [{ ...parts[1], name: "Барабаны" }],
                stagePartitionSnapshot(user, "song")
            )?.id
        ).toBe("drums");
        expect(
            resolveStagePartition(
                [parts[0]],
                stagePartitionSnapshot(user, "song")
            )
        ).toBeUndefined();
        expect(resolveStagePartition(parts, "corrupted storage")?.id).toBe(
            "words"
        );
    });
    it("matches remembered names independently of IDs, case and surrounding whitespace", () => {
        const snapshot = JSON.stringify(["", "  заметки  барабанщика  "]);
        expect(resolveStagePartition(parts, snapshot)?.id).toBe("drums");
    });
    it("keeps the current choice working when persistent storage is unavailable", () => {
        browser.localStorage.getItem = () => {
            throw new Error("Storage disabled");
        };
        browser.localStorage.setItem = () => {
            throw new Error("Storage disabled");
        };
        expect(() =>
            rememberStagePartition(user, "song", parts[1])
        ).not.toThrow();
        expect(
            resolveStagePartition(parts, stagePartitionSnapshot(user, "song"))
                ?.id
        ).toBe("drums");
    });
    it("adopts changes from another tab and notifies the mounted selector", () => {
        rememberStagePartition(user, "song", parts[0]);
        const listener = vi.fn();
        const unsubscribe = subscribeStagePartition(listener);
        const key = `wband:stage-partition:${user}:song`;
        browser.localStorage.setItem(
            key,
            JSON.stringify({ id: parts[1].id, name: parts[1].name })
        );
        const event = new Event("storage");
        Object.defineProperty(event, "key", { value: key });
        browser.dispatchEvent(event);
        expect(listener).toHaveBeenCalledOnce();
        expect(
            resolveStagePartition(parts, stagePartitionSnapshot(user, "song"))
                ?.id
        ).toBe("drums");
        unsubscribe();
    });
});
