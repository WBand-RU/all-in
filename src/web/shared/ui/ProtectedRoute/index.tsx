import { Navigate, Outlet, useLocation } from "react-router";
import { useAuthStore } from "../../../entities/session";

/**
 * Компонент для защиты маршрутов, требующих аутентификации.
 * Если пользователь не авторизован, перенаправляет на страницу входа.
 */
export const ProtectedRoute = () => {
    const { user } = useAuthStore();
    const location = useLocation();

    // Если пользователь не авторизован, перенаправляем на страницу входа
    // с сохранением изначального URL для возможного редиректа обратно после входа
    if (!user) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    // Если пользователь авторизован, рендерим дочерние компоненты (через Outlet)
    return <Outlet />;
};
