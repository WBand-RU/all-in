import { ReactKeycloakProvider } from "@react-keycloak/web";
import type { PropsWithChildren } from "react";
import { useIdentityStore } from "../stores/identity.store";
import { keycloak } from "../keycloak";
import { AuthContextProvider } from "./auth/AuthorizationProviderContext";

export function AuthorizedProvider(props: PropsWithChildren) {
    const setAccessToken = useIdentityStore(x => x.setAccessToken);

    return (
        <ReactKeycloakProvider
            authClient={keycloak}
            initOptions={{
                onLoad: "login-required", // 'login-required' or 'check-sso'
                scope: "openid profile email roles", // Request the 'email' scope
            }} // 'login-required' èëè 'check-sso'
            autoRefreshToken={true}
            onTokens={x => setAccessToken(x.token)}
            onEvent={(event) => {
                if (event === "onAuthSuccess" || event === "onAuthRefreshSuccess") {
                    setAccessToken(keycloak.token);
                }
                if (event === "onAuthLogout" || event === "onAuthError") {
                    setAccessToken(undefined);
                }
            }}
        >
            <AuthContextProvider>
                {props.children}
            </AuthContextProvider>
        </ReactKeycloakProvider>
    );
}
