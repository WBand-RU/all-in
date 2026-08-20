import { describe, expect, it } from "vitest";
import { resolveKeycloakConfig } from "./keycloak-config";

describe("resolveKeycloakConfig", () => {
    it("prefers runtime container values", () => {
        const result = resolveKeycloakConfig({
            keycloakUrl: "https://runtime.example",
            keycloakRealm: "runtime-realm",
            keycloakClientId: "runtime-client",
        }, {
            VITE_KEYCLOAK_URL: "https://build.example",
            VITE_KEYCLOAK_REALM: "build-realm",
            VITE_KEYCLOAK_CLIENT_ID: "build-client",
        });

        expect(result).toEqual({ url: "https://runtime.example", realm: "runtime-realm",
            clientId: "runtime-client" });
    });

    it("falls back to Vite build-time values", () => {
        const result = resolveKeycloakConfig(undefined, {
            VITE_KEYCLOAK_URL: "https://build.example",
            VITE_KEYCLOAK_REALM: "build-realm",
            VITE_KEYCLOAK_CLIENT_ID: "build-client",
        });

        expect(result).toEqual({ url: "https://build.example", realm: "build-realm",
            clientId: "build-client" });
    });

    it("uses development defaults when no configuration is supplied", () => {
        expect(resolveKeycloakConfig({}, {})).toEqual({ url: "https://auth.wband.ru",
            realm: "wband-dev", clientId: "web" });
    });
});
