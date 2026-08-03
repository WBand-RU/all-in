import Keycloak, { type KeycloakConfig } from "keycloak-js";

const keycloakConfig: KeycloakConfig = {
    url: "https://localhost:8080", // URL Keycloak
    realm: "wband", // realm
    clientId: "web", // Client ID
};

export const keycloak = new Keycloak(keycloakConfig);
