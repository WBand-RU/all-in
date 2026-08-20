export {};

declare global {
    interface Window {
        __WBAND_CONFIG__?: {
            keycloakUrl?: string;
            keycloakRealm?: string;
            keycloakClientId?: string;
        };
    }
}
