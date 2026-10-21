import type { PartitionResponse } from "@/lib/generated/web-api/models";

type Choice = { id: string; name: string };
const memory = new Map<string, string>();
export const stagePreferenceEvent = "wband-stage-partition-change";
const keys = (userId: string, songId: string) => ({
    song: `wband:stage-partition:${userId}:${songId}`,
    preferred: `wband:stage-partition:${userId}:preferred-name`,
});

function read(key: string) {
    if (memory.has(key)) return memory.get(key)!;
    try {
        return window.localStorage.getItem(key) ?? "";
    } catch {
        return "";
    }
}
function write(key: string, value: string) {
    // Keep the current session usable even when storage is disabled or full.
    memory.set(key, value);
    try {
        window.localStorage.setItem(key, value);
    } catch {
        /* Retain the in-memory choice. */
    }
}
export function stagePartitionSnapshot(
    userId: string | undefined,
    songId: string
): string {
    if (!userId || typeof window === "undefined") return '["",""]';
    const key = keys(userId, songId);
    return JSON.stringify([read(key.song), read(key.preferred)]);
}
export function rememberStagePartition(
    userId: string | undefined,
    songId: string,
    partition: Pick<PartitionResponse, "id" | "name">
) {
    if (!userId || typeof window === "undefined") return;
    const key = keys(userId, songId);
    write(key.song, JSON.stringify({ id: partition.id, name: partition.name }));
    write(key.preferred, partition.name);
    window.dispatchEvent(new Event(stagePreferenceEvent));
}
export function subscribeStagePartition(listener: () => void) {
    const changed = (event: StorageEvent) => {
        if (event.key) memory.delete(event.key);
        else memory.clear();
        listener();
    };
    window.addEventListener("storage", changed);
    window.addEventListener(stagePreferenceEvent, listener);
    return () => {
        window.removeEventListener("storage", changed);
        window.removeEventListener(stagePreferenceEvent, listener);
    };
}
const normalized = (name: string) =>
    name
        .normalize("NFC")
        .trim()
        .replace(/\s+/g, " ")
        .toLocaleLowerCase("ru-RU");

export function resolveStagePartition(
    partitions: PartitionResponse[],
    snapshot: string
): PartitionResponse | undefined {
    let choice: Choice | undefined;
    let preferred = "";
    try {
        const [saved, fallback] = JSON.parse(snapshot);
        preferred = typeof fallback === "string" ? fallback : "";
        if (typeof saved === "string" && saved) {
            const parsed = JSON.parse(saved);
            if (
                typeof parsed?.id === "string" &&
                typeof parsed?.name === "string"
            )
                choice = parsed;
        }
    } catch {
        /* Ignore malformed browser preferences. */
    }
    if (choice) {
        return (
            partitions.find((partition) => partition.id === choice.id) ??
            partitions.find(
                (partition) =>
                    normalized(partition.name) === normalized(choice.name)
            )
        );
    }
    if (preferred)
        return partitions.find(
            (partition) => normalized(partition.name) === normalized(preferred)
        );
    return [...partitions].sort((a, b) => Number(a.order) - Number(b.order))[0];
}
