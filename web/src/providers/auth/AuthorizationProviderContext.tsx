import { createContext, useContext, useEffect, useState, type PropsWithChildren } from "react";
import type { Roles } from "../../types/roles";
import { useKeycloak } from "@react-keycloak/web";

type AuthContext = {
	email?: string;
	role?: Roles;
};

const Context = createContext<AuthContext>({
	email: undefined,
	role: undefined,
});

export function useAuthContext() {
	return useContext(Context);
}

export function AuthContextProvider({ children }: PropsWithChildren) {
	const [email, setEmail] = useState<string>();
	const [role, setRole] = useState<Roles>();
	const { initialized, keycloak } = useKeycloak();

	useEffect(() => {
		if (!initialized) {
			return;
		}

		if (!keycloak.authenticated) {
			setEmail(undefined);
			setRole(undefined);
		}

		keycloak.loadUserInfo()
			.then(info => {
				setEmail(info.email);
			});

		if (keycloak.hasRealmRole("admin")) {
			setRole("admin");
		} else if (keycloak.hasRealmRole("user")) {
			setRole("user");
		} else if (keycloak.hasRealmRole("moderator")) {
			setRole("moderator");
		} else {
			setRole(undefined);
		}
	}, [initialized, keycloak]);

	function hasRole(role: Roles): boolean {
		return keycloak.hasRealmRole(role);
	}

	return (
		<Context.Provider value={{ email, role, }}>
			{children}
		</Context.Provider>
	);
}