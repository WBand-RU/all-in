import Keycloak, { type KeycloakConfig } from "keycloak-js";

const runtimeConfig = window.__WBAND_CONFIG__;
const keycloakConfig: KeycloakConfig = {
    url: runtimeConfig?.keycloakUrl || import.meta.env.VITE_KEYCLOAK_URL || "https://auth.wband.ru",
    realm: runtimeConfig?.keycloakRealm || import.meta.env.VITE_KEYCLOAK_REALM || "wband-dev",
    clientId: runtimeConfig?.keycloakClientId || import.meta.env.VITE_KEYCLOAK_CLIENT_ID || "web",
};

export const keycloak = new Keycloak(keycloakConfig);
