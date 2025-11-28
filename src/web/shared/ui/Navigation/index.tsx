import { type FC } from "react";
import { Link, useLocation } from "react-router";
import { Button } from "../Button";
import { useAuthStore } from "@/entities/session";

// Хук для переключения темы
import { useEffect, useState } from "react";

function useTheme() {
    const [theme, setTheme] = useState(() => {
        if (typeof window !== "undefined") {
            return localStorage.getItem("theme") || "light";
        }
        return "light";
    });

    useEffect(() => {
        if (theme === "dark") {
            document.documentElement.classList.add("dark");
        } else {
            document.documentElement.classList.remove("dark");
        }
        localStorage.setItem("theme", theme);
    }, [theme]);

    return [theme, setTheme] as const;
}

export const Navigation: FC = () => {
    const { user, clearAuth } = useAuthStore();
    const isAuthenticated = !!user;
    const location = useLocation();
    const [theme, setTheme] = useTheme();

    const isActive = (path: string) => {
        return location.pathname === path;
    };

    return (
        <nav className="border-border bg-background border-b shadow-sm">
            <div className="container mx-auto px-4">
                <div className="flex h-16 items-center justify-between">
                    {/* Логотип */}
                    <div className="flex items-center">
                        <Link to="/" className="text-primary text-xl font-bold">
                            WBand
                        </Link>
                    </div>

                    {/* Навигационные ссылки */}
                    <div className="hidden items-center space-x-6 md:flex">
                        <Link
                            to="/"
                            className={`text-sm font-medium transition-colors ${
                                isActive("/")
                                    ? "text-primary border-primary border-b-2 pb-1"
                                    : "hover:text-primary text-muted-foreground"
                            }`}
                        >
                            Главная
                        </Link>

                        {isAuthenticated && (
                            <>
                                <Link
                                    to="/bands"
                                    className={`text-sm font-medium transition-colors ${
                                        isActive("/bands")
                                            ? "text-primary border-primary border-b-2 pb-1"
                                            : "hover:text-primary text-gray-600"
                                    }`}
                                >
                                    Мои группы
                                </Link>
                                <Link
                                    to="/invitations"
                                    className={`text-sm font-medium transition-colors ${
                                        isActive("/invitations")
                                            ? "text-primary border-primary border-b-2 pb-1"
                                            : "hover:text-primary text-gray-600"
                                    }`}
                                >
                                    Приглашения
                                </Link>
                            </>
                        )}
                    </div>

                    {/* Пользовательское меню и переключатель темы */}
                    <div className="flex items-center space-x-4">
                        <button
                            aria-label="Переключить тему"
                            className="hover:bg-muted rounded p-2 text-xl transition-colors"
                            onClick={() =>
                                setTheme(theme === "dark" ? "light" : "dark")
                            }
                        >
                            {theme === "dark" ? "🌙" : "☀️"}
                        </button>
                        {isAuthenticated ? (
                            <div className="flex items-center space-x-3">
                                <span className="text-muted-foreground text-sm">
                                    {user.username || user.email}
                                </span>
                                <Button
                                    onClick={clearAuth}
                                    className="bg-muted hover:bg-muted-foreground px-3 py-1 text-sm"
                                >
                                    Выйти
                                </Button>
                            </div>
                        ) : (
                            <div className="flex items-center space-x-3">
                                <Link to="/login">
                                    <Button className="bg-muted hover:bg-muted-foreground px-3 py-1 text-sm">
                                        Войти
                                    </Button>
                                </Link>
                                <Link to="/register">
                                    <Button className="px-3 py-1 text-sm">
                                        Регистрация
                                    </Button>
                                </Link>
                            </div>
                        )}
                    </div>
                </div>

                {/* Мобильное меню */}
                <div className="pb-4 md:hidden">
                    <div className="flex flex-col space-y-2">
                        <Link
                            to="/"
                            className={`rounded px-3 py-2 text-sm font-medium transition-colors ${
                                isActive("/")
                                    ? "bg-primary text-primary-foreground"
                                    : "text-muted-foreground hover:bg-muted"
                            }`}
                        >
                            Главная
                        </Link>

                        {isAuthenticated && (
                            <>
                                <Link
                                    to="/bands"
                                    className={`rounded px-3 py-2 text-sm font-medium transition-colors ${
                                        isActive("/bands")
                                            ? "bg-primary text-primary-foreground"
                                            : "text-muted-foreground hover:bg-muted"
                                    }`}
                                >
                                    Мои группы
                                </Link>
                                <Link
                                    to="/invitations"
                                    className={`rounded px-3 py-2 text-sm font-medium transition-colors ${
                                        isActive("/invitations")
                                            ? "bg-primary text-primary-foreground"
                                            : "text-muted-foreground hover:bg-muted"
                                    }`}
                                >
                                    Приглашения
                                </Link>
                            </>
                        )}
                    </div>
                </div>
            </div>
        </nav>
    );
};
