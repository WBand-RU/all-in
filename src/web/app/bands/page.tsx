"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthStore } from "shared/store/auth-store";
import { Button } from "shared/ui/button";
import {
    Card,
    CardContent,
    CardHeader,
    CardTitle,
} from "shared/shadcn/ui/card";
import { Badge } from "@/shared/shadcn/ui/badge";
import { Plus, Users, Settings, ChevronRight } from "lucide-react";

interface Band {
    _id: string;
    name: string;
    description?: string;
    owner: {
        username: string;
        email: string;
    };
    members: Array<{
        user: {
            username: string;
            email: string;
        };
        role: string;
        status: string;
    }>;
    roles: Array<{
        name: string;
        permissions: string[];
    }>;
}

export default function BandsPage() {
    const { isAuthenticated, logout } = useAuthStore();
    const router = useRouter();
    const [bands, setBands] = useState<Band[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        if (!isAuthenticated) {
            router.push("/");
            return;
        }

        fetchBands();
    }, [isAuthenticated, router]);

    const fetchBands = async () => {
        try {
            const response = await fetch("/api/bands");
            if (response.ok) {
                const data = await response.json();
                setBands(data.bands);
            }
        } catch (error) {
            console.error("Ошибка при загрузке групп:", error);
        } finally {
            setIsLoading(false);
        }
    };

    const createBand = () => {
        router.push("/bands/create");
    };

    const openBand = (bandId: string) => {
        router.push(`/bands/${bandId}`);
    };

    if (!isAuthenticated) {
        return null;
    }

    return (
        <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
            {/* Header */}
            <header className="bg-white shadow-sm border-b">
                <div className="max-w-6xl mx-auto px-4 py-4 flex justify-between items-center">
                    <div className="flex items-center gap-6">
                        <h1 className="text-2xl font-bold text-gray-800">
                            WBand
                        </h1>
                        <nav className="flex gap-6">
                            <Button
                                variant="ghost"
                                onClick={() => router.push("/")}
                            >
                                Главная
                            </Button>
                            <Button
                                variant="ghost"
                                onClick={() => router.push("/bands")}
                            >
                                Группы
                            </Button>
                        </nav>
                    </div>
                    <Button variant="outline" onClick={logout}>
                        Выйти
                    </Button>
                </div>
            </header>

            {/* Main Content */}
            <div className="max-w-6xl mx-auto px-4 py-8">
                <div className="flex justify-between items-center mb-8">
                    <div>
                        <h1 className="text-3xl font-bold text-gray-800 mb-2">
                            Мои группы
                        </h1>
                        <p className="text-gray-600">
                            Управляйте своими музыкальными группами и
                            участниками
                        </p>
                    </div>
                    <Button
                        onClick={createBand}
                        className="flex items-center gap-2"
                    >
                        <Plus className="h-4 w-4" />
                        Создать группу
                    </Button>
                </div>

                {isLoading ? (
                    <div className="flex justify-center items-center py-12">
                        <div className="text-gray-500">Загрузка групп...</div>
                    </div>
                ) : bands.length === 0 ? (
                    <Card className="text-center py-12">
                        <CardContent>
                            <Users className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                            <h3 className="text-xl font-semibold text-gray-800 mb-2">
                                У вас пока нет групп
                            </h3>
                            <p className="text-gray-600 mb-6">
                                Создайте свою первую музыкальную группу для
                                начала работы
                            </p>
                            <Button onClick={createBand}>
                                <Plus className="h-4 w-4 mr-2" />
                                Создать первую группу
                            </Button>
                        </CardContent>
                    </Card>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                        {bands.map((band) => (
                            <Card
                                key={band._id}
                                className="hover:shadow-lg transition-shadow cursor-pointer"
                                onClick={() => openBand(band._id)}
                            >
                                <CardHeader>
                                    <div className="flex justify-between items-start">
                                        <div className="flex-1">
                                            <CardTitle className="text-lg">
                                                {band.name}
                                            </CardTitle>
                                            {band.description && (
                                                <p className="text-sm text-gray-600 mt-1 line-clamp-2">
                                                    {band.description}
                                                </p>
                                            )}
                                        </div>
                                        <ChevronRight className="h-5 w-5 text-gray-400" />
                                    </div>
                                </CardHeader>
                                <CardContent>
                                    <div className="space-y-3">
                                        <div className="flex items-center gap-2 text-sm text-gray-600">
                                            <Users className="h-4 w-4" />
                                            <span>
                                                {band.members.length} участников
                                            </span>
                                        </div>

                                        <div className="flex flex-wrap gap-1">
                                            {band.members
                                                .slice(0, 3)
                                                .map((member, index) => (
                                                    <Badge
                                                        key={index}
                                                        variant="secondary"
                                                        className="text-xs"
                                                    >
                                                        {member.user.username}
                                                    </Badge>
                                                ))}
                                            {band.members.length > 3 && (
                                                <Badge
                                                    variant="outline"
                                                    className="text-xs"
                                                >
                                                    +{band.members.length - 3}
                                                </Badge>
                                            )}
                                        </div>

                                        <div className="flex justify-between items-center pt-2">
                                            <span className="text-xs text-gray-500">
                                                Владелец: {band.owner.username}
                                            </span>
                                            <Button size="sm" variant="outline">
                                                <Settings className="h-3 w-3 mr-1" />
                                                Настройки
                                            </Button>
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}
