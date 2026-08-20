import type { KeycloakConfig } from "keycloak-js";

export interface RuntimeKeycloakConfig {
    keycloakUrl?: string;
    keycloakRealm?: string;
    keycloakClientId?: string;
}

export interface BuildTimeKeycloakConfig {
    VITE_KEYCLOAK_URL?: string;
    VITE_KEYCLOAK_REALM?: string;
    VITE_KEYCLOAK_CLIENT_ID?: string;
}

export function resolveKeycloakConfig(runtime: RuntimeKeycloakConfig | undefined,
    buildTime: BuildTimeKeycloakConfig): KeycloakConfig {
    return {
        url: runtime?.keycloakUrl || buildTime.VITE_KEYCLOAK_URL || "https://auth.wband.ru",
        realm: runtime?.keycloakRealm || buildTime.VITE_KEYCLOAK_REALM || "wband-dev",
        clientId: runtime?.keycloakClientId || buildTime.VITE_KEYCLOAK_CLIENT_ID || "web",
    };
}
