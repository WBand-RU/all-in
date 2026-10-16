import {
    abortStemBatch, audioMimeType, completeStemBatch, completeStemFile,
    createStemBatch, getStemUploadUrl, sha256, uploadToSignedUrl, type CreateStemInput, type StemKind,
} from "./api";

export type UploadPhase = "preparing" | "uploading" | "committing" | "cleaning";

/** A network failure hid both the commit result and its reconciliation response. */
export class UploadOutcomeUnknownError extends Error {
    constructor() { super("Could not confirm whether the complete folder was committed"); }
}

function statusOf(error: unknown): number | undefined {
    const value = error as { status?: number; response?: { status?: number } };
    return value?.response?.status ?? value?.status;
}

function codeOf(error: unknown): string | undefined {
    return (error as { response?: { data?: { code?: string } } })?.response?.data?.code;
}

function throwIfCancelled(signal?: AbortSignal) {
    if (signal?.aborted) throw new DOMException("Upload cancelled", "AbortError");
}

function pause(milliseconds: number, signal?: AbortSignal): Promise<void> {
    return new Promise((resolve, reject) => {
        throwIfCancelled(signal);
        const cancel = () => { clearTimeout(timer); reject(new DOMException("Upload cancelled", "AbortError")); };
        const timer = setTimeout(() => { signal?.removeEventListener("abort", cancel); resolve(); }, milliseconds);
        signal?.addEventListener("abort", cancel, { once: true });
    });
}

/** Retries temporary failures with cancellation; validation and permission failures are final. */
export async function retryUpload<T>(operation: () => Promise<T>, signal?: AbortSignal): Promise<T> {
    for (let attempt = 0; ; attempt++) {
        throwIfCancelled(signal);
        try { return await operation(); }
        catch (error) {
            throwIfCancelled(signal);
            const status = statusOf(error);
            const aborted = error instanceof Error && ["AbortError", "CanceledError"].includes(error.name);
            const temporary = status === undefined || status === 408 || status === 429 || status >= 500
                || codeOf(error) === "file_object_unavailable";
            if (aborted || !temporary || attempt >= 2) throw error;
            await pause(attempt === 0 ? 500 : 1500, signal);
        }
    }
}

/** Stages a folder, verifies every file and commits the complete folder once. */
export async function uploadFolder(songId: string, files: File[], inferKind: (name: string) => StemKind,
    onProgress: (progress: number) => void, onPhase: (phase: UploadPhase) => void,
    signal: AbortSignal): Promise<void> {
    const batchId = crypto.randomUUID();
    let initialized = false;
    let commitAttempted = false;
    try {
        onPhase("preparing");
        const inputs: CreateStemInput[] = [];
        if (files.length === 0 || files.length > 100) throw new Error("Select between 1 and 100 audio files");
        for (const file of files) {
            throwIfCancelled(signal);
            inputs.push({ name: file.name.replace(/\.[^.]+$/, ""), kind: inferKind(file.name),
                groupId: null, fileName: file.name, mimeType: audioMimeType(file), size: file.size,
                sha256: await sha256(file) });
        }
        throwIfCancelled(signal);
        initialized = true;
        const batch = await retryUpload(() => createStemBatch(songId, inputs, batchId, signal), signal);
        if (batch.files.length !== files.length) throw new Error("Upload manifest does not match selected files");
        onPhase("uploading");
        for (let index = 0; index < files.length; index++) {
            const ticket = batch.files[index];
            await retryUpload(async () => {
                const url = await getStemUploadUrl(songId, batchId, ticket.stem.fileId, signal);
                await uploadToSignedUrl(files[index], url,
                    current => onProgress(Math.min(99, Math.round((index + current / 100) / files.length * 100))), signal);
            }, signal);
            await retryUpload(() => completeStemFile(ticket.stem.fileId, signal), signal);
        }
        throwIfCancelled(signal);
        onPhase("committing");
        commitAttempted = true;
        await retryUpload(() => completeStemBatch(songId, batchId, signal), signal);
    }
    catch (error) {
        if (initialized) {
            onPhase("cleaning");
            try { await retryUpload(() => abortStemBatch(songId, batchId)); }
            catch (cleanupError) {
                // A lost commit response must not turn a successful whole-folder commit into failure.
                if (codeOf(cleanupError) === "stem_upload_already_completed") return;
                if (commitAttempted) throw new UploadOutcomeUnknownError();
                // The server's persisted manifest will retry cleanup even if this request fails.
            }
        }
        throw error;
    }
}
