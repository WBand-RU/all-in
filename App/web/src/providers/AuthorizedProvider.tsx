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
				onLoad: "login-required", // 'login-required' или 'check-sso'
				scope: 'openid profile email roles', // Request the 'email' scope
			}} // 'login-required' или 'check-sso'
			autoRefreshToken={true}
			onTokens={x => setAccessToken(x.token)}
		>
			<AuthContextProvider>
				{props.children}
			</AuthContextProvider>
		</ReactKeycloakProvider>
	);
}
