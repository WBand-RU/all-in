import Keycloak, { type KeycloakConfig } from "keycloak-js";

const keycloakConfig: KeycloakConfig = {
    url: "https://auth.wband.ru", // URL Keycloak
    realm: "wband-dev", // realm
    clientId: "web", // Client ID
};

export const keycloak = new Keycloak(keycloakConfig);
