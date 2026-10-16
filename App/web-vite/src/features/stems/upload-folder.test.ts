import { beforeEach, afterEach, describe, expect, it, vi } from "vitest";
import * as api from "./api";
import { retryUpload, uploadFolder, UploadOutcomeUnknownError } from "./upload-folder";

vi.mock("./api", () => ({
    abortStemBatch: vi.fn(), audioMimeType: vi.fn(() => "audio/wav"),
    completeStemBatch: vi.fn(), completeStemFile: vi.fn(), createStemBatch: vi.fn(),
    getStemUploadUrl: vi.fn(), sha256: vi.fn(), uploadToSignedUrl: vi.fn(),
}));
const files = [new File(["one"], "one.wav"), new File(["two"], "two.wav")];
const controller = () => new AbortController();
const failure = (status: number, code?: string) => ({ response: { status, data: { code } } });

beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(api.sha256).mockResolvedValue("a".repeat(64));
    vi.mocked(api.createStemBatch).mockResolvedValue({ batchId: "batch", contentVersion: 0,
        files: files.map((_, index) => ({ stem: { fileId: `file-${index}` }, uploadUrl: "old-url" })) } as api.CreateStemBatchResult);
    vi.mocked(api.getStemUploadUrl).mockResolvedValue("fresh-url");
    vi.mocked(api.uploadToSignedUrl).mockResolvedValue(undefined);
    vi.mocked(api.completeStemFile).mockResolvedValue(undefined);
    vi.mocked(api.completeStemBatch).mockResolvedValue(undefined);
    vi.mocked(api.abortStemBatch).mockResolvedValue(undefined);
});
afterEach(() => vi.useRealTimers());
const run = (signal = controller().signal) => uploadFolder("song", files, () => "Other", vi.fn(), vi.fn(), signal);

describe("atomic folder upload", () => {
    it("commits once after both files have been verified", async () => {
        await run();
        expect(api.completeStemFile).toHaveBeenCalledTimes(2);
        expect(api.completeStemBatch).toHaveBeenCalledTimes(1);
        expect(vi.mocked(api.completeStemBatch).mock.invocationCallOrder[0])
            .toBeGreaterThan(vi.mocked(api.completeStemFile).mock.invocationCallOrder[1]);
        expect(api.uploadToSignedUrl).toHaveBeenCalledWith(files[0], "fresh-url", expect.any(Function), expect.any(AbortSignal));
        expect(api.abortStemBatch).not.toHaveBeenCalled();
    });
    it("aborts the entire session if the second file fails validation", async () => {
        vi.mocked(api.completeStemFile).mockResolvedValueOnce(undefined).mockRejectedValueOnce(failure(400));
        await expect(run()).rejects.toEqual(failure(400));
        expect(api.completeStemBatch).not.toHaveBeenCalled();
        expect(api.abortStemBatch).toHaveBeenCalledTimes(1);
    });
    it("cleans a lost initialization response using the original batch identifier", async () => {
        vi.mocked(api.createStemBatch).mockRejectedValue(failure(400));
        await expect(run()).rejects.toEqual(failure(400));
        expect(vi.mocked(api.abortStemBatch).mock.calls[0][1]).toBe(vi.mocked(api.createStemBatch).mock.calls[0][2]);
    });
    it("cancels an active upload and cleans its session without committing", async () => {
        const abort = controller();
        vi.mocked(api.uploadToSignedUrl).mockImplementationOnce(async () => { abort.abort(); throw new DOMException("Cancelled", "AbortError"); });
        await expect(run(abort.signal)).rejects.toMatchObject({ name: "AbortError" });
        expect(api.abortStemBatch).toHaveBeenCalledTimes(1);
        expect(api.completeStemBatch).not.toHaveBeenCalled();
    });
    it("recognizes a committed folder when the commit response was lost", async () => {
        vi.mocked(api.completeStemBatch).mockRejectedValue(failure(400));
        vi.mocked(api.abortStemBatch).mockRejectedValue(failure(409, "stem_upload_already_completed"));
        await expect(run()).resolves.toBeUndefined();
    });
    it("reports upload failure even if immediate cleanup is unavailable", async () => {
        vi.mocked(api.uploadToSignedUrl).mockRejectedValue(failure(403));
        vi.mocked(api.abortStemBatch).mockRejectedValue(failure(403));
        await expect(run()).rejects.toEqual(failure(403));
        expect(api.completeStemBatch).not.toHaveBeenCalled();
    });
    it("does not claim rollback if both the commit and reconciliation responses are unavailable", async () => {
        vi.mocked(api.completeStemBatch).mockRejectedValue(failure(400));
        vi.mocked(api.abortStemBatch).mockRejectedValue(failure(403));
        await expect(run()).rejects.toBeInstanceOf(UploadOutcomeUnknownError);
    });
});
describe("temporary failure retries", () => {
    it("retries a transient error and succeeds", async () => {
        vi.useFakeTimers();
        const operation = vi.fn().mockRejectedValueOnce(failure(503)).mockResolvedValue("ok");
        const result = retryUpload(operation);
        await vi.runAllTimersAsync();
        await expect(result).resolves.toBe("ok");
        expect(operation).toHaveBeenCalledTimes(2);
    });
    it("limits retries to three attempts", async () => {
        vi.useFakeTimers();
        const operation = vi.fn().mockRejectedValue(failure(503));
        const result = expect(retryUpload(operation)).rejects.toEqual(failure(503));
        await vi.runAllTimersAsync();
        await result;
        expect(operation).toHaveBeenCalledTimes(3);
    });
    it("does not retry permission failures", async () => {
        const operation = vi.fn().mockRejectedValue(failure(403));
        await expect(retryUpload(operation)).rejects.toEqual(failure(403));
        expect(operation).toHaveBeenCalledTimes(1);
    });
    it("cancels during the retry delay", async () => {
        vi.useFakeTimers();
        const abort = controller();
        const operation = vi.fn().mockRejectedValue(failure(503));
        const result = expect(retryUpload(operation, abort.signal)).rejects.toMatchObject({ name: "AbortError" });
        await Promise.resolve();
        abort.abort();
        await result;
        expect(operation).toHaveBeenCalledTimes(1);
    });
});
