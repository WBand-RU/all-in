import Keycloak, { type KeycloakConfig } from "keycloak-js";

const keycloakConfig: KeycloakConfig = {
    url: import.meta.env.VITE_KEYCLOAK_URL ?? "https://auth.wband.ru",
    realm: import.meta.env.VITE_KEYCLOAK_REALM ?? "wband-dev",
    clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? "web",
};

export const keycloak = new Keycloak(keycloakConfig);
