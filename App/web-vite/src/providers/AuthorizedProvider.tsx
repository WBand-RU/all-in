import { useEffect, type PropsWithChildren } from "react";
import { AuthContextProvider, useAuthContext } from "./auth/AuthorizationProviderContext";

function AuthorizedContent({ children }: PropsWithChildren) {
    const { authenticated, loading, error, login, refresh } = useAuthContext();
    useEffect(() => {
        if (!loading && !error && !authenticated) login();
    }, [authenticated, loading, error, login]);
    if (error) return <div role="alert" className="p-6">
        <p>{error.message}</p>
        <button type="button" onClick={refresh}>Повторить</button>
    </div>;
    if (loading || !authenticated) return <p role="status" className="p-6">Загрузка профиля…</p>;
    return children;
}
export function AuthorizedProvider({ children }: PropsWithChildren) {
    return <AuthContextProvider><AuthorizedContent>{children}</AuthorizedContent></AuthContextProvider>;
}
