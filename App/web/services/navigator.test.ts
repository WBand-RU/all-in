import { describe, expect, it } from "vitest";
import { resolveAppPath } from "./navigator";

describe("App Router navigation", () => {
    it.each([
        ["/songs/new", "/app/bands", "/app/songs/new"],
        ["/app/invitations", "/app", "/app/invitations"],
        ["band-id", "/app/bands", "/app/bands/band-id"],
        ["..", "/app/bands/band-id", "/app/bands/"],
        ["../../..", "/app/bands/band-id", "/app"],
    ])("resolves %s from %s", (path, current, expected) => {
        expect(resolveAppPath(path, current)).toBe(expected);
    });
});
