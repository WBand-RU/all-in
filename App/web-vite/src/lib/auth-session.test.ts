import { afterEach, describe, expect, it, vi } from "vitest";
import { axiosInstance } from "./axios-instance";
import { login, setCsrfToken } from "./auth-session";

afterEach(() => { setCsrfToken(undefined); vi.unstubAllGlobals(); });
describe("cookie authorization", () => {
    it.each(["post", "put", "delete", "patch"])("sends CSRF on %s without exposing bearer tokens", async method => {
        setCsrfToken("session-csrf");
        await axiosInstance.request({ method, url: "/band-api/bands", adapter: async config => {
            expect(config.withCredentials).toBe(true);
            expect(config.headers.get("X-CSRF-TOKEN")).toBe("session-csrf");
            expect(config.headers.has("Authorization")).toBe(false);
            return { config, status: 200, statusText: "OK", headers: {}, data: {} };
        } });
    });
    it("omits CSRF on reads", async () => {
        setCsrfToken("session-csrf");
        await axiosInstance.get("/band-api/bands", { adapter: async config => {
            expect(config.headers.has("X-CSRF-TOKEN")).toBe(false);
            return { config, status: 200, statusText: "OK", headers: {}, data: {} };
        } });
    });
    it("preserves the current route through login", () => {
        const assign = vi.fn();
        vi.stubGlobal("window", { location: { pathname: "/app/songs", search: "?band=123", hash: "#section", assign } });
        login();
        expect(assign).toHaveBeenCalledWith("/auth/login?returnUrl=%2Fapp%2Fsongs%3Fband%3D123%23section");
    });
});
