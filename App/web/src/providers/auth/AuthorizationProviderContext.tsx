import { createContext, useContext, useEffect, useMemo, useState, type PropsWithChildren } from "react";
import { Roles, type Roles as PlatformRole } from "../../types/roles";
import { useKeycloak } from "@react-keycloak/web";
import { apiClient } from "@/lib/axios-instance";

export type AuthContext = {
	email?: string;
	name?: string;
	avatar?: string;
	role?: PlatformRole;
	platformRoles: readonly PlatformRole[];
	hasRole: (role: PlatformRole) => boolean;
};

const Context = createContext<AuthContext>({
	email: undefined,
	platformRoles: [],
	hasRole: () => false,
});

export function useAuthContext() {
	return useContext(Context);
}

export function AuthContextProvider({ children }: PropsWithChildren) {
	const [email, setEmail] = useState<string>();
	const [name, setName] = useState<string>();
	const [avatar, setAvatar] = useState<string>();
	const { initialized, keycloak } = useKeycloak();

	useEffect(() => {
		if (!initialized) {
			return;
		}

		if (!keycloak.authenticated) {
			setEmail(undefined);
			setName(undefined);
			setAvatar(undefined);
			return;
		}

		keycloak.loadUserInfo()
			.then(info => {
				setEmail(info.email);
				setName(info.name ?? info.preferred_username);
				setAvatar(info.picture);
            });

		void apiClient.get("/api/user/me", {
			headers: { Authorization: `Bearer ${keycloak.token}` },
		});
	}, [initialized, keycloak]);

	const platformRoles = useMemo(() =>
		Object.values(Roles).filter(role => keycloak.hasRealmRole(role)),
	[keycloak, initialized]);
	const hasRole = (role: PlatformRole) => platformRoles.includes(role);
	const role = platformRoles[0];

	return (
		<Context.Provider value={{ email, name, avatar, role, platformRoles, hasRole }}>
			{children}
		</Context.Provider>
	);
}
