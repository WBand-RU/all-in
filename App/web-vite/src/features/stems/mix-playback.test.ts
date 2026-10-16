import { describe, expect, it, vi } from "vitest";
import { MixPlayback } from "./mix-playback";

function setup(getUrl = vi.fn(async (id: string) => `https://storage.test/${id}`)) {
    const media = {
        src: "", error: null,
        play: vi.fn(async () => {}), pause: vi.fn(), load: vi.fn(),
        hasAttribute: vi.fn(() => !!media.src),
        removeAttribute: vi.fn(() => { media.src = ""; }),
    };
    return { media, getUrl, playback: new MixPlayback(media as unknown as HTMLAudioElement, getUrl) };
}

describe("mix streaming", () => {
    it("does not request audio or a signed URL before selection", () => {
        const { media, getUrl } = setup();
        expect(getUrl).not.toHaveBeenCalled();
        expect(media.src).toBe("");
        expect(media.load).not.toHaveBeenCalled();
    });

    it("resumes the same source without resetting playback or refreshing its URL", async () => {
        const { media, getUrl, playback } = setup();
        await playback.play("first");
        await playback.play("first");
        expect(getUrl).toHaveBeenCalledTimes(1);
        expect(media.load).toHaveBeenCalledTimes(1);
        expect(media.play).toHaveBeenCalledTimes(2);
    });

    it("releases the previous stream before requesting another", async () => {
        const { media, getUrl, playback } = setup();
        await playback.play("first");
        getUrl.mockImplementation(async () => {
            expect(media.src).toBe("");
            expect(media.pause).toHaveBeenCalledTimes(2);
            return "https://storage.test/second";
        });
        await playback.play("second");
        expect(media.src).toBe("https://storage.test/second");
    });

    it("ignores an older URL response when selections race", async () => {
        let resolveFirst!: (url: string) => void;
        const { media, playback } = setup(vi.fn((id: string) => id === "first"
            ? new Promise<string>(resolve => { resolveFirst = resolve; })
            : Promise.resolve("https://storage.test/second")));
        const first = playback.play("first");
        await playback.play("second");
        resolveFirst("https://storage.test/first");
        expect(await first).toBe(false);
        expect(media.src).toBe("https://storage.test/second");
        expect(media.play).toHaveBeenCalledTimes(1);
    });

    it("does not start a pending stream after stop or unmount", async () => {
        let resolveUrl!: (url: string) => void;
        const { media, playback } = setup(vi.fn(() =>
            new Promise<string>(resolve => { resolveUrl = resolve; })));
        const playing = playback.play("first");
        playback.stop();
        resolveUrl("https://storage.test/first");
        expect(await playing).toBe(false);
        expect(media.src).toBe("");
        expect(media.play).not.toHaveBeenCalled();
    });

    it("can retry after a signed URL request fails", async () => {
        const { media, getUrl, playback } = setup();
        getUrl.mockRejectedValueOnce(new Error("unavailable"));
        await expect(playback.play("first")).rejects.toThrow("unavailable");
        await playback.play("first");
        expect(media.src).toBe("https://storage.test/first");
        expect(getUrl).toHaveBeenCalledTimes(2);
    });
});
