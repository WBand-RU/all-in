import Keycloak from "keycloak-js";
import { resolveKeycloakConfig } from "@/config/keycloak-config";

const keycloakConfig = resolveKeycloakConfig(window.__WBAND_CONFIG__, {
    VITE_KEYCLOAK_URL: import.meta.env.VITE_KEYCLOAK_URL,
    VITE_KEYCLOAK_REALM: import.meta.env.VITE_KEYCLOAK_REALM,
    VITE_KEYCLOAK_CLIENT_ID: import.meta.env.VITE_KEYCLOAK_CLIENT_ID,
});

export const keycloak = new Keycloak(keycloakConfig);
