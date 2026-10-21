import { afterEach, describe, expect, it, vi } from "vitest";

const cookieStore = {
    has: vi.fn(),
    toString: () => "WBand.Session=opaque-session",
};
vi.mock("server-only", () => ({}));
vi.mock("react", () => ({ cache: (fn: unknown) => fn }));
vi.mock("next/headers", () => ({ cookies: async () => cookieStore }));
vi.mock("next/navigation", () => ({
    redirect: (url: string) => {
        throw new Error(`redirect:${url}`);
    },
}));
import { requireSession } from "./session";

afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
    vi.clearAllMocks();
});
describe("server BFF session verification", () => {
    it("redirects an anonymous request before loading a protected page", async () => {
        cookieStore.has.mockReturnValue(false);
        const fetch = vi.fn();
        vi.stubGlobal("fetch", fetch);
        await expect(requireSession("/app/songs/new")).rejects.toThrow(
            "redirect:/auth/login?returnUrl=%2Fapp%2Fsongs%2Fnew"
        );
        expect(fetch).not.toHaveBeenCalled();
    });
    it("validates the HttpOnly cookie against the configured BFF without a shared response cache", async () => {
        cookieStore.has.mockReturnValue(true);
        vi.stubEnv("BFF_ORIGIN", "http://bff.test:4000");
        const fetch = vi
            .fn()
            .mockResolvedValue(new Response("{}", { status: 200 }));
        vi.stubGlobal("fetch", fetch);
        await requireSession("/app/bands");
        const [url, init] = fetch.mock.calls[0];
        expect(String(url)).toBe("http://bff.test:4000/auth/me");
        expect(init).toMatchObject({
            cache: "no-store",
            redirect: "manual",
            headers: { cookie: "WBand.Session=opaque-session" },
        });
    });
    it("redirects an expired session and preserves the requested detail route", async () => {
        cookieStore.has.mockReturnValue(true);
        vi.stubGlobal(
            "fetch",
            vi.fn().mockResolvedValue(new Response(null, { status: 401 }))
        );
        await expect(requireSession("/app/bands/123")).rejects.toThrow(
            "redirect:/auth/login?returnUrl=%2Fapp%2Fbands%2F123"
        );
    });
    it("shows an availability error instead of starting a login loop on a BFF failure", async () => {
        cookieStore.has.mockReturnValue(true);
        vi.stubGlobal(
            "fetch",
            vi.fn().mockResolvedValue(new Response(null, { status: 503 }))
        );
        await expect(requireSession("/app")).rejects.toThrow(
            "Сервис авторизации временно недоступен"
        );
    });
});
