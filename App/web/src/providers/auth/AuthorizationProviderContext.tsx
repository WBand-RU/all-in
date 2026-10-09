import { createContext, useContext, type PropsWithChildren } from "react";
import { useQuery } from "@tanstack/react-query";
import { getAuthMe } from "@/lib/generated-api/auth/auth";
import { Roles, type Roles as PlatformRole } from "../../types/roles";
import { getCsrfToken, login, logout, setCsrfToken } from "@/lib/auth-session";

export type AuthContext = {
    id?: string;
    username?: string;
    email?: string;
    name?: string;
    avatar?: string;
    roles: readonly string[];
    role?: PlatformRole;
    platformRoles: readonly PlatformRole[];
    hasRole: (role: string) => boolean;
    authenticated: boolean;
    loading: boolean;
    error: Error | null;
    login: () => void;
    logout: () => void;
    refresh: () => void;
};
const Context = createContext<AuthContext | null>(null);
export function useAuthContext() {
    const context = useContext(Context);
    if (!context) throw new Error("useAuthContext must be used within AuthContextProvider");
    return context;
}
export function AuthContextProvider({ children }: PropsWithChildren) {
    const session = useQuery({
        queryKey: ["auth", "session"],
        queryFn: async ({ signal }) => {
            const response = await getAuthMe({ credentials: "same-origin", cache: "no-store", signal });
            if (response.status === 401) {
                setCsrfToken(undefined);
                return null;
            }
            if (response.status !== 200) throw new Error("Не удалось загрузить профиль пользователя");
            const profile = response.data;
            setCsrfToken(profile.csrfToken);
            return profile;
        },
        retry: false,
        staleTime: 60_000,
        refetchInterval: 60_000,
    });
    const profile = session.data;
    const roles = profile?.roles ?? [];
    const platformRoles = Object.values(Roles).filter(role => roles.includes(role));
    return <Context.Provider value={{
        id: profile?.id, username: profile?.username ?? undefined, email: profile?.email ?? undefined,
        name: profile?.name ?? profile?.username ?? undefined, avatar: profile?.avatar ?? undefined, roles,
        role: platformRoles[0], platformRoles, hasRole: role => roles.includes(role),
        authenticated: !!profile && !!getCsrfToken(), loading: session.isPending,
        error: session.error, login, logout, refresh: () => { void session.refetch(); },
    }}>{children}</Context.Provider>;
}
